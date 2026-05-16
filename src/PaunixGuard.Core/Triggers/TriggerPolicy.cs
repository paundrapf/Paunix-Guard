using PaunixGuard.Core.Settings;

namespace PaunixGuard.Core.Triggers;

public sealed class TriggerPolicy
{
    private static readonly HashSet<TriggerType> ConfirmedThreats =
    [
        TriggerType.ChargerUnplugged,
        TriggerType.LidClosed,
        TriggerType.SleepAttempt,
        TriggerType.ShutdownAttempt,
        TriggerType.ManualPanic,
        TriggerType.DesktopSwitch
    ];

    private static readonly HashSet<TriggerType> SuspiciousSignals =
    [
        TriggerType.PhoneDisconnected,
        TriggerType.WebcamMotion,
        TriggerType.WifiChanged
    ];

    public TriggerDecision Decide(TriggerSignal signal, GuardSettings settings)
    {
        if (!settings.EnabledTriggers.Contains(signal.Type))
        {
            return TriggerDecision.Ignore;
        }

        if (ConfirmedThreats.Contains(signal.Type))
        {
            return TriggerDecision.Alarm;
        }

        if (signal.Type == TriggerType.InputActivity)
        {
            return TriggerDecision.Warning;
        }

        if (SuspiciousSignals.Contains(signal.Type))
        {
            return TriggerDecision.Warning;
        }

        return TriggerDecision.Ignore;
    }
}

