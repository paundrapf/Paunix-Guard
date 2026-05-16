using System.Windows;
using PaunixGuard.App.ViewModels;
using PaunixGuard.Core.Events;

namespace PaunixGuard.App.Views;

public partial class HistoryWindow : Window
{
    private readonly MainViewModel viewModel;

    public HistoryWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        this.viewModel = viewModel;
        _ = LoadEventsAsync();
    }

    private async Task LoadEventsAsync()
    {
        try
        {
            var store = viewModel.EventHistoryStore;
            var events = await store.GetAllAsync(50);

            Dispatcher.Invoke(() =>
            {
                foreach (var evt in events)
                {
                    EventsList.Items.Add(new EventRow(
                        evt.CreatedAt.LocalDateTime.ToString("g"),
                        evt.TriggerType.ToString(),
                        evt.Resolution.ToString(),
                        evt.Reason));
                }
            });
        }
        catch
        {
        }
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e)
    {
        EventsList.Items.Clear();
        await LoadEventsAsync();
    }

    private sealed record EventRow(string CreatedAt, string TriggerType, string Resolution, string Reason);
}
