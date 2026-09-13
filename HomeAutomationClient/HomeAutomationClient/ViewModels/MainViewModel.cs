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
    /// Who is logged in, as the server reported it at the login, for the menu bar. The roles are the enum names as
    /// they are, not localized: they are what the server's user list says, and what an administrator would type.
    /// </summary>
    [ObservableProperty, NotifyPropertyChangedFor(nameof(UserText), nameof(SettingsItems))]
    public partial UserInfo? User { get; set; }

    public string? UserText => User is { } user ? $"{user.UserName} ({user.Roles})" : null;

    /// <summary>
    /// What the Settings menu offers: the devices with settings and, for an administrator, the user management.
    /// The server checks the role on every call anyway, so leaving the entry out is only a courtesy.
    /// </summary>
    /// <remarks>
    /// A list, not a lazy query: the menu copies whatever it is handed the moment the binding reads it. It is read
    /// when <see cref="User"/> is set - which is before the devices are known - and so again once
    /// <see cref="Initialize"/> has started the <see cref="UpdateService"/>.
    /// </remarks>
    public IReadOnlyList<object> SettingsItems => User is { Roles: var roles } && roles.HasFlag(Roles.Administrator)
        ? [.. UpdateService.DevicesWithSettings, UserManagementEntry.Instance]
        : [.. UpdateService.DevicesWithSettings];

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
    public bool IsModalDialogVisible => CurrentDialog is { Parameters.IsModal: true };

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
        User = loginViewModel.User;
        BusyText = Loc.GetInverterLocalization;
        await gen24Loc.Initialize().ConfigureAwait(false);
        BusyText = Loc.ConnectingToHas;
        await UpdateService.StartAsync().ConfigureAwait(false);
        OnPropertyChanged(nameof(SettingsItems));
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

    /// <summary>
    /// Opens the settings dialog of one entry of the Settings menu: a device, or the user management. The title
    /// has to be settled before the dialog goes up, because the frame reads it once when it is created, so it
    /// comes from the device rather than from what the inverter reports about itself a moment later.
    /// </summary>
    [RelayCommand]
    private Task Settings(object parameter) => TaskExceptionHandler(async () =>
    {
        if (parameter is UserManagementEntry)
        {
            await new UserManagementViewModel(new DialogParameters { Title = Loc.UserManagement }).ShowDialogAsync().ConfigureAwait(false);
            return;
        }

        if (parameter is not IKeyedDevice device)
        {
            throw new ArgumentException($"No settings for {parameter.GetType().Name}", nameof(parameter));
        }

        if (device.Device is WattPilot wattPilot)
        {
            // Nothing is read first: the client holds the live charger, kept current by the deltas the server
            // pushes, and the dialog starts from a copy of it.
            var wattPilotDialog = new WattPilotSettingsDialogViewModel(new DialogParameters { Title = $"{Loc.Settings}: {wattPilot.DisplayName}" }, device.Key, wattPilot);
            await wattPilotDialog.ShowDialogAsync().ConfigureAwait(true);
            return;
        }

        if (device.Device is not Gen24System gen24System)
        {
            await new MessageBox
            {
                Text = $"Settings for device type {device.Device.GetType().Name} are not implemented.",
                Title = "NotImplemented",
                Icon = new ErrorIcon(),
            }.Show().ConfigureAwait(false);

            return;
        }

        var name = gen24System.Config?.InverterSettings?.SystemName ?? gen24System.Manufacturer + ' ' + gen24System.SerialNumber;

        var dialog = new Gen24SettingsDialogViewModel(new DialogParameters { Title = $"{Loc.InverterSettings}: {name}" })
        {
            DeviceId = device.Key,
        };

        await dialog.ShowDialogAsync().ConfigureAwait(true);
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

        // Off the UI thread on purpose, so the busy text reaches the screen before the dashboard is built.
        await Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.ForceYielding);
        await Task.Delay(100).ConfigureAwait(false);

        // And back onto the UI thread to build the view. Not because MainViewContent is bound - the binding system
        // marshals a bound write by itself - but because IoC.Get constructs the DashboardView singleton on its
        // first use, and constructing a control is what needs the UI thread. Neither await above keeps it:
        // ForceYielding without ContinueOnCapturedContext drops the context, and ConfigureAwait(false) does not ask
        // for it back, so on a desktop head we are on the thread pool by here. WebAssembly has a single thread,
        // which is why only the desktop heads ever crashed.
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            MainViewContent = IoC.Get<DashboardView>();

            if (updatesAddress)
            {
                uriService.SetPath(ViewPath.Dashboard);
            }
        });
    });

    [RelayCommand]
    private Task ShowDashboard() => ShowDashboardView();
}
