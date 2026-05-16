using PaunixGuard.Core.Triggers;

namespace PaunixGuard.Core.Events;

public sealed class GuardEvent
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public DateTimeOffset CreatedAt { get; init; }

    public TriggerType TriggerType { get; init; }

    public GuardState.GuardState GuardStateBefore { get; init; }

    public DateTimeOffset? AlarmStartedAt { get; set; }

    public DateTimeOffset? AlarmStoppedAt { get; set; }

    public DisarmMethod DisarmMethod { get; set; } = DisarmMethod.Unknown;

    public EventResolution Resolution { get; set; } = EventResolution.Active;

    public string AppVersion { get; init; } = "0.0.0-dev";

    public string Reason { get; init; } = "";

    public string Source { get; init; } = "";
}

