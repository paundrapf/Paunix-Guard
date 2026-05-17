using System.Windows.Input;
using PaunixGuard.App.ViewModels;

namespace PaunixGuard.App.Tray;

public static class CommandExtensions
{
    public static async Task ExecuteAsync(this ICommand command)
    {
        if (!command.CanExecute(null))
        {
            return;
        }

        if (command is RelayCommand relayCommand)
        {
            await relayCommand.ExecuteTaskAsync();
            return;
        }

        if (command.CanExecute(null))
        {
            command.Execute(null);
        }
    }
}
