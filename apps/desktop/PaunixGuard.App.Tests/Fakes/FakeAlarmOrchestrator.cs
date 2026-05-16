using PaunixGuard.Core.Alarm;
using PaunixGuard.Core.Events;
using PaunixGuard.Core.Settings;
using PaunixGuard.Core.Triggers;

namespace PaunixGuard.App.Tests.Fakes;

internal sealed class FakeAlarmOrchestrator : IAlarmOrchestrator
{
    public int StartCount { get; private set; }

    public int StopCount { get; private set; }

    public Task StartAsync(TriggerSignal signal, GuardSettings settings, GuardEvent guardEvent, CancellationToken cancellationToken)
    {
        StartCount++;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        StopCount++;
        return Task.CompletedTask;
    }
}

