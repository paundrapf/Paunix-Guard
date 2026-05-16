using System.Runtime.InteropServices;
using PaunixGuard.Core.Triggers;
using PaunixGuard.Windows.Power;

namespace PaunixGuard.Windows.Triggers;

public sealed class PowerStatusTriggerSource(TimeSpan? pollingInterval = null) : ITriggerSource
{
    private readonly TimeSpan interval = pollingInterval ?? TimeSpan.FromSeconds(1);
    private CancellationTokenSource? cancellation;
    private Task? pollingTask;

    public string Name => "WindowsPowerStatus";

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
        var baseline = ReadAcLineStatus();

        while (baseline == AcLineStatus.Unknown && !cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(interval, cancellationToken);
            baseline = ReadAcLineStatus();
        }

        if (baseline == AcLineStatus.Unknown)
        {
            return;
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(interval, cancellationToken);
            var current = ReadAcLineStatus();

            if (current == AcLineStatus.Unknown)
            {
                continue;
            }

            if (baseline == AcLineStatus.Online && current == AcLineStatus.Offline)
            {
                await onTrigger(TriggerSignal.Create(
                    TriggerType.ChargerUnplugged,
                    "Power adapter was unplugged while guard mode was armed.",
                    Name,
                    DateTimeOffset.UtcNow));
                return;
            }

            baseline = current;
        }
    }

    private static AcLineStatus ReadAcLineStatus()
    {
        return GetSystemPowerStatus(out var status)
            ? (AcLineStatus)status.ACLineStatus
            : AcLineStatus.Unknown;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetSystemPowerStatus(out SystemPowerStatus status);

    [StructLayout(LayoutKind.Sequential)]
    private struct SystemPowerStatus
    {
        public byte ACLineStatus;
        public byte BatteryFlag;
        public byte BatteryLifePercent;
        public byte SystemStatusFlag;
        public uint BatteryLifeTime;
        public uint BatteryFullLifeTime;
    }
}

