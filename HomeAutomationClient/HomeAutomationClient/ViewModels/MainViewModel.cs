using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using De.Hochstaetter.Fronius.Models.Charging;
using BatteryDetailsView = De.Hochstaetter.HomeAutomationClient.Views.BatteryDetailsView;
using InverterDetailsView = De.Hochstaetter.HomeAutomationClient.Views.InverterDetailsView;
using SmartMeterDetailsView = De.Hochstaetter.HomeAutomationClient.Views.SmartMeterDetailsView;
using WattPilotDetailsView = De.Hochstaetter.HomeAutomationClient.Views.WattPilotDetailsView;

namespace De.Hochstaetter.HomeAutomationClient.ViewModels;

public sealed partial class MainViewModel : ViewModelBase
{
    private readonly IGen24LocalizationService gen24Loc;

    public IUpdateService UpdateService { get; }

    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    public MainViewModel(IWebClientService webClient, IGen24LocalizationService gen24Loc, IUpdateService updateService)
    {
        this.gen24Loc = gen24Loc;
        UpdateService = updateService;
        ApiUri = IoC.TryGetRegistered<ICache>()?.Get<string>(CacheKeys.ApiUri) ?? "https://home-automation.example.com";
        webClient.Initialize(ApiUri, "hacc", "0.5.0.0");
    }

    public string ApiUri { get; }

    [ObservableProperty, NotifyPropertyChangedFor(nameof(IsDialogBusy))]
    public partial string? DialogBusyText { get; set; }

    public bool IsDialogBusy => DialogBusyText != null && IsDialogVisible;

    [ObservableProperty]
    public partial bool IsReady { get; set; }

    /// <summary>
    /// Colors all ticks of every gauge, not just those up to the current value. Lives here because the switch for
    /// it sits in the main view and applies to all views; the other view models reach it through this singleton.
    /// </summary>
    [ObservableProperty]
    public partial bool ColorAllTicks { get; set; } = true;

    [ObservableProperty]
    public partial object? MainViewContent { get; set; }

    public bool IsDialogVisible => CurrentDialog != null;

    /// <summary>
    /// True while a dialog blocks the rest of the UI. A non-modal dialog is visible without disabling anything.
    /// </summary>
    public bool IsModalDialogVisible => CurrentDialog is { IsModal: true };

    [ObservableProperty, NotifyPropertyChangedFor(nameof(IsDialogVisible), nameof(IsDialogBusy), nameof(IsModalDialogVisible))]
    public partial DialogQueueItem? CurrentDialog { get; set; }

    public ConcurrentStack<DialogQueueItem?> DialogQueue { get; } = new();

    [RelayCommand]
    public async Task DialogClosed()
    {
        try
        {
            if (CurrentDialog?.Body is IDialogControl { DataContext: IDialogBase dialogBase })
            {
                await dialogBase.AbortAsync().ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            await ex.Show().ConfigureAwait(false);
        }
    }

    public override async Task Initialize()
    {
        try
        {
            await base.Initialize().ConfigureAwait(false);

            var loginViewModel = new LoginViewModel(new DialogParameters
            {
                Title = $"{AppConstants.AppName} - {Loc.LoginNoun}",
                ShowCloseBox = false,
                IsModal = false,
            });

            await loginViewModel.ShowDialogAsync().ConfigureAwait(false);
            BusyText = Loc.GetInverterLocalization;
            await gen24Loc.Initialize().ConfigureAwait(false);
            BusyText = Loc.ConnectingToHas;
            await UpdateService.StartAsync().ConfigureAwait(false);
            IsReady = true;
            await Dispatcher.UIThread.InvokeAsync(() => MainViewContent = IoC.Get<DashboardView>());
        }
        catch (Exception ex)
        {
            BusyText = null;
            await ex.Show().ConfigureAwait(false);
        }
        finally
        {
            BusyText = null;
        }
    }

    [RelayCommand]
    private async Task ShowDetails(IKeyedDevice device)
    {
        switch (device.Device)
        {
            case Gen24System gen24System:
                var detailsView = IoC.Get<InverterDetailsView>();
                detailsView.ViewModel.Gen24System = gen24System;
                MainViewContent = detailsView;
                break;

            // The battery view needs the inverter, because the net state of charge and the net capacity live there.
            // It must not be looked up from the Gen24Storage of the menu entry: Gen24System.CopyFrom replaces
            // Sensors on every update, so that object is a stale snapshot by the time the entry is clicked.
            case Gen24Storage when UpdateService.BatteryGen24System is { } batteryInverter:
                var batteryView = IoC.Get<BatteryDetailsView>();
                batteryView.ViewModel.Gen24System = batteryInverter;
                MainViewContent = batteryView;
                break;

            // Same here: the meter of the menu entry is replaced on every update, so the view binds the live
            // UpdateService.SmartMeter instead of the object handed in.
            case Gen24PowerMeter3P:
                MainViewContent = IoC.Get<SmartMeterDetailsView>();
                break;

            case WattPilot wattPilot:
                var wattPilotView = IoC.Get<WattPilotDetailsView>();
                wattPilotView.ViewModel.WattPilot = wattPilot;
                MainViewContent = wattPilotView;
                break;

            default:
                await new MessageBox
                {
                    Text = $"Details view for device type {device.Device.GetType().Name} is not implemented.",
                    Title = "NotImplemented",
                    Icon = new ErrorIcon(),
                }.Show().ConfigureAwait(false);

                break;
        }
    }

    [RelayCommand]
    private async Task ShowDashboard()
    {
        try
        {
            BusyText = "Loading dashboard";
            await Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.ForceYielding);
            var dashboardView = IoC.Get<DashboardView>();
            await Task.Delay(100).ConfigureAwait(false);
            MainViewContent = dashboardView;
        }
        finally
        {
            BusyText = null;
        }
    }
}
