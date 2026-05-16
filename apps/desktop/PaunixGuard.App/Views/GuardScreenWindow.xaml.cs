using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using PaunixGuard.App.ViewModels;

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
        RootGrid.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x1A, 0x1D, 0x23));
        StateText.Text = "ARMED";
        StateText.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x38, 0xB2, 0x49));
        MessageText.Text = "Your laptop is protected. Alarm will sound if someone touches it.";
    }

    public void SetWarningVisual()
    {
        StateText.Text = "TYPE YOUR PIN";
        StateText.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0xFF, 0x99, 0x00));
        MessageText.Text = "Keyboard activity detected. Enter your PIN to cancel.";

        warningFlashTimer?.Stop();
        warningFlash = false;
        warningFlashTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(400)
        };

        warningFlashTimer.Tick += (_, _) =>
        {
            warningFlash = !warningFlash;
            RootGrid.Background = warningFlash
                ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x33, 0x26, 0x00))
                : new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x1A, 0x1D, 0x23));
        };

        warningFlashTimer.Start();
    }

    private void GuardPinBox_OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.PinInput = GuardPinBox.Password;
        }
    }
}
