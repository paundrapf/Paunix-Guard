namespace PaunixGuard.Core.Triggers;

public sealed record TriggerSignal(
    TriggerType Type,
    DateTimeOffset OccurredAt,
    string Reason,
    string Source,
    bool ContainsSensitiveContent = false)
{
    public static TriggerSignal Create(TriggerType type, string reason, string source, DateTimeOffset occurredAt)
    {
        return new TriggerSignal(type, occurredAt, reason, source);
    }
}

