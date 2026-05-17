using System.Windows;
using PaunixGuard.App.ViewModels;

namespace PaunixGuard.App.Views;

public partial class SettingsWindow : Window
{
    private readonly MainViewModel viewModel;
    private readonly Core.Settings.GuardSettings draftSettings;

    public SettingsWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        this.viewModel = viewModel;

        draftSettings = viewModel.Settings.Clone();
        var s = draftSettings;

        ChkCharger.IsChecked = s.EnabledTriggers.Contains(Core.Triggers.TriggerType.ChargerUnplugged);
        ChkInput.IsChecked = s.EnabledTriggers.Contains(Core.Triggers.TriggerType.InputActivity);
        ChkLidClose.IsChecked = s.EnabledTriggers.Contains(Core.Triggers.TriggerType.LidClosed);
        ChkSleep.IsChecked = s.EnabledTriggers.Contains(Core.Triggers.TriggerType.SleepAttempt);
        ChkShutdown.IsChecked = s.EnabledTriggers.Contains(Core.Triggers.TriggerType.ShutdownAttempt);

        ChkForceVolume.IsChecked = s.ForceVolumeEnabled;
        ChkRestoreAudio.IsChecked = s.RestoreAudioAfterDisarm;
        TxtArmingDelay.Text = s.ArmingDelaySeconds.ToString();
        TxtAlarmSound.Text = s.AlarmSound ?? "";

        ChkKeepAwake.IsChecked = s.KeepSystemAwakeWhileArmed;
        ChkBlockShutdown.IsChecked = s.BlockShutdownWhileArmed;
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        var s = draftSettings;

        SetTrigger(Core.Triggers.TriggerType.ChargerUnplugged, ChkCharger.IsChecked == true);
        SetTrigger(Core.Triggers.TriggerType.InputActivity, ChkInput.IsChecked == true);
        SetTrigger(Core.Triggers.TriggerType.LidClosed, ChkLidClose.IsChecked == true);
        SetTrigger(Core.Triggers.TriggerType.SleepAttempt, ChkSleep.IsChecked == true);
        SetTrigger(Core.Triggers.TriggerType.ShutdownAttempt, ChkShutdown.IsChecked == true);

        s.ForceVolumeEnabled = ChkForceVolume.IsChecked == true;
        s.RestoreAudioAfterDisarm = ChkRestoreAudio.IsChecked == true;
        s.KeepSystemAwakeWhileArmed = ChkKeepAwake.IsChecked == true;
        s.BlockShutdownWhileArmed = ChkBlockShutdown.IsChecked == true;

        if (int.TryParse(TxtArmingDelay.Text, out var delay) && delay >= 0)
        {
            s.ArmingDelaySeconds = delay;
        }

        if (!string.IsNullOrWhiteSpace(TxtAlarmSound.Text))
        {
            s.AlarmSound = TxtAlarmSound.Text;
        }

        try
        {
            await viewModel.GuardEngine?.SaveSettingsAsync(s, CancellationToken.None)!;
            DialogResult = true;
            Close();
        }
        catch
        {
            DialogResult = false;
            Close();
        }
    }

    private void SetTrigger(Core.Triggers.TriggerType type, bool enabled)
    {
        var s = draftSettings;
        if (enabled)
            s.EnabledTriggers.Add(type);
        else
            s.EnabledTriggers.Remove(type);
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
