using PaunixGuard.Core.GuardState;
using PaunixGuard.Core.Settings;

namespace PaunixGuard.App.Tests.Fakes;

internal sealed class FakePowerProtectionService : IPowerProtectionService
{
    public int EnableCount { get; private set; }

    public int DisableCount { get; private set; }

    public Task EnableAsync(GuardSettings settings, CancellationToken cancellationToken)
    {
        EnableCount++;
        return Task.CompletedTask;
    }

    public Task DisableAsync(CancellationToken cancellationToken)
    {
        DisableCount++;
        return Task.CompletedTask;
    }
}

