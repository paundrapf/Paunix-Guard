using System.Runtime.InteropServices;
using PaunixGuard.Core.Triggers;

namespace PaunixGuard.Windows.Triggers;

public sealed class InputActivityTriggerSource(TimeSpan? pollingInterval = null) : ITriggerSource
{
    private readonly TimeSpan interval = pollingInterval ?? TimeSpan.FromMilliseconds(250);
    private CancellationTokenSource? cancellation;
    private Task? pollingTask;

    public string Name => "WindowsInputActivity";

    public Task StartAsync(Func<TriggerSignal, Task> onTrigger, CancellationToken cancellationToken)
    {
        if (pollingTask is { IsCompleted: false })
        {
            return Task.CompletedTask;
        }

        cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        pollingTask = Task.Run(() => PollAsync(onTrigger, cancellation.Token), cancellation.Token);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (cancellation is null)
        {
            return;
        }

        await cancellation.CancelAsync();
        if (pollingTask is not null)
        {
            try
            {
                await pollingTask.WaitAsync(TimeSpan.FromSeconds(2), cancellationToken);
            }
            catch
            {
            }
        }

        cancellation.Dispose();
        cancellation = null;
        pollingTask = null;
    }

    private async Task PollAsync(Func<TriggerSignal, Task> onTrigger, CancellationToken cancellationToken)
    {
        var baseline = GetLastInputTick();
        await Task.Delay(750, cancellationToken);

        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(interval, cancellationToken);
            var current = GetLastInputTick();

            if (current > baseline)
            {
                await onTrigger(TriggerSignal.Create(
                    TriggerType.InputActivity,
                    "Keyboard, mouse, or trackpad activity occurred while guard mode was armed.",
                    Name,
                    DateTimeOffset.UtcNow));
                baseline = current;
            }
        }
    }

    private static uint GetLastInputTick()
    {
        var info = new LastInputInfo
        {
            Size = (uint)Marshal.SizeOf<LastInputInfo>()
        };

        return GetLastInputInfo(ref info) ? info.Time : unchecked((uint)Environment.TickCount);
    }

    [DllImport("user32.dll")]
    private static extern bool GetLastInputInfo(ref LastInputInfo info);

    [StructLayout(LayoutKind.Sequential)]
    private struct LastInputInfo
    {
        public uint Size;
        public uint Time;
    }
}
