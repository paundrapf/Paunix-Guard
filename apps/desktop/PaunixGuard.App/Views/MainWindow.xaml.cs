using System.ComponentModel;
using System.Windows;
using System.Windows.Interop;
using PaunixGuard.App.Composition;
using PaunixGuard.App.ViewModels;
using PaunixGuard.Core.GuardState;

namespace PaunixGuard.App.Views;

public partial class MainWindow : Window
{
    private readonly AppCompositionRoot compositionRoot;
    private AlarmWindow? alarmWindow;
    private GuardScreenWindow? guardScreen;

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
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (System.Windows.Application.Current.ShutdownMode != ShutdownMode.OnExplicitShutdown)
        {
            e.Cancel = true;
            Hide();
            return;
        }

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

    private void OnGuardStateChanged(object? sender, GuardStateChangedEventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            if (e.CurrentState == GuardState.Alarm)
            {
                CloseGuardScreen();
                alarmWindow ??= new AlarmWindow(compositionRoot.MainViewModel);
                alarmWindow.Show();
                alarmWindow.Activate();
                return;
            }

            if (e.CurrentState is GuardState.Armed or GuardState.Arming)
            {
                guardScreen ??= new GuardScreenWindow(compositionRoot.MainViewModel);
                guardScreen.Show();
                guardScreen.Activate();
                Hide();
                return;
            }

            if (e.CurrentState == GuardState.Idle)
            {
                CloseGuardScreen();
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

    private void CloseGuardScreen()
    {
        if (guardScreen is not null)
        {
            guardScreen.Close();
            guardScreen = null;
        }
    }
}
