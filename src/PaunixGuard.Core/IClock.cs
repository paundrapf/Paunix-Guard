namespace PaunixGuard.Core;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}

