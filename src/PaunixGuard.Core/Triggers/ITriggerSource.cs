namespace PaunixGuard.Core.Triggers;

public interface ITriggerSource
{
    string Name { get; }

    Task StartAsync(Func<TriggerSignal, Task> onTrigger, CancellationToken cancellationToken);

    Task StopAsync(CancellationToken cancellationToken);
}

