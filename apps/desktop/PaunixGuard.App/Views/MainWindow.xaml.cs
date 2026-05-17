using System.ComponentModel;
using System.Windows;
using System.Windows.Interop;
using PaunixGuard.App.Composition;
using PaunixGuard.App.ViewModels;
using PaunixGuard.Core.GuardState;
using PaunixGuard.Windows.Input;

namespace PaunixGuard.App.Views;

public partial class MainWindow : Window
{
    private readonly AppCompositionRoot compositionRoot;
    private readonly List<GuardScreenWindow> guardScreens = [];
    private AlarmWindow? alarmWindow;
    private KeyboardInterceptor? kbInterceptor;

    public MainWindow(AppCompositionRoot compositionRoot)
    {
        InitializeComponent();
        this.compositionRoot = compositionRoot;
        DataContext = compositionRoot.MainViewModel;
        compositionRoot.GuardEngine.StateChanged += OnGuardStateChanged;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        var source = (HwndSource)PresentationSource.FromVisual(this);
        source.AddHook(WndProc);
        compositionRoot.SystemEventRouter.AttachWindow(new WindowInteropHelper(this).Handle);
        kbInterceptor = new KeyboardInterceptor();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (System.Windows.Application.Current.ShutdownMode != ShutdownMode.OnExplicitShutdown)
        {
            e.Cancel = true;
            Hide();
            return;
        }

        kbInterceptor?.Dispose();
        base.OnClosing(e);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        return compositionRoot.SystemEventRouter.HandleWindowMessage(hwnd, msg, wParam, lParam, ref handled);
    }

    private void PinBox_OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
        {
            viewModel.PinInput = PinBox.Password;
        }
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        if (compositionRoot.GuardEngine.CurrentState != GuardState.Idle)
        {
            System.Windows.MessageBox.Show(
                "Disarm Paunix Guard before changing settings.",
                "Paunix Guard",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        var window = new SettingsWindow(compositionRoot.MainViewModel);
        window.ShowDialog();
    }

    private void History_Click(object sender, RoutedEventArgs e)
    {
        var window = new HistoryWindow(compositionRoot.MainViewModel);
        window.ShowDialog();
    }

    private async void ResetPin_Click(object sender, RoutedEventArgs e)
    {
        if (compositionRoot.GuardEngine.CurrentState != GuardState.Idle)
        {
            System.Windows.MessageBox.Show(
                "Disarm Paunix Guard before resetting the PIN.",
                "Paunix Guard",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        if (string.IsNullOrWhiteSpace(compositionRoot.MainViewModel.PinInput))
        {
            System.Windows.MessageBox.Show(
                "Enter your current PIN in the main PIN box before resetting it.",
                "Reset PIN",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        var result = System.Windows.MessageBox.Show(
            "This will erase your current PIN. You will need to set a new one.",
            "Reset PIN",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes) return;

        try
        {
            var reset = await compositionRoot.GuardEngine.ResetPinAsync(
                compositionRoot.MainViewModel.PinInput,
                CancellationToken.None);
            if (!reset)
            {
                System.Windows.MessageBox.Show(
                    "Current PIN is invalid.",
                    "Reset PIN",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            compositionRoot.MainViewModel.PinInput = "";
            PinBox.Password = "";
            System.Windows.MessageBox.Show(
                "PIN has been reset. The setup wizard will open.",
                "Paunix Guard",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            var wizard = new SetupWizardWindow();
            if (wizard.ShowDialog() == true || !string.IsNullOrWhiteSpace(wizard.WizardPin))
            {
                await compositionRoot.GuardEngine.SetPinAsync(wizard.WizardPin, CancellationToken.None);
                compositionRoot.MainViewModel.PinInput = "";
                PinBox.Password = "";
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Failed to reset PIN: {ex.Message}", "Paunix Guard", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnGuardStateChanged(object? sender, GuardStateChangedEventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            if (e.CurrentState == GuardState.Alarm)
            {
                EnsureGuardScreens();
                SetAllGuardScreens(g => g.SetAlarmVisual());
                kbInterceptor?.SetArmed(true);
                alarmWindow ??= new AlarmWindow(compositionRoot.MainViewModel);
                alarmWindow.Show();
                alarmWindow.Activate();
                Hide();
                return;
            }

            if (e.CurrentState is GuardState.Armed or GuardState.Arming)
            {
                EnsureGuardScreens();
                SetAllGuardScreens(g => g.SetArmedVisual());
                kbInterceptor?.SetArmed(true);
                Hide();
                return;
            }

            if (e.CurrentState == GuardState.Warning)
            {
                EnsureGuardScreens();
                SetAllGuardScreens(g => g.SetWarningVisual());
                kbInterceptor?.SetArmed(true);
                Hide();
                return;
            }

            if (e.CurrentState == GuardState.Idle)
            {
                kbInterceptor?.SetArmed(false);
                CloseGuardScreens();

                if (alarmWindow is not null)
                {
                    alarmWindow.Close();
                    alarmWindow = null;
                }

                Show();
                Activate();
            }
        });
    }

    private void EnsureGuardScreens()
    {
        if (guardScreens.Count > 0)
        {
            return;
        }

        compositionRoot.SystemEventRouter.ClearGuardScreenHandles();
        var screens = System.Windows.Forms.Screen.AllScreens;
        for (var i = 0; i < screens.Length; i++)
        {
            var screen = screens[i];
            var guard = new GuardScreenWindow(compositionRoot.MainViewModel);
            guard.WindowStartupLocation = WindowStartupLocation.Manual;
            guard.Left = screen.Bounds.Left;
            guard.Top = screen.Bounds.Top;
            guard.Width = screen.Bounds.Width;
            guard.Height = screen.Bounds.Height;
            guard.WindowState = WindowState.Normal;
            guard.ResizeMode = ResizeMode.NoResize;
            guard.Topmost = true;
            guard.ShowInTaskbar = false;
            guard.Show();

            if (guard.IsLoaded)
            {
                RegisterGuardScreenHandle(guard);
            }
            else
            {
                var captured = guard;
                guard.SourceInitialized += (_, _) => RegisterGuardScreenHandle(captured);
            }

            guardScreens.Add(guard);
        }
    }

    private void RegisterGuardScreenHandle(GuardScreenWindow guard)
    {
        var helper = new WindowInteropHelper(guard);
        compositionRoot.SystemEventRouter.SetGuardScreenHandle(helper.Handle);
    }

    private void SetAllGuardScreens(Action<GuardScreenWindow> action)
    {
        foreach (var guard in guardScreens)
        {
            action(guard);
        }
    }

    private void CloseGuardScreens()
    {
        foreach (var guard in guardScreens)
        {
            guard.Close();
        }

        guardScreens.Clear();
        compositionRoot.SystemEventRouter.ClearGuardScreenHandles();
    }
}
