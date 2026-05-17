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

        try
        {
            await using var stream = File.OpenRead(paths.SettingsPath);
            var settings = await JsonSerializer.DeserializeAsync<GuardSettings>(stream, JsonOptions, cancellationToken)
                ?? new GuardSettings();
            settings.Normalize();
            return settings;
        }
        catch (JsonException)
        {
            return await RecoverWithDefaultsAsync(cancellationToken);
        }
        catch (IOException)
        {
            return await RecoverWithDefaultsAsync(cancellationToken);
        }
    }

    public async Task SaveAsync(GuardSettings settings, CancellationToken cancellationToken)
    {
        paths.EnsureCreated();
        settings.Normalize();
        var tempPath = $"{paths.SettingsPath}.{Guid.NewGuid():N}.tmp";

        await using (var stream = File.Create(tempPath))
        {
            await JsonSerializer.SerializeAsync(stream, settings, JsonOptions, cancellationToken);
        }

        File.Move(tempPath, paths.SettingsPath, true);
    }

    private async Task<GuardSettings> RecoverWithDefaultsAsync(CancellationToken cancellationToken)
    {
        TryMoveCorruptSettings();
        var defaults = new GuardSettings();
        await SaveAsync(defaults, cancellationToken);
        return defaults;
    }

    private void TryMoveCorruptSettings()
    {
        try
        {
            if (!File.Exists(paths.SettingsPath))
            {
                return;
            }

            var corruptPath = $"{paths.SettingsPath}.corrupt-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}";
            File.Move(paths.SettingsPath, corruptPath, false);
        }
        catch
        {
        }
    }
}
