using PaunixGuard.Core.Settings;
using PaunixGuard.Core.Triggers;
using Xunit;

namespace PaunixGuard.App.Tests.Core;

public sealed class TriggerPolicyTests
{
    [Fact]
    public void Decide_ReturnsAlarm_ForEnabledConfirmedThreat()
    {
        var policy = new TriggerPolicy();
        var settings = new GuardSettings();
        var signal = TriggerSignal.Create(
            TriggerType.ChargerUnplugged,
            "Power adapter was unplugged.",
            "test",
            DateTimeOffset.UtcNow);

        Assert.Equal(TriggerDecision.Alarm, policy.Decide(signal, settings));
    }

    [Fact]
    public void Decide_ReturnsAlarm_ForLidClosed()
    {
        var policy = new TriggerPolicy();
        var settings = new GuardSettings();
        var signal = TriggerSignal.Create(
            TriggerType.LidClosed,
            "Laptop lid was closed.",
            "test",
            DateTimeOffset.UtcNow);

        Assert.Equal(TriggerDecision.Alarm, policy.Decide(signal, settings));
    }

    [Fact]
    public void Decide_ReturnsAlarm_ForSleepAttempt()
    {
        var policy = new TriggerPolicy();
        var settings = new GuardSettings();
        var signal = TriggerSignal.Create(
            TriggerType.SleepAttempt,
            "Sleep was requested.",
            "test",
            DateTimeOffset.UtcNow);

        Assert.Equal(TriggerDecision.Alarm, policy.Decide(signal, settings));
    }

    [Fact]
    public void Decide_ReturnsAlarm_ForShutdownAttempt()
    {
        var policy = new TriggerPolicy();
        var settings = new GuardSettings();
        var signal = TriggerSignal.Create(
            TriggerType.ShutdownAttempt,
            "Shutdown was requested.",
            "test",
            DateTimeOffset.UtcNow);

        Assert.Equal(TriggerDecision.Alarm, policy.Decide(signal, settings));
    }

    [Fact]
    public void Decide_ReturnsAlarm_ForManualPanic()
    {
        var policy = new TriggerPolicy();
        var settings = new GuardSettings();
        var signal = TriggerSignal.Create(
            TriggerType.ManualPanic,
            "Manual panic triggered.",
            "test",
            DateTimeOffset.UtcNow);

        Assert.Equal(TriggerDecision.Alarm, policy.Decide(signal, settings));
    }

    [Fact]
    public void Decide_ReturnsWarning_ForPhoneDisconnected()
    {
        var policy = new TriggerPolicy();
        var settings = new GuardSettings();
        settings.EnabledTriggers.Add(TriggerType.PhoneDisconnected);
        var signal = TriggerSignal.Create(
            TriggerType.PhoneDisconnected,
            "Phone disconnected.",
            "test",
            DateTimeOffset.UtcNow);

        Assert.Equal(TriggerDecision.Warning, policy.Decide(signal, settings));
    }

    [Fact]
    public void Decide_ReturnsWarning_ForWebcamMotion()
    {
        var policy = new TriggerPolicy();
        var settings = new GuardSettings();
        settings.EnabledTriggers.Add(TriggerType.WebcamMotion);
        var signal = TriggerSignal.Create(
            TriggerType.WebcamMotion,
            "Webcam motion detected.",
            "test",
            DateTimeOffset.UtcNow);

        Assert.Equal(TriggerDecision.Warning, policy.Decide(signal, settings));
    }

    [Fact]
    public void Decide_ReturnsIgnore_WhenTriggerDisabled()
    {
        var policy = new TriggerPolicy();
        var settings = new GuardSettings();
        settings.EnabledTriggers.Remove(TriggerType.InputActivity);
        var signal = TriggerSignal.Create(
            TriggerType.InputActivity,
            "Input occurred.",
            "test",
            DateTimeOffset.UtcNow);

        Assert.Equal(TriggerDecision.Ignore, policy.Decide(signal, settings));
    }

    [Fact]
    public void Decide_ReturnsIgnore_WhenChargerTriggerDisabled()
    {
        var policy = new TriggerPolicy();
        var settings = new GuardSettings();
        settings.EnabledTriggers.Remove(TriggerType.ChargerUnplugged);
        var signal = TriggerSignal.Create(
            TriggerType.ChargerUnplugged,
            "Power adapter was unplugged.",
            "test",
            DateTimeOffset.UtcNow);

        Assert.Equal(TriggerDecision.Ignore, policy.Decide(signal, settings));
    }
}

