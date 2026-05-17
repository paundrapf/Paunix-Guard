using System.IO;
using System.Windows;
using System.Windows.Threading;
using PaunixGuard.App.Composition;
using PaunixGuard.App.Tray;
using PaunixGuard.App.Views;

namespace PaunixGuard.App;

public partial class App : System.Windows.Application
{
    private AppCompositionRoot? compositionRoot;
    private TrayController? trayController;

    public App()
    {
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        DispatcherUnhandledException += OnDispatcherUnhandledException;
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        _ = StartupAsync();
    }

    private async Task StartupAsync()
    {
        try
        {
            compositionRoot = new AppCompositionRoot();
            await compositionRoot.InitializeAsync(CancellationToken.None);

            var mainWindow = new MainWindow(compositionRoot);
            MainWindow = mainWindow;
            trayController = new TrayController(mainWindow, compositionRoot);
            trayController.Initialize();

            mainWindow.Show();
            mainWindow.Activate();
            ShutdownMode = ShutdownMode.OnLastWindowClose;

            if (!compositionRoot.GuardEngine.HasPin)
            {
                await RunFirstSetupAsync(mainWindow, CancellationToken.None);
            }

            if (!compositionRoot.GuardEngine.HasPin)
            {
                ShutdownMode = ShutdownMode.OnExplicitShutdown;
                Shutdown();
                return;
            }

            mainWindow.Show();
            mainWindow.WindowState = WindowState.Normal;
            mainWindow.Activate();
        }
        catch (Exception ex)
        {
            LogError("Startup", ex);
            System.Windows.MessageBox.Show(
                $"Paunix Guard failed to start: {ex.Message}",
                "Paunix Guard Fatal Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown();
        }
    }

    private async Task RunFirstSetupAsync(Window owner, CancellationToken cancellationToken)
    {
        if (compositionRoot is null)
        {
            throw new InvalidOperationException("Application composition is not initialized.");
        }

        while (!compositionRoot.GuardEngine.HasPin)
        {
            var wizard = new SetupWizardWindow
            {
                Owner = owner,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            var result = wizard.ShowDialog();

            if (result != true || string.IsNullOrWhiteSpace(wizard.WizardPin))
            {
                return;
            }

            await compositionRoot.GuardEngine.SetPinAsync(wizard.WizardPin, cancellationToken);

            var settings = wizard.ResultSettings;
            var current = compositionRoot.GuardEngine.Settings;
            current.ForceVolumeEnabled = settings.ForceVolumeEnabled;
            current.RestoreAudioAfterDisarm = settings.RestoreAudioAfterDisarm;
            current.KeepSystemAwakeWhileArmed = settings.KeepSystemAwakeWhileArmed;
            current.BlockShutdownWhileArmed = settings.BlockShutdownWhileArmed;
            await compositionRoot.GuardEngine.SaveSettingsAsync(current, cancellationToken);
            compositionRoot.MainViewModel.NotifyPinChanged();
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        try
        {
            trayController?.Dispose();

            if (compositionRoot is not null)
            {
                compositionRoot.DisposeAsync().AsTask().GetAwaiter().GetResult();
            }
        }
        catch (Exception ex)
        {
            LogError("Exit", ex);
        }

        base.OnExit(e);
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            LogError("UnhandledDomain", ex);
        }
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        LogError("UnobservedTask", e.Exception);
        e.SetObserved();
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        LogError("Dispatcher", e.Exception);

        if (e.Exception is not InvalidOperationException)
        {
            e.Handled = true;
        }
    }

    private static void LogError(string category, Exception ex)
    {
        try
        {
            var logDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "PaunixGuard");

            Directory.CreateDirectory(logDir);

            var logPath = Path.Combine(logDir, "error.log");
            var line = $"{DateTimeOffset.UtcNow:O} [{category}] {ex}";

            File.AppendAllText(logPath, line + Environment.NewLine);
        }
        catch
        {
        }
    }
}
