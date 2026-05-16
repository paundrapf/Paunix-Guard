using PaunixGuard.Core.Events;
using PaunixGuard.Core.Settings;
using PaunixGuard.Core.Triggers;

namespace PaunixGuard.Core.Alarm;

public sealed class AlarmOrchestrator(IAudioService audioService, IAlarmSoundPlayer soundPlayer) : IAlarmOrchestrator
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private AudioStateSnapshot? snapshot;

    public async Task StartAsync(TriggerSignal signal, GuardSettings settings, GuardEvent guardEvent, CancellationToken cancellationToken)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            if (soundPlayer.IsPlaying)
            {
                return;
            }

            if (settings.ForceVolumeEnabled)
            {
                snapshot = await audioService.CaptureAsync(cancellationToken);
                await audioService.PrepareForAlarmAsync(settings.BluetoothAlarmBehavior, cancellationToken);
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

            if (snapshot is not null)
            {
                await audioService.RestoreAsync(snapshot, cancellationToken);
                snapshot = null;
            }
        }
        finally
        {
            gate.Release();
        }
    }
}

