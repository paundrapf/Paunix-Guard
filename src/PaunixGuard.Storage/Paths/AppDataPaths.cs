namespace PaunixGuard.Storage.Paths;

public sealed class AppDataPaths
{
    public AppDataPaths(string? rootDirectory = null)
    {
        RootDirectory = rootDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PaunixGuard");
    }

    public string RootDirectory { get; }

    public string SettingsPath => Path.Combine(RootDirectory, "settings.json");

    public string EventsDatabasePath => Path.Combine(RootDirectory, "events.db");

    public void EnsureCreated()
    {
        Directory.CreateDirectory(RootDirectory);
    }
}

