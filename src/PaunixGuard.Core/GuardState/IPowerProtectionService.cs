using PaunixGuard.Core.Settings;

namespace PaunixGuard.Core.GuardState;

public interface IPowerProtectionService
{
    Task EnableAsync(GuardSettings settings, CancellationToken cancellationToken);

    Task DisableAsync(CancellationToken cancellationToken);
}

