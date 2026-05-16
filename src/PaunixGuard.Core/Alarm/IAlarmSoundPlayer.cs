namespace PaunixGuard.Core.Alarm;

public interface IAlarmSoundPlayer
{
    bool IsPlaying { get; }

    Task StartAsync(string? alarmSound, CancellationToken cancellationToken);

    Task StopAsync(CancellationToken cancellationToken);
}

