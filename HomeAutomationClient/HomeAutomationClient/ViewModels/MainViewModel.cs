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
    private readonly IUriService uriService;

    public IUpdateService UpdateService { get; }

    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    public MainViewModel(IWebClientService webClient, IGen24LocalizationService gen24Loc, IUpdateService updateService, IUriService uriService)
    {
        this.gen24Loc = gen24Loc;
        this.uriService = uriService;
        uriService.PathChanged += OnPathChanged;
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

    public override Task Initialize() => TaskExceptionHandler(async () =>
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

        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            // A link into a detail view survives the login: the address the app was started with is only
            // resolved now, because the devices of the installation are known only now.
            if (ViewPath.Find(UpdateService.DetailDevices, uriService.StartupPath) is { } device)
            {
                await ShowDetails(device).ConfigureAwait(true);
                return;
            }

            await ShowDashboardView().ConfigureAwait(true);
        });
    });

    [RelayCommand]
    private Task ShowDetails(IKeyedDevice device) => ShowDetails(device, updatesAddress: true);

    /// <param name="updatesAddress">
    /// False while following the back or forward button: the address is already the one we are navigating to,
    /// and writing it again would push a second history entry the user can never get past.
    /// </param>
    /// <remarks>
    /// Reports its own failures for the same reason as <see cref="ShowDashboardView"/>: the caller in
    /// <see cref="OnPathChanged"/> is a plain action posted to the UI thread and cannot await this.
    /// </remarks>
    private Task ShowDetails(IKeyedDevice device, bool updatesAddress) => TaskExceptionHandler(async () =>
    {
        var isShown = true;

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
                isShown = false;

                await new MessageBox
                {
                    Text = $"Details view for device type {device.Device.GetType().Name} is not implemented.",
                    Title = "NotImplemented",
                    Icon = new ErrorIcon(),
                }.Show().ConfigureAwait(false);

                break;
        }

        if (isShown && updatesAddress)
        {
            uriService.SetPath(ViewPath.For(device.Device));
        }
    });

    /// <summary>
    /// The user pressed back or forward in the browser. The address has already changed, so only the view has to
    /// follow: no login, no reload, and nothing written back into the history - SetPath sees the address it is
    /// asked for is the address already and stays quiet. An address whose device this installation does not
    /// have leads to the dashboard, exactly like a link to it.
    /// </summary>
    /// <remarks>
    /// This view model lives as long as the app, so the event is never unsubscribed.
    /// </remarks>
    private void OnPathChanged(object? sender, string path) => Dispatcher.UIThread.Post(() =>
    {
        if (!IsReady)
        {
            // Still at the login: MainViewModel.Initialize resolves the address once the devices are known.
            return;
        }

        if (ViewPath.Find(UpdateService.DetailDevices, path) is { } device)
        {
            _ = ShowDetails(device, updatesAddress: false);
            return;
        }

        _ = ShowDashboardView(updatesAddress: false);
    });

    /// <summary>
    /// Shows the dashboard and makes it the address of the app. The dashboard is the root, so this is where a
    /// link without a path leads.
    /// </summary>
    /// <remarks>
    /// Goes through <see cref="ViewModelBase.TaskExceptionHandler"/>, so it reports its own failures instead of
    /// throwing: <see cref="OnPathChanged"/> posts a plain action to the UI thread and has no way to await this,
    /// so an exception out of here would belong to nobody and end up as an unobserved task exception. Setting the
    /// address is part of the guarded body, so a dashboard that did not come up is never advertised as one.
    /// </remarks>
    private Task ShowDashboardView(bool updatesAddress = true) => TaskExceptionHandler(async () =>
    {
        BusyText = "Loading dashboard";
        await Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.ForceYielding);
        await Task.Delay(100).ConfigureAwait(false);
        MainViewContent = IoC.Get<DashboardView>();

        if (updatesAddress)
        {
            uriService.SetPath(ViewPath.Dashboard);
        }
    });

    [RelayCommand]
    private Task ShowDashboard() => ShowDashboardView();
}
