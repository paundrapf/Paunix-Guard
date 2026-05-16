using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using PaunixGuard.Core.Events;
using PaunixGuard.Core.GuardState;
using PaunixGuard.Core.Triggers;
using PaunixGuard.Core.Updates;
using PaunixGuard.Windows.Session;

namespace PaunixGuard.App.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly GuardEngine guardEngine;
    private readonly IUpdateService updateService;
    private readonly IEventHistoryStore eventHistoryStore;
    private readonly WindowsSystemEventRouter systemEventRouter;
    private string pinInput = "";
    private string statusMessage = "Ready";
    private string latestEvent = "No events yet";
    private bool isBusy;

    public MainViewModel(
        GuardEngine guardEngine,
        IUpdateService updateService,
        IEventHistoryStore eventHistoryStore,
        WindowsSystemEventRouter systemEventRouter)
    {
        this.guardEngine = guardEngine;
        this.updateService = updateService;
        this.eventHistoryStore = eventHistoryStore;
        this.systemEventRouter = systemEventRouter;

        StartGuardCommand = new RelayCommand(StartGuardAsync, () => !IsBusy && HasPin);
        DisarmCommand = new RelayCommand(DisarmAsync, () => !IsBusy);
        TestAlarmCommand = new RelayCommand(TestAlarmAsync, () => !IsBusy);
        SavePinCommand = new RelayCommand(SavePinAsync, () => !IsBusy);
        CheckUpdatesCommand = new RelayCommand(CheckUpdatesAsync, () => !IsBusy);

        guardEngine.StateChanged += OnStateChanged;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ICommand StartGuardCommand { get; }

    public ICommand DisarmCommand { get; }

    public ICommand TestAlarmCommand { get; }

    public ICommand SavePinCommand { get; }

    public ICommand CheckUpdatesCommand { get; }

    public string PinInput
    {
        get => pinInput;
        set
        {
            pinInput = value;
            OnPropertyChanged();
        }
    }

    public GuardState CurrentState => guardEngine.CurrentState;

    public bool HasPin => guardEngine.HasPin;

    public string StatusMessage
    {
        get => statusMessage;
        private set
        {
            statusMessage = value;
            OnPropertyChanged();
        }
    }

    public string LatestEvent
    {
        get => latestEvent;
        private set
        {
            latestEvent = value;
            OnPropertyChanged();
        }
    }

    public bool IsBusy
    {
        get => isBusy;
        private set
        {
            isBusy = value;
            OnPropertyChanged();
        }
    }

    public Core.Settings.GuardSettings Settings => guardEngine.Settings;

    public GuardEngine GuardEngine => guardEngine;

    public IEventHistoryStore EventHistoryStore => eventHistoryStore;

    public async Task SaveSettingsAsync(Core.Settings.GuardSettings settings)
    {
        await guardEngine.SaveSettingsAsync(settings, CancellationToken.None);
    }

    public async Task RefreshLatestEventAsync(CancellationToken cancellationToken)
    {
        var guardEvent = await eventHistoryStore.GetLatestAsync(cancellationToken);
        LatestEvent = guardEvent is null
            ? "No events yet"
            : $"{guardEvent.CreatedAt.LocalDateTime:g} - {guardEvent.TriggerType} - {guardEvent.Resolution}";
    }

    private async Task SavePinAsync()
    {
        await RunBusyAsync(async () =>
        {
            if (PinInput.Length < 4)
            {
                StatusMessage = "Use at least 4 digits/characters for the PIN.";
                return;
            }

            await guardEngine.SetPinAsync(PinInput, CancellationToken.None);
            PinInput = "";
            StatusMessage = "PIN saved.";
            OnPropertyChanged(nameof(HasPin));
        });
    }

    private async Task StartGuardAsync()
    {
        await RunBusyAsync(async () =>
        {
            await guardEngine.StartGuardAsync(CancellationToken.None);
            StatusMessage = "Guard mode is armed.";
        });
    }

    private async Task DisarmAsync()
    {
        await RunBusyAsync(async () =>
        {
            var ok = await guardEngine.DisarmAsync(PinInput, DisarmMethod.LaptopPin, CancellationToken.None);
            StatusMessage = ok ? "Guard disarmed." : "Invalid PIN.";
            if (ok)
            {
                PinInput = "";
            }

            await RefreshLatestEventAsync(CancellationToken.None);
        });
    }

    private async Task TestAlarmAsync()
    {
        await RunBusyAsync(async () =>
        {
            if (guardEngine.CurrentState == GuardState.Idle)
            {
                await guardEngine.StartGuardAsync(CancellationToken.None);
            }

            await guardEngine.TriggerManualAlarmAsync(CancellationToken.None);
            StatusMessage = "Test alarm triggered. Enter PIN to stop.";
        });
    }

    private async Task CheckUpdatesAsync()
    {
        await RunBusyAsync(async () =>
        {
            var result = await updateService.CheckAsync(guardEngine.Settings.UpdateChannel, CancellationToken.None);
            StatusMessage = result.ErrorMessage is not null
                ? $"Update check failed: {result.ErrorMessage}"
                : result.IsAvailable
                    ? $"Update available: {result.Version}"
                    : "No updates available.";
        });
    }

    private async Task RunBusyAsync(Func<Task> action)
    {
        if (IsBusy)
        {
            return;
        }

        try
        {
            IsBusy = true;
            await action();
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
            RaiseCommandStates();
        }
    }

    private void OnStateChanged(object? sender, GuardStateChangedEventArgs e)
    {
        OnPropertyChanged(nameof(CurrentState));
        systemEventRouter.Configure(
            e.CurrentState is GuardState.Armed or GuardState.Warning or GuardState.Alarm,
            guardEngine.Settings);

        StatusMessage = e.Signal is null
            ? $"State changed: {e.CurrentState}"
            : $"{e.CurrentState}: {e.Signal.Reason}";
    }

    private void RaiseCommandStates()
    {
        foreach (var command in new[] { StartGuardCommand, DisarmCommand, TestAlarmCommand, SavePinCommand, CheckUpdatesCommand })
        {
            if (command is RelayCommand relayCommand)
            {
                relayCommand.RaiseCanExecuteChanged();
            }
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

