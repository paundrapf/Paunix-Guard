using System.Text.Json;
using System.Text.Json.Serialization;
using PaunixGuard.Core.Settings;
using PaunixGuard.Storage.Paths;

namespace PaunixGuard.Storage.Settings;

public sealed class JsonSettingsStore(AppDataPaths paths) : ISettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task<GuardSettings> LoadAsync(CancellationToken cancellationToken)
    {
        paths.EnsureCreated();

        if (!File.Exists(paths.SettingsPath))
        {
            var defaults = new GuardSettings();
            await SaveAsync(defaults, cancellationToken);
            return defaults;
        }

        await using var stream = File.OpenRead(paths.SettingsPath);
        var settings = await JsonSerializer.DeserializeAsync<GuardSettings>(stream, JsonOptions, cancellationToken);
        return settings ?? new GuardSettings();
    }

    public async Task SaveAsync(GuardSettings settings, CancellationToken cancellationToken)
    {
        paths.EnsureCreated();
        var tempPath = paths.SettingsPath + ".tmp";

        await using (var stream = File.Create(tempPath))
        {
            await JsonSerializer.SerializeAsync(stream, settings, JsonOptions, cancellationToken);
        }

        File.Move(tempPath, paths.SettingsPath, true);
    }
}

