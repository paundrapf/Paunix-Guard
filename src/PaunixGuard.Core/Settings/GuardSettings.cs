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

    public int GracePeriodSeconds { get; set; } = 3;

    public int InputWarningSeconds { get; set; } = 4;

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
        TriggerType.ManualPanic,
        TriggerType.DesktopSwitch
    ];

    public string UpdateChannel { get; set; } = "stable";

    public bool AutoCheckUpdates { get; set; } = true;

    public GuardSettings Clone()
    {
        return new GuardSettings
        {
            PinHash = PinHash,
            ArmingDelaySeconds = ArmingDelaySeconds,
            AlarmSound = AlarmSound,
            ForceVolumeEnabled = ForceVolumeEnabled,
            RestoreAudioAfterDisarm = RestoreAudioAfterDisarm,
            GracePeriodSeconds = GracePeriodSeconds,
            InputWarningSeconds = InputWarningSeconds,
            BluetoothAlarmBehavior = BluetoothAlarmBehavior,
            KeepSystemAwakeWhileArmed = KeepSystemAwakeWhileArmed,
            KeepDisplayAwakeWhileArmed = KeepDisplayAwakeWhileArmed,
            BlockShutdownWhileArmed = BlockShutdownWhileArmed,
            EnabledTriggers = EnabledTriggers is null ? [] : [.. EnabledTriggers],
            UpdateChannel = UpdateChannel,
            AutoCheckUpdates = AutoCheckUpdates
        };
    }

    public void Normalize()
    {
        ArmingDelaySeconds = Math.Clamp(ArmingDelaySeconds, 0, 3600);
        GracePeriodSeconds = Math.Clamp(GracePeriodSeconds, 0, 300);
        InputWarningSeconds = Math.Clamp(InputWarningSeconds, 1, 300);

        EnabledTriggers ??=
        [
            TriggerType.ChargerUnplugged,
            TriggerType.InputActivity,
            TriggerType.LidClosed,
            TriggerType.SleepAttempt,
            TriggerType.ShutdownAttempt,
            TriggerType.ManualPanic,
            TriggerType.DesktopSwitch
        ];

        if (string.IsNullOrWhiteSpace(UpdateChannel))
        {
            UpdateChannel = "stable";
        }
    }
}
