using PaunixGuard.Core;

namespace PaunixGuard.App.Tests.Fakes;

internal sealed class FakeClock : IClock
{
    public DateTimeOffset UtcNow { get; set; } = new(2026, 5, 16, 0, 0, 0, TimeSpan.Zero);
}

