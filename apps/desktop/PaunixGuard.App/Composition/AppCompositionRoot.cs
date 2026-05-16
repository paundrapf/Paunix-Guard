using System.IO;
using System.Reflection;
using PaunixGuard.App.ViewModels;
using PaunixGuard.Core.Alarm;
using PaunixGuard.Core.GuardState;
using PaunixGuard.Core.Security;
using PaunixGuard.Core.Triggers;
using PaunixGuard.Core.Updates;
using PaunixGuard.Storage.Events;
using PaunixGuard.Storage.Paths;
using PaunixGuard.Storage.Settings;
using PaunixGuard.Updater.Velopack;
using PaunixGuard.Windows;
using PaunixGuard.Windows.Alarm;
using PaunixGuard.Windows.Audio;
using PaunixGuard.Windows.Power;
using PaunixGuard.Windows.Session;
using PaunixGuard.Windows.Triggers;

namespace PaunixGuard.App.Composition;

public sealed class AppCompositionRoot : IAsyncDisposable
{
    public AppCompositionRoot()
    {
        Paths = new AppDataPaths();
        SystemEventRouter = new WindowsSystemEventRouter();

        var settingsStore = new JsonSettingsStore(Paths);
        var eventHistoryStore = new SqliteEventHistoryStore(Paths);
        var pinHasher = new PinHasher();
        var audioService = new WindowsAudioService();
        var soundPlayer = new MediaAlarmSoundPlayer();
        var alarmOrchestrator = new AlarmOrchestrator(audioService, soundPlayer);
        var powerProtection = new WindowsPowerProtectionService();
        var triggerSources = new ITriggerSource[]
        {
            new PowerStatusTriggerSource(),
            new InputActivityTriggerSource()
        };
        var triggerSupervisor = new WindowsTriggerSupervisor(triggerSources, SystemEventRouter);

        GuardEngine = new GuardEngine(
            settingsStore,
            eventHistoryStore,
            pinHasher,
            alarmOrchestrator,
            triggerSupervisor,
            powerProtection,
            new SystemClock(),
            new TriggerPolicy());

        UpdateService = new VelopackUpdateService();
        MainViewModel = new MainViewModel(GuardEngine, UpdateService, eventHistoryStore, SystemEventRouter);
    }

    public AppDataPaths Paths { get; }

    public GuardEngine GuardEngine { get; }

    public IUpdateService UpdateService { get; }

    public WindowsSystemEventRouter SystemEventRouter { get; }

    public MainViewModel MainViewModel { get; }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await UpdateService.InitializeAsync(cancellationToken);
        await GuardEngine.InitializeAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(GuardEngine.Settings.AlarmSound))
        {
            var alarmPath = EnsureEmbeddedAlarmExtracted();
            GuardEngine.Settings.AlarmSound = alarmPath;
            await GuardEngine.SaveSettingsAsync(GuardEngine.Settings, cancellationToken);
        }

        if (GuardEngine.Settings.AutoCheckUpdates)
        {
            _ = CheckForUpdatesAsync(cancellationToken);
        }

        await MainViewModel.RefreshLatestEventAsync(cancellationToken);
    }

    private async Task CheckForUpdatesAsync(CancellationToken ct)
    {
        try
        {
            var result = await UpdateService.CheckAsync(
                GuardEngine.Settings.UpdateChannel, ct);
            if (result.IsAvailable)
            {
                MainViewModel.SetPendingUpdate(result);
            }
        }
        catch
        {
        }
    }

    private static string? EnsureEmbeddedAlarmExtracted()
    {
        var paths = new AppDataPaths();
        paths.EnsureCreated();

        var targetPath = Path.Combine(paths.RootDirectory, "alarm.wav");
        if (File.Exists(targetPath))
        {
            return targetPath;
        }

        var assembly = Assembly.GetAssembly(typeof(MediaAlarmSoundPlayer));
        if (assembly is null)
        {
            return null;
        }

        using var stream = assembly.GetManifestResourceStream("PaunixGuard.Windows.Assets.alarm.wav");
        if (stream is null)
        {
            return null;
        }

        using var fileStream = File.Create(targetPath);
        stream.CopyTo(fileStream);
        return targetPath;
    }

    public async ValueTask DisposeAsync()
    {
        await GuardEngine.ForceStopAsync(Core.Events.DisarmMethod.SystemShutdown, CancellationToken.None);
        SystemEventRouter.Clear();
    }
}
