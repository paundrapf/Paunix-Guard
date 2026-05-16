using System.Runtime.InteropServices;
using PaunixGuard.Core.Alarm;

namespace PaunixGuard.Windows.Alarm;

public sealed class BeepAlarmSoundPlayer : IAlarmSoundPlayer, IDisposable
{
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
        playbackTask = Task.Run(() => PlaybackLoop(cancellation.Token), cancellation.Token);
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
                await playbackTask.WaitAsync(TimeSpan.FromSeconds(2), cancellationToken);
            }
            catch (OperationCanceledException)
            {
            }
            catch (TimeoutException)
            {
            }
        }

        cancellation.Dispose();
        cancellation = null;
        playbackTask = null;
    }

    public void Dispose()
    {
        cancellation?.Cancel();
        cancellation?.Dispose();
    }

    private static async Task PlaybackLoop(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                Console.Beep(1400, 350);
                Console.Beep(900, 250);
            }
            catch
            {
                MessageBeep(0x00000010);
            }

            await Task.Delay(150, cancellationToken);
        }
    }

    [DllImport("user32.dll")]
    private static extern bool MessageBeep(uint type);
}

