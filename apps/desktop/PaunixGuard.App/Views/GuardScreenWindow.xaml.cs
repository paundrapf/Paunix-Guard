using System.ComponentModel;
using System.Windows;
using System.Windows.Threading;
using PaunixGuard.App.ViewModels;
using GuardStateEnum = PaunixGuard.Core.GuardState.GuardState;

namespace PaunixGuard.App.Views;

public partial class GuardScreenWindow : Window
{
    private readonly MainViewModel viewModel;
    private DispatcherTimer? warningFlashTimer;
    private bool warningFlash;

    public GuardScreenWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        this.viewModel = viewModel;
        DataContext = viewModel;
    }

    public void SetArmedVisual()
    {
        warningFlashTimer?.Stop();
        warningFlashTimer = null;
        RootGrid.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x1A, 0x1D, 0x23));
        StateText.Text = "ARMED";
        StateText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x38, 0xB2, 0x49));
        MessageText.Text = "Your laptop is protected. Alarm will sound if someone touches it.";
    }

    public void SetWarningVisual()
    {
        StateText.Text = "TYPE YOUR PIN";
        StateText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xFF, 0x99, 0x00));
        MessageText.Text = "Keyboard activity detected. Enter your PIN to cancel.";

        warningFlashTimer?.Stop();
        warningFlash = false;
        warningFlashTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(400)
        };

        warningFlashTimer.Tick += (_, _) =>
        {
            if (RootGrid is null) return;
            warningFlash = !warningFlash;
            RootGrid.Background = warningFlash
                ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x33, 0x26, 0x00))
                : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x1A, 0x1D, 0x23));
        };

        warningFlashTimer.Start();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        warningFlashTimer?.Stop();
        warningFlashTimer = null;

        if (viewModel.CurrentState is GuardStateEnum.Armed
            or GuardStateEnum.Arming
            or GuardStateEnum.Warning)
        {
            e.Cancel = true;
            return;
        }

        base.OnClosing(e);
    }

    private void GuardPinBox_OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.PinInput = GuardPinBox.Password;
        }
    }
}
