using PaunixGuard.Core.Alarm;
using PaunixGuard.Core.Triggers;

namespace PaunixGuard.Core.Settings;

public sealed class GuardSettings
{
    public string? PinHash { get; set; }

    public int ArmingDelaySeconds { get; set; } = 5;

    public string? AlarmSound { get; set; }

    public bool ForceVolumeEnabled { get; set; } = true;

    public bool RestoreAudioAfterDisarm { get; set; } = true;

    public BluetoothAlarmBehavior BluetoothAlarmBehavior { get; set; } = BluetoothAlarmBehavior.PreferInternalSpeaker;

    public bool KeepSystemAwakeWhileArmed { get; set; } = true;

    public bool KeepDisplayAwakeWhileArmed { get; set; }

    public bool BlockShutdownWhileArmed { get; set; } = true;

    public HashSet<TriggerType> EnabledTriggers { get; set; } =
    [
        TriggerType.ChargerUnplugged,
        TriggerType.InputActivity,
        TriggerType.LidClosed,
        TriggerType.SleepAttempt,
        TriggerType.ShutdownAttempt,
        TriggerType.ManualPanic
    ];

    public string UpdateChannel { get; set; } = "stable";

    public bool AutoCheckUpdates { get; set; } = true;
}

