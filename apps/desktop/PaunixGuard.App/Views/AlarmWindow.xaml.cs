using System.Windows;
using PaunixGuard.App.ViewModels;

namespace PaunixGuard.App.Views;

public partial class AlarmWindow : Window
{
    public AlarmWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void AlarmPinBox_OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
        {
            viewModel.PinInput = AlarmPinBox.Password;
        }
    }
}

