namespace PaunixGuard.Core.Alarm;

public interface IAudioService
{
    Task<AudioStateSnapshot> CaptureAsync(CancellationToken cancellationToken);

    Task PrepareForAlarmAsync(BluetoothAlarmBehavior bluetoothBehavior, CancellationToken cancellationToken);

    Task RestoreAsync(AudioStateSnapshot snapshot, CancellationToken cancellationToken);
}

