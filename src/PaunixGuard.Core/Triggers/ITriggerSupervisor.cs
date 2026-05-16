namespace PaunixGuard.Core.Triggers;

public interface ITriggerSupervisor
{
    Task StartAsync(Func<TriggerSignal, Task> onTrigger, CancellationToken cancellationToken);

    Task StopAsync(CancellationToken cancellationToken);
}

