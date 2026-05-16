using PaunixGuard.Core.Settings;

namespace PaunixGuard.App.Tests.Fakes;

internal sealed class FakeSettingsStore(GuardSettings? initialSettings = null) : ISettingsStore
{
    public GuardSettings Settings { get; private set; } = initialSettings ?? new GuardSettings();

    public Task<GuardSettings> LoadAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(Settings);
    }

    public Task SaveAsync(GuardSettings settings, CancellationToken cancellationToken)
    {
        Settings = settings;
        return Task.CompletedTask;
    }
}

