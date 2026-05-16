using System.Windows;
using PaunixGuard.App.Composition;
using PaunixGuard.App.Views;
using Forms = System.Windows.Forms;

namespace PaunixGuard.App.Tray;

public sealed class TrayController(MainWindow mainWindow, AppCompositionRoot compositionRoot) : IDisposable
{
    private Forms.NotifyIcon? notifyIcon;

    public void Initialize()
    {
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("Open Paunix Guard", null, (_, _) => ShowMainWindow());
        menu.Items.Add("Start Guard", null, async (_, _) => await TryExecuteAsync("Start Guard", compositionRoot.MainViewModel.StartGuardCommand));
        menu.Items.Add("Test Alarm", null, async (_, _) => await TryExecuteAsync("Test Alarm", compositionRoot.MainViewModel.TestAlarmCommand));
        menu.Items.Add("Check Updates", null, async (_, _) => await TryExecuteAsync("Check Updates", compositionRoot.MainViewModel.CheckUpdatesCommand));
        menu.Items.Add("Quit", null, (_, _) => Quit());

        notifyIcon = new Forms.NotifyIcon
        {
            Text = "Paunix Guard",
            Icon = System.Drawing.Icon.ExtractAssociatedIcon(System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? ""),
            Visible = true,
            ContextMenuStrip = menu
        };

        notifyIcon.DoubleClick += (_, _) => ShowMainWindow();
    }

    public void Dispose()
    {
        notifyIcon?.Dispose();
    }

    private static async Task TryExecuteAsync(string label, System.Windows.Input.ICommand command)
    {
        try
        {
            await command.ExecuteAsync();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Error executing '{label}': {ex.Message}",
                "Paunix Guard",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }

    private void ShowMainWindow()
    {
        mainWindow.Show();
        mainWindow.WindowState = WindowState.Normal;
        mainWindow.Activate();
    }

    private static void Quit()
    {
        System.Windows.Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;
        System.Windows.Application.Current.Shutdown();
    }
}

