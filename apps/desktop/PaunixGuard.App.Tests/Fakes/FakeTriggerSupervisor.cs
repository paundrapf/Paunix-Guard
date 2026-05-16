using PaunixGuard.Core.Triggers;

namespace PaunixGuard.App.Tests.Fakes;

internal sealed class FakeTriggerSupervisor : ITriggerSupervisor
{
    private Func<TriggerSignal, Task>? onTrigger;

    public bool IsStarted { get; private set; }

    public Task StartAsync(Func<TriggerSignal, Task> onTrigger, CancellationToken cancellationToken)
    {
        this.onTrigger = onTrigger;
        IsStarted = true;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        IsStarted = false;
        return Task.CompletedTask;
    }

    public Task RaiseAsync(TriggerSignal signal)
    {
        return onTrigger?.Invoke(signal) ?? Task.CompletedTask;
    }
}

