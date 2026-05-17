using PaunixGuard.Core.Events;
using PaunixGuard.Core.Settings;
using PaunixGuard.Core.Triggers;

namespace PaunixGuard.Core.Alarm;

public sealed class AlarmOrchestrator(IAudioService audioService, IAlarmSoundPlayer soundPlayer) : IAlarmOrchestrator
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private AudioStateSnapshot? snapshot;
    private bool restoreAudioOnStop;

    public async Task StartAsync(TriggerSignal signal, GuardSettings settings, GuardEvent guardEvent, CancellationToken cancellationToken)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            if (soundPlayer.IsPlaying)
            {
                return;
            }

            snapshot = null;
            restoreAudioOnStop = settings.RestoreAudioAfterDisarm;

            if (settings.ForceVolumeEnabled)
            {
                try
                {
                    if (settings.RestoreAudioAfterDisarm)
                    {
                        snapshot = await audioService.CaptureAsync(cancellationToken);
                    }

                    await audioService.PrepareForAlarmAsync(settings.BluetoothAlarmBehavior, cancellationToken);
                }
                catch
                {
                    snapshot = null;
                }
            }

            await soundPlayer.StartAsync(settings.AlarmSound, cancellationToken);
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            await soundPlayer.StopAsync(cancellationToken);

            if (restoreAudioOnStop && snapshot is not null)
            {
                await audioService.RestoreAsync(snapshot, cancellationToken);
            }

            snapshot = null;
            restoreAudioOnStop = false;
        }
        finally
        {
            gate.Release();
        }
    }
}
