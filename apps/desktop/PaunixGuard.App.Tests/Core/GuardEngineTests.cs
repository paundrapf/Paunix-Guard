using PaunixGuard.App.Tests.Fakes;
using PaunixGuard.Core.Alarm;
using PaunixGuard.Core.Events;
using PaunixGuard.Core.GuardState;
using PaunixGuard.Core.Security;
using PaunixGuard.Core.Settings;
using PaunixGuard.Core.Triggers;
using Xunit;

namespace PaunixGuard.App.Tests.Core;

public sealed class GuardEngineTests
{
    [Fact]
    public async Task StartGuardAsync_TransitionsToArmed_AndStartsProtection()
    {
        var fixture = CreateFixture(new GuardSettings { ArmingDelaySeconds = 0 });
        await fixture.Engine.InitializeAsync(CancellationToken.None);
        await fixture.Engine.SetPinAsync("1234", CancellationToken.None);

        await fixture.Engine.StartGuardAsync(CancellationToken.None);

        Assert.Equal(GuardState.Armed, fixture.Engine.CurrentState);
        Assert.True(fixture.TriggerSupervisor.IsStarted);
        Assert.Equal(1, fixture.PowerProtection.EnableCount);
    }

    [Fact]
    public async Task ConfirmedTrigger_StartsAlarm_AndWritesEvent()
    {
        var fixture = CreateFixture(new GuardSettings { ArmingDelaySeconds = 0 });
        await fixture.Engine.InitializeAsync(CancellationToken.None);
        await fixture.Engine.SetPinAsync("1234", CancellationToken.None);
        await fixture.Engine.StartGuardAsync(CancellationToken.None);

        await fixture.TriggerSupervisor.RaiseAsync(TriggerSignal.Create(
            TriggerType.ChargerUnplugged,
            "Power adapter unplugged.",
            "test",
            fixture.Clock.UtcNow));

        Assert.Equal(GuardState.Alarm, fixture.Engine.CurrentState);
        Assert.Equal(1, fixture.AlarmOrchestrator.StartCount);
        Assert.Single(fixture.EventHistory.Events);
        Assert.Equal(TriggerType.ChargerUnplugged, fixture.EventHistory.Events[0].TriggerType);
    }

    [Fact]
    public async Task InputActivity_TransitionsToWarning_NotAlarm()
    {
        var fixture = CreateFixture(new GuardSettings
        {
            ArmingDelaySeconds = 0,
            GracePeriodSeconds = 0,
            InputWarningSeconds = 5
        });
        await fixture.Engine.InitializeAsync(CancellationToken.None);
        await fixture.Engine.SetPinAsync("1234", CancellationToken.None);
        await fixture.Engine.StartGuardAsync(CancellationToken.None);

        await fixture.TriggerSupervisor.RaiseAsync(TriggerSignal.Create(
            TriggerType.InputActivity,
            "Input occurred.",
            "test",
            fixture.Clock.UtcNow));

        Assert.Equal(GuardState.Warning, fixture.Engine.CurrentState);
        Assert.Equal(0, fixture.AlarmOrchestrator.StartCount);
        Assert.Empty(fixture.EventHistory.Events);
    }

    [Fact]
    public async Task DisarmAsync_RequiresValidPin_AndStopsAlarm()
    {
        var fixture = CreateFixture(new GuardSettings { ArmingDelaySeconds = 0 });
        await fixture.Engine.InitializeAsync(CancellationToken.None);
        await fixture.Engine.SetPinAsync("1234", CancellationToken.None);
        await fixture.Engine.StartGuardAsync(CancellationToken.None);
        await fixture.TriggerSupervisor.RaiseAsync(TriggerSignal.Create(
            TriggerType.ChargerUnplugged,
            "Power adapter unplugged.",
            "test",
            fixture.Clock.UtcNow));

        var wrong = await fixture.Engine.DisarmAsync("0000", DisarmMethod.LaptopPin, CancellationToken.None);
        var right = await fixture.Engine.DisarmAsync("1234", DisarmMethod.LaptopPin, CancellationToken.None);

        Assert.False(wrong);
        Assert.True(right);
        Assert.Equal(GuardState.Idle, fixture.Engine.CurrentState);
        Assert.Equal(1, fixture.AlarmOrchestrator.StopCount);
        Assert.Equal(EventResolution.Disarmed, fixture.EventHistory.Events[0].Resolution);
    }

    [Fact]
    public async Task ForceStopAsync_StopsGuardWithoutPin()
    {
        var fixture = CreateFixture(new GuardSettings { ArmingDelaySeconds = 0 });
        await fixture.Engine.InitializeAsync(CancellationToken.None);
        await fixture.Engine.SetPinAsync("1234", CancellationToken.None);
        await fixture.Engine.StartGuardAsync(CancellationToken.None);
        await fixture.TriggerSupervisor.RaiseAsync(TriggerSignal.Create(
            TriggerType.ShutdownAttempt,
            "Shutdown attempted.",
            "test",
            fixture.Clock.UtcNow));

        await fixture.Engine.ForceStopAsync(DisarmMethod.SystemShutdown, CancellationToken.None);

        Assert.Equal(GuardState.Idle, fixture.Engine.CurrentState);
        Assert.Equal(1, fixture.AlarmOrchestrator.StopCount);
        Assert.Equal(EventResolution.Cancelled, fixture.EventHistory.Events[0].Resolution);
    }

    [Fact]
    public async Task HasPin_ReturnsFalse_WhenNoPinSet()
    {
        var fixture = CreateFixture(new GuardSettings());
        await fixture.Engine.InitializeAsync(CancellationToken.None);

        Assert.False(fixture.Engine.HasPin);
    }

    [Fact]
    public async Task HasPin_ReturnsTrue_AfterPinSet()
    {
        var fixture = CreateFixture(new GuardSettings());
        await fixture.Engine.InitializeAsync(CancellationToken.None);
        await fixture.Engine.SetPinAsync("abcd", CancellationToken.None);

        Assert.True(fixture.Engine.HasPin);
    }

    [Fact]
    public async Task StartGuardWithoutPin_ThrowsInvalidOperation()
    {
        var fixture = CreateFixture(new GuardSettings());
        await fixture.Engine.InitializeAsync(CancellationToken.None);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Engine.StartGuardAsync(CancellationToken.None));
    }

    [Fact]
    public async Task SavedSettings_AreLoadedOnInitialize()
    {
        var settings = new GuardSettings
        {
            ArmingDelaySeconds = 10,
            KeepSystemAwakeWhileArmed = false,
            BlockShutdownWhileArmed = false
        };

        var fixture = CreateFixture(settings);
        await fixture.Engine.InitializeAsync(CancellationToken.None);

        Assert.Equal(10, fixture.Engine.Settings.ArmingDelaySeconds);
        Assert.False(fixture.Engine.Settings.KeepSystemAwakeWhileArmed);
        Assert.False(fixture.Engine.Settings.BlockShutdownWhileArmed);
    }

    private static Fixture CreateFixture(GuardSettings settings)
    {
        var settingsStore = new FakeSettingsStore(settings);
        var eventHistory = new FakeEventHistoryStore();
        var alarm = new FakeAlarmOrchestrator();
        var triggers = new FakeTriggerSupervisor();
        var power = new FakePowerProtectionService();
        var clock = new FakeClock();
        var engine = new GuardEngine(
            settingsStore,
            eventHistory,
            new PinHasher(),
            alarm,
            triggers,
            power,
            clock,
            new TriggerPolicy());

        return new Fixture(engine, eventHistory, alarm, triggers, power, clock);
    }

    private sealed record Fixture(
        GuardEngine Engine,
        FakeEventHistoryStore EventHistory,
        FakeAlarmOrchestrator AlarmOrchestrator,
        FakeTriggerSupervisor TriggerSupervisor,
        FakePowerProtectionService PowerProtection,
        FakeClock Clock);
}

