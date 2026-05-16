using PaunixGuard.Core.Events;
using PaunixGuard.Core.Settings;
using PaunixGuard.Core.Triggers;

namespace PaunixGuard.Core.Alarm;

public interface IAlarmOrchestrator
{
    Task StartAsync(TriggerSignal signal, GuardSettings settings, GuardEvent guardEvent, CancellationToken cancellationToken);

    Task StopAsync(CancellationToken cancellationToken);
}

