using PaunixGuard.Core.Triggers;
using PaunixGuard.Storage.Paths;
using PaunixGuard.Storage.Settings;
using Xunit;

namespace PaunixGuard.App.Tests.Storage;

public sealed class JsonSettingsStoreTests : IDisposable
{
    private readonly string tempDirectory = Path.Combine(Path.GetTempPath(), $"PaunixGuard.Tests.{Guid.NewGuid():N}");

    [Fact]
    public async Task LoadAsync_CorruptJson_ReturnsDefaultsAndKeepsBackup()
    {
        Directory.CreateDirectory(tempDirectory);
        var paths = new AppDataPaths(tempDirectory);
        await File.WriteAllTextAsync(paths.SettingsPath, "{ broken json", CancellationToken.None);
        var store = new JsonSettingsStore(paths);

        var settings = await store.LoadAsync(CancellationToken.None);

        Assert.Contains(TriggerType.ChargerUnplugged, settings.EnabledTriggers);
        Assert.True(File.Exists(paths.SettingsPath));
        Assert.Single(Directory.GetFiles(tempDirectory, "settings.json.corrupt-*"));
    }

    [Fact]
    public async Task LoadAsync_NullTriggerSet_NormalizesDefaults()
    {
        Directory.CreateDirectory(tempDirectory);
        var paths = new AppDataPaths(tempDirectory);
        await File.WriteAllTextAsync(paths.SettingsPath, """
            {
              "EnabledTriggers": null,
              "InputWarningSeconds": 0,
              "UpdateChannel": ""
            }
            """, CancellationToken.None);
        var store = new JsonSettingsStore(paths);

        var settings = await store.LoadAsync(CancellationToken.None);

        Assert.Contains(TriggerType.DesktopSwitch, settings.EnabledTriggers);
        Assert.Equal(1, settings.InputWarningSeconds);
        Assert.Equal("stable", settings.UpdateChannel);
    }

    public void Dispose()
    {
        if (Directory.Exists(tempDirectory))
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }
}
