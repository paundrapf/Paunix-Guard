namespace PaunixGuard.Core.Settings;

public interface ISettingsStore
{
    Task<GuardSettings> LoadAsync(CancellationToken cancellationToken);

    Task SaveAsync(GuardSettings settings, CancellationToken cancellationToken);
}

