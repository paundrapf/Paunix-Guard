using PaunixGuard.Core;

namespace PaunixGuard.Windows;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}

