using System.Windows.Input;

namespace PaunixGuard.App.ViewModels;

public sealed class RelayCommand(Func<Task> execute, Func<bool>? canExecute = null) : ICommand
{
    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => canExecute?.Invoke() ?? true;

    public async void Execute(object? parameter)
    {
        await execute();
    }

    public async Task ExecuteTaskAsync()
    {
        await execute();
    }

    public void RaiseCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}

