using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;

namespace PaunixGuard.App.Views;

public partial class SetupWizardWindow : Window
{
    private int step;

    public string WizardPin { get; private set; } = "";

    public Core.Settings.GuardSettings ResultSettings { get; } = new();

    public SetupWizardWindow()
    {
        InitializeComponent();
        UpdateButtons();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        var hwnd = new WindowInteropHelper(this).Handle;
        var menu = GetSystemMenu(hwnd, false);
        RemoveMenu(menu, SC_CLOSE, MF_BYCOMMAND);
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (DialogResult != true)
        {
            e.Cancel = true;
            return;
        }

        base.OnClosing(e);
    }

    private void Back_Click(object sender, RoutedEventArgs e)
    {
        if (step <= 0) return;
        HideStep(step);
        step--;
        ShowStep(step);
        UpdateButtons();
    }

    private void Next_Click(object sender, RoutedEventArgs e)
    {
        if (step == 1)
        {
            var pin = WizardPinBox.Password;
            var confirm = WizardPinConfirmBox.Password;

            if (pin.Length < 4)
            {
                PinError.Text = "Use at least 4 characters.";
                PinError.Visibility = Visibility.Visible;
                return;
            }

            if (pin != confirm)
            {
                PinError.Text = "PINs do not match.";
                PinError.Visibility = Visibility.Visible;
                return;
            }

            PinError.Visibility = Visibility.Collapsed;
            WizardPin = pin;
        }

        if (step == 2)
        {
            ResultSettings.ForceVolumeEnabled = ChkForceVolume.IsChecked == true;
            ResultSettings.RestoreAudioAfterDisarm = ChkRestoreAudio.IsChecked == true;
            ResultSettings.KeepSystemAwakeWhileArmed = ChkKeepAwake.IsChecked == true;
            ResultSettings.BlockShutdownWhileArmed = ChkBlockShutdown.IsChecked == true;
        }

        if (step >= 3)
        {
            DialogResult = true;
            Close();
            return;
        }

        HideStep(step);
        step++;
        ShowStep(step);
        UpdateButtons();
    }

    private void TestAlarm_Click(object sender, RoutedEventArgs e)
    {
        BtnTestAlarm.IsEnabled = false;
        BtnTestAlarm.Content = "Test requested. Continue setup.";
    }

    private void HideStep(int stepIndex)
    {
        GetStepPanel(stepIndex).Visibility = Visibility.Collapsed;
    }

    private void ShowStep(int stepIndex)
    {
        GetStepPanel(stepIndex).Visibility = Visibility.Visible;
    }

    private System.Windows.Controls.Panel GetStepPanel(int stepIndex)
    {
        return stepIndex switch
        {
            0 => Step1_Welcome,
            1 => Step2_Pin,
            2 => Step3_Settings,
            3 => Step4_Finish,
            _ => throw new ArgumentOutOfRangeException(nameof(stepIndex))
        };
    }

    private void UpdateButtons()
    {
        BtnBack.Visibility = step > 0 ? Visibility.Visible : Visibility.Collapsed;
        BtnNext.Content = step switch
        {
            3 => "Finish",
            _ => "Next"
        };
    }

    private const int SC_CLOSE = 0xF060;
    private const int MF_BYCOMMAND = 0;

    [DllImport("user32.dll")]
    private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

    [DllImport("user32.dll")]
    private static extern int RemoveMenu(IntPtr hMenu, int nPosition, int wFlags);
}
