using System.Windows;
using PaunixGuard.App.ViewModels;
using GuardStateEnum = PaunixGuard.Core.GuardState.GuardState;

namespace PaunixGuard.App.Views;

public partial class AlarmWindow : Window
{
    private readonly MainViewModel viewModel;

    public AlarmWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        this.viewModel = viewModel;
        DataContext = viewModel;
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        if (viewModel.CurrentState == GuardStateEnum.Alarm)
        {
            e.Cancel = true;
            return;
        }

        base.OnClosing(e);
    }

    private void AlarmPinBox_OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
        {
            viewModel.PinInput = AlarmPinBox.Password;
        }
    }
}
