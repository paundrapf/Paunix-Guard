using System.Reflection;
using PaunixGuard.Core.Alarm;
using PaunixGuard.Core.Events;
using PaunixGuard.Core.Security;
using PaunixGuard.Core.Settings;
using PaunixGuard.Core.Triggers;

namespace PaunixGuard.Core.GuardState;

public sealed class GuardEngine
{
    private readonly ISettingsStore settingsStore;
    private readonly IEventHistoryStore eventHistoryStore;
    private readonly IPinHasher pinHasher;
    private readonly IAlarmOrchestrator alarmOrchestrator;
    private readonly ITriggerSupervisor triggerSupervisor;
    private readonly IPowerProtectionService powerProtectionService;
    private readonly IClock clock;
    private readonly TriggerPolicy triggerPolicy;
    private readonly SemaphoreSlim gate = new(1, 1);

    private GuardSettings settings = new();
    private GuardEvent? activeEvent;
    private CancellationTokenSource? guardCancellation;

    public GuardEngine(
        ISettingsStore settingsStore,
        IEventHistoryStore eventHistoryStore,
        IPinHasher pinHasher,
        IAlarmOrchestrator alarmOrchestrator,
        ITriggerSupervisor triggerSupervisor,
        IPowerProtectionService powerProtectionService,
        IClock clock,
        TriggerPolicy triggerPolicy)
    {
        this.settingsStore = settingsStore;
        this.eventHistoryStore = eventHistoryStore;
        this.pinHasher = pinHasher;
        this.alarmOrchestrator = alarmOrchestrator;
        this.triggerSupervisor = triggerSupervisor;
        this.powerProtectionService = powerProtectionService;
        this.clock = clock;
        this.triggerPolicy = triggerPolicy;
    }

    public event EventHandler<GuardStateChangedEventArgs>? StateChanged;

    public GuardState CurrentState { get; private set; } = GuardState.Idle;

    public GuardSettings Settings => settings;

    public bool HasPin => !string.IsNullOrWhiteSpace(settings.PinHash);

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await eventHistoryStore.InitializeAsync(cancellationToken);
        settings = await settingsStore.LoadAsync(cancellationToken);
    }

    public async Task SaveSettingsAsync(GuardSettings updatedSettings, CancellationToken cancellationToken)
    {
        settings = updatedSettings;
        await settingsStore.SaveAsync(settings, cancellationToken);
    }

    public async Task SetPinAsync(string pin, CancellationToken cancellationToken)
    {
        settings.PinHash = pinHasher.HashPin(pin);
        await settingsStore.SaveAsync(settings, cancellationToken);
    }

    public async Task StartGuardAsync(CancellationToken cancellationToken)
    {
        if (!HasPin)
        {
            throw new InvalidOperationException("A PIN is required before starting guard mode.");
        }

        await gate.WaitAsync(cancellationToken);
        try
        {
            if (CurrentState is GuardState.Arming or GuardState.Armed or GuardState.Alarm)
            {
                return;
            }

            guardCancellation?.Dispose();
            guardCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            TransitionTo(GuardState.Arming);
        }
        finally
        {
            gate.Release();
        }

        var delay = TimeSpan.FromSeconds(Math.Max(0, settings.ArmingDelaySeconds));
        if (delay > TimeSpan.Zero)
        {
            await Task.Delay(delay, guardCancellation.Token);
        }

        await gate.WaitAsync(cancellationToken);
        try
        {
            if (CurrentState != GuardState.Arming)
            {
                return;
            }

            await powerProtectionService.EnableAsync(settings, cancellationToken);
            await triggerSupervisor.StartAsync(HandleTriggerAsync, guardCancellation.Token);
            TransitionTo(GuardState.Armed);
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task TriggerManualAlarmAsync(CancellationToken cancellationToken)
    {
        await HandleTriggerAsync(TriggerSignal.Create(
            TriggerType.ManualPanic,
            "Manual panic/test alarm requested.",
            "PaunixGuard.App",
            clock.UtcNow));
    }

    public async Task<bool> DisarmAsync(string pin, DisarmMethod method, CancellationToken cancellationToken)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            if (!ValidatePin(pin))
            {
                return false;
            }

            if (CurrentState == GuardState.Idle)
            {
                return true;
            }

            TransitionTo(GuardState.Disarming);
            guardCancellation?.Cancel();
            await triggerSupervisor.StopAsync(cancellationToken);
            await powerProtectionService.DisableAsync(cancellationToken);
            await alarmOrchestrator.StopAsync(cancellationToken);

            if (activeEvent is not null)
            {
                activeEvent.AlarmStoppedAt = clock.UtcNow;
                activeEvent.DisarmMethod = method;
                activeEvent.Resolution = EventResolution.Disarmed;
                await eventHistoryStore.UpdateAsync(activeEvent, cancellationToken);
                activeEvent = null;
            }

            TransitionTo(GuardState.Idle);
            return true;
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task ForceStopAsync(DisarmMethod method, CancellationToken cancellationToken)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            if (CurrentState == GuardState.Idle)
            {
                return;
            }

            TransitionTo(GuardState.Disarming);
            guardCancellation?.Cancel();
            await triggerSupervisor.StopAsync(cancellationToken);
            await powerProtectionService.DisableAsync(cancellationToken);
            await alarmOrchestrator.StopAsync(cancellationToken);

            if (activeEvent is not null)
            {
                activeEvent.AlarmStoppedAt = clock.UtcNow;
                activeEvent.DisarmMethod = method;
                activeEvent.Resolution = EventResolution.Cancelled;
                await eventHistoryStore.UpdateAsync(activeEvent, cancellationToken);
                activeEvent = null;
            }

            TransitionTo(GuardState.Idle);
        }
        finally
        {
            gate.Release();
        }
    }

    private async Task HandleTriggerAsync(TriggerSignal signal)
    {
        if (signal.ContainsSensitiveContent)
        {
            throw new InvalidOperationException("Trigger signals must not contain sensitive input contents.");
        }

        await gate.WaitAsync(CancellationToken.None);
        try
        {
            if (CurrentState is not GuardState.Armed and not GuardState.Warning)
            {
                return;
            }

            var decision = triggerPolicy.Decide(signal, settings);
            if (decision == TriggerDecision.Ignore)
            {
                return;
            }

            if (decision == TriggerDecision.Warning)
            {
                TransitionTo(GuardState.Warning, signal);
                return;
            }

            var previousState = CurrentState;
            var guardEvent = CreateEvent(signal, previousState);
            activeEvent = guardEvent;
            await eventHistoryStore.AddAsync(guardEvent, CancellationToken.None);

            TransitionTo(GuardState.Alarm, signal, guardEvent);
            guardEvent.AlarmStartedAt = clock.UtcNow;
            await eventHistoryStore.UpdateAsync(guardEvent, CancellationToken.None);
            await alarmOrchestrator.StartAsync(signal, settings, guardEvent, CancellationToken.None);
        }
        finally
        {
            gate.Release();
        }
    }

    private bool ValidatePin(string pin)
    {
        return settings.PinHash is not null && pinHasher.Verify(pin, settings.PinHash);
    }

    private GuardEvent CreateEvent(TriggerSignal signal, GuardState previousState)
    {
        return new GuardEvent
        {
            CreatedAt = clock.UtcNow,
            TriggerType = signal.Type,
            GuardStateBefore = previousState,
            Reason = signal.Reason,
            Source = signal.Source,
            AppVersion = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "0.0.0-dev"
        };
    }

    private void TransitionTo(GuardState nextState, TriggerSignal? signal = null, GuardEvent? guardEvent = null)
    {
        var previousState = CurrentState;
        CurrentState = nextState;
        StateChanged?.Invoke(this, new GuardStateChangedEventArgs(previousState, nextState, signal, guardEvent));
    }
}
