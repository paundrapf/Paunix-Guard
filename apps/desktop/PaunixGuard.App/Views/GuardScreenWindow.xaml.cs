using System.Windows;
using PaunixGuard.App.ViewModels;

namespace PaunixGuard.App.Views;

public partial class GuardScreenWindow : Window
{
    public GuardScreenWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void GuardPinBox_OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
        {
            viewModel.PinInput = GuardPinBox.Password;
        }
    }
}
