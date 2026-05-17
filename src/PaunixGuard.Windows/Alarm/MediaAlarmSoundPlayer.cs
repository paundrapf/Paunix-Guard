using System.Media;
using System.Reflection;
using PaunixGuard.Core.Alarm;

namespace PaunixGuard.Windows.Alarm;

public sealed class MediaAlarmSoundPlayer : IAlarmSoundPlayer, IDisposable
{
    private readonly object playerGate = new();
    private SoundPlayer? soundPlayer;
    private CancellationTokenSource? cancellation;
    private Task? playbackTask;

    public bool IsPlaying => playbackTask is { IsCompleted: false };

    public Task StartAsync(string? alarmSound, CancellationToken cancellationToken)
    {
        if (IsPlaying)
        {
            return Task.CompletedTask;
        }

        cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        playbackTask = Task.Run(() => PlaybackLoop(alarmSound, cancellation.Token), cancellation.Token);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (cancellation is null)
        {
            return;
        }

        await cancellation.CancelAsync();

        if (playbackTask is not null)
        {
            try
            {
                await playbackTask.WaitAsync(TimeSpan.FromSeconds(3), cancellationToken);
            }
            catch (OperationCanceledException)
            {
            }
            catch (TimeoutException)
            {
            }
        }

        StopSound();
        cancellation.Dispose();
        cancellation = null;
        playbackTask = null;
    }

    public void Dispose()
    {
        cancellation?.Cancel();
        StopSound();
        cancellation?.Dispose();
    }

    private async Task PlaybackLoop(string? filePath, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            if (File.Exists(filePath))
            {
                try
                {
                    StopSound();
                    SoundPlayer player;
                    lock (playerGate)
                    {
                        player = new SoundPlayer(filePath);
                        soundPlayer = player;
                    }

                    player.PlaySync();
                }
                catch
                {
                    Console.Beep(1400, 350);
                    Console.Beep(900, 250);
                }
            }
            else
            {
                try
                {
                    Console.Beep(1400, 350);
                    Console.Beep(900, 250);
                }
                catch
                {
                }
            }

            try
            {
                await Task.Delay(100, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    private void StopSound()
    {
        SoundPlayer? player;
        lock (playerGate)
        {
            player = soundPlayer;
            soundPlayer = null;
        }

        if (player is null)
        {
            return;
        }

        try
        {
            player.Stop();
            player.Dispose();
        }
        catch
        {
        }
    }
}
