using PaunixGuard.Core.Triggers;
using PaunixGuard.Windows.Session;

namespace PaunixGuard.Windows.Triggers;

public sealed class WindowsTriggerSupervisor(IEnumerable<ITriggerSource> triggerSources, WindowsSystemEventRouter systemEventRouter)
    : ITriggerSupervisor
{
    private readonly IReadOnlyList<ITriggerSource> triggerSources = triggerSources.ToArray();
    private Func<TriggerSignal, Task>? onTrigger;

    public async Task StartAsync(Func<TriggerSignal, Task> onTrigger, CancellationToken cancellationToken)
    {
        this.onTrigger = onTrigger;
        systemEventRouter.Triggered += HandleSystemTriggerAsync;

        foreach (var triggerSource in triggerSources)
        {
            await triggerSource.StartAsync(onTrigger, cancellationToken);
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        systemEventRouter.Triggered -= HandleSystemTriggerAsync;

        foreach (var triggerSource in triggerSources)
        {
            await triggerSource.StopAsync(cancellationToken);
        }

        onTrigger = null;
    }

    private void HandleSystemTriggerAsync(object? sender, TriggerSignal signal)
    {
        _ = Task.Run(async () =>
        {
            if (onTrigger is not null)
            {
                await onTrigger(signal);
            }
        });
    }
}

