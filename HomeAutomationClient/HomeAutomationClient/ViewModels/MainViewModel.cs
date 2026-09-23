using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Styling;
using De.Hochstaetter.Fronius.Models.Charging;
using BatteryDetailsView = De.Hochstaetter.HomeAutomationClient.Views.BatteryDetailsView;
using InverterDetailsView = De.Hochstaetter.HomeAutomationClient.Views.InverterDetailsView;
using SmartMeterDetailsView = De.Hochstaetter.HomeAutomationClient.Views.SmartMeterDetailsView;
using WattPilotDetailsView = De.Hochstaetter.HomeAutomationClient.Views.WattPilotDetailsView;

namespace De.Hochstaetter.HomeAutomationClient.ViewModels;

public sealed partial class MainViewModel : ViewModelBase, IPowerDisplayOptions
{
    private const string ProductName = "hacc";
    private const string ProductVersion = "0.5.0.0";

    private readonly IGen24LocalizationService gen24Loc;
    private readonly IUriService uriService;
    private readonly IWebClientService webClient;
    private readonly IPagePresenter pagePresenter;
    private readonly IDialogPresenter dialogPresenter;

    /// <summary>Which outdated firmware the user has been told about, so the same status arriving again says nothing.</summary>
    private readonly FirmwareUpdateNotice firmwareNotice = new();

    public IUpdateService UpdateService { get; }

    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    public MainViewModel(IWebClientService webClient, IGen24LocalizationService gen24Loc, IUpdateService updateService, IUriService uriService, IPagePresenter pagePresenter, IDialogPresenter dialogPresenter)
    {
        this.webClient = webClient;
        this.gen24Loc = gen24Loc;
        this.uriService = uriService;
        this.pagePresenter = pagePresenter;
        this.dialogPresenter = dialogPresenter;
        uriService.PathChanged += OnPathChanged;
        UpdateService = updateService;
        updateService.SolarWebFirmwareStatusChanged += OnSolarWebFirmwareStatusChanged;
        SetApiUri(IoC.TryGetRegistered<ICache>()?.Get<string>(CacheKeys.ApiUri) ?? "https://home-automation.example.com");
        PublishColorAllTicks();
    }

    /// <summary>
    /// Where the server's web api is. Set through <see cref="SetApiUri"/> only, so that the address this says and
    /// the address the requests go to are never two different things.
    /// </summary>
    public string ApiUri { get; private set; }

    /// <summary>
    /// Points the client at another server. The login dialog calls this when the user changes the connection.
    /// </summary>
    [MemberNotNull(nameof(ApiUri))]
    public void SetApiUri(string apiUri)
    {
        ApiUri = apiUri;
        webClient.Initialize(apiUri, ProductName, ProductVersion);
    }

    [ObservableProperty, NotifyPropertyChangedFor(nameof(IsDialogBusy))]
    public partial string? DialogBusyText { get; set; }

    public bool IsDialogBusy => DialogBusyText != null && IsDialogVisible;

    [ObservableProperty]
    public partial bool IsReady { get; set; }

    /// <summary>
    /// Who is logged in, as the server reported it at the login, for the menu bar. The roles are the enum names as
    /// they are, not localized: they are what the server's user list says, and what an administrator would type.
    /// </summary>
    [ObservableProperty, NotifyPropertyChangedFor(nameof(UserText), nameof(SettingsItems), nameof(ShowSettingsMenu), nameof(ShowOpenApiDocumentMenu))]
    public partial UserInfo? User { get; set; }

    public string? UserText => User is { } user ? $"{user.UserName} ({user.Roles})" : null;

    /// <summary>
    /// The Settings menu is for users, not guests: a guest may change nothing, so every entry in it would only
    /// earn a 403. Hidden as a whole for a guest - unlike <see cref="UserManagementEntry"/> inside it, which stays
    /// visible to every user on purpose; see <see cref="SettingsItems"/>.
    /// </summary>
    public bool ShowSettingsMenu => User?.Roles.SeesAllDevices() ?? false;

    /// <summary>
    /// The OpenAPI document in the View menu, for developers only: the server answers anybody else with 403, so for
    /// them the entry could lead nowhere but to an error.
    /// </summary>
    public bool ShowOpenApiDocumentMenu => User?.Roles.HasFlag(Roles.Developer) ?? false;

    /// <summary>
    /// The Dashboard entry, which is there to bring the dashboard back once a detail page has taken its place.
    /// Hidden where a detail page opens in a window of its own - the desktop - because the dashboard is never
    /// covered there and the entry would do nothing at all.
    /// </summary>
    public bool ShowDashboardMenu => pagePresenter.ShowsPagesInMainView;

    /// <summary>
    /// Whether the menu bar may open a dialog. Where one frame shows one dialog at a time, it may not while a
    /// dialog is up: a second one from the menu would cover the first, and the user would come back to it with no
    /// idea it had been there. Where every dialog gets a window, there is nothing to cover and this is always true.
    /// </summary>
    /// <remarks>
    /// The menu bar is the one thing a modal dialog does not disable, on purpose - switching to another device's
    /// detail page while a dialog is open is allowed and stays allowed. Only what would open a second dialog is
    /// held back, which is why this is on the four commands that do rather than on the bar.
    /// </remarks>
    public bool CanOpenDialog => !dialogPresenter.ShowsDialogsInMainView || !IsDialogVisible;

    /// <summary>
    /// What the Settings menu offers: the devices with settings and the user management. Shown to every user
    /// regardless of role, on purpose - the server's <c>IdentityController</c> is the one place that enforces who
    /// may actually add, edit or delete a user, and hiding the entry here would only get in the way of testing that.
    /// Changing your own password and logging out are not here - they are about the account that is logged in
    /// rather than a thing to configure, so they sit behind the user's own name at the right of the menu bar; see
    /// <see cref="ChangePassword"/> and <see cref="Logout"/>.
    /// </summary>
    /// <remarks>
    /// A list, not a lazy query: the menu copies whatever it is handed the moment the binding reads it. It is read
    /// when <see cref="User"/> is set - which is before the devices are known - and so again once
    /// <see cref="Initialize"/> has started the <see cref="UpdateService"/>.
    /// </remarks>
    public IReadOnlyList<object> SettingsItems => [.. UpdateService.DevicesWithSettings, UserManagementEntry.Instance];

    /// <summary>
    /// The application resource the gauges of the detail views read <see cref="ColorAllTicks"/> from; see
    /// <see cref="PublishColorAllTicks"/> and the <c>WrapPanel.GaugeGroups</c> style in
    /// <c>Styles/DetailViews.axaml</c>.
    /// </summary>
    public const string ColorAllTicksResourceKey = "ColorAllGaugeTicks";

    /// <summary>
    /// Colors all ticks of every gauge, not just those up to the current value. Lives here because the switch for
    /// it sits in the main view and applies to all views; the other view models reach it through this singleton.
    /// </summary>
    [ObservableProperty]
    public partial bool ColorAllTicks { get; set; } = true;

    partial void OnColorAllTicksChanged(bool value) => PublishColorAllTicks();

    /// <summary>
    /// Publishes <see cref="ColorAllTicks"/> as an application resource, which is the one thing every window of
    /// the app shares.
    /// </summary>
    /// <remarks>
    /// A detail page is inside the main view in the browser and on the phones, so a binding up the tree finds
    /// this view model there - but on the desktop the page stands in a window of its own, where there is no
    /// <c>MainView</c> above it and such a binding finds nothing at all. That is why the switch appeared to work
    /// on the web and to do nothing on the desktop until 2026-09-19. Application resources do not care which
    /// window asks, and a <c>DynamicResource</c> follows every later change of this one.
    /// </remarks>
    private void PublishColorAllTicks()
    {
        if (Application.Current is { } app)
        {
            app.Resources[ColorAllTicksResourceKey] = ColorAllTicks;
        }
    }

    /// <summary>
    /// "Solar Web" mode - see <see cref="IPowerDisplayOptions.IncludeInverterPower"/>, which is the contract the
    /// house block and the power flow page take this through rather than the whole main view model. Here because
    /// its switch sits in the main view beside the other two and applies to every view that shows a consumption.
    /// </summary>
    [ObservableProperty]
    public partial bool IncludeInverterPower { get; set; }

    /// <summary>
    /// Overrides whatever light or dark variant the OS, the browser or Avalonia's own default reported, so a user
    /// can force the app into either one. Starts out reflecting whatever is in effect when the app comes up, not
    /// forcing dark or light before the user has touched the switch.
    /// </summary>
    [ObservableProperty]
    public partial bool IsDarkMode { get; set; } = Application.Current?.ActualThemeVariant == ThemeVariant.Dark;

    partial void OnIsDarkModeChanged(bool value)
    {
        if (Application.Current is { } app)
        {
            app.RequestedThemeVariant = value ? ThemeVariant.Dark : ThemeVariant.Light;
        }
    }

    [ObservableProperty]
    public partial object? MainViewContent { get; set; }

    public bool IsDialogVisible => CurrentDialog != null;

    /// <summary>
    /// True while a dialog blocks the rest of the UI. A non-modal dialog is visible without disabling anything.
    /// </summary>
    public bool IsModalDialogVisible => CurrentDialog is { Parameters.IsModal: true };

    [ObservableProperty, NotifyPropertyChangedFor(nameof(IsDialogVisible), nameof(IsDialogBusy), nameof(IsModalDialogVisible), nameof(CanOpenDialog))]
    [NotifyCanExecuteChangedFor(nameof(SettingsCommand), nameof(ShowEnergyChartCommand), nameof(ChangePasswordCommand), nameof(LogoutCommand))]
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
        await LoginAndStartAsync(followStartupPath: true, tryCachedLogin: true).ConfigureAwait(false);
    });

    /// <summary>
    /// Logs in and, once that succeeds, starts <see cref="UpdateService"/> and shows a view. Called
    /// once by <see cref="Initialize"/> at startup and again by <see cref="Logout"/> once it has torn the previous
    /// session down - nothing here assumes it is the first time.
    /// </summary>
    /// <param name="followStartupPath">
    /// Whether a link the app was started with should be resolved once the devices are known, as only
    /// <see cref="Initialize"/> wants: after <see cref="Logout"/> the address has already been reset to the
    /// dashboard, and there is no startup link to follow a second time.
    /// </param>
    /// <param name="tryCachedLogin">
    /// Whether the remembered credentials are tried before the login box is shown. True at startup, where a
    /// remembered login gets straight in and the box appears only when that does not work. False after a logout,
    /// which has just forgotten the credentials - and where forgetting them failed, the attempt would put the very
    /// user who has just logged out straight back in.
    /// </param>
    private async Task LoginAndStartAsync(bool followStartupPath, bool tryCachedLogin)
    {
        var loginViewModel = new LoginViewModel(new DialogParameters
        {
            Title = $"{AppConstants.AppName} - {Loc.LoginNoun}",
            ShowCloseBox = false,
            IsModal = false,
            // Even where every other dialog gets a window: this is what the app shows before there is anything to
            // put a window beside, and nobody may get past it.
            StaysInMainView = true,
        });

        if (tryCachedLogin)
        {
            BusyText = Loc.BusyLoggingIn;

            try
            {
                await loginViewModel.TryLoginWithCachedCredentialsAsync().ConfigureAwait(false);
            }
            catch (Exception)
            {
                // A failed attempt must never keep the box from showing - without this, a corrupt cache would
                // leave the app with no way to log in at all. Nothing is reported here on purpose: the dialog
                // reads the very same cache in its Initialize and reports the failure there.
            }
            finally
            {
                BusyText = null;
            }
        }

        if (loginViewModel.User is null)
        {
            await loginViewModel.ShowDialogAsync().ConfigureAwait(false);
        }

        User = loginViewModel.User;
        BusyText = Loc.GetInverterLocalization;
        await gen24Loc.Initialize().ConfigureAwait(false);
        BusyText = Loc.ConnectingToHas;
        await UpdateService.StartAsync(User?.Roles ?? Roles.None).ConfigureAwait(false);
        OnPropertyChanged(nameof(SettingsItems));
        IsReady = true;

        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            // A link into a page survives the login: the address the app was started with is only resolved now,
            // because the devices of the installation are known only now.
            await ShowPath(followStartupPath ? uriService.StartupPath : ViewPath.Dashboard, updatesAddress: true).ConfigureAwait(true);
        });
    }

    /// <summary>
    /// Opens the settings dialog of one entry of the Settings menu: a device, or the user management. The title
    /// has to be settled before the dialog goes up, because the frame reads it once when it is created, so it
    /// comes from the device rather than from what the inverter reports about itself a moment later.
    /// </summary>
    // Concurrently, because what this opens is a window on a head that has windows, and the command is pending
    // for as long as that window is open. Without this the menu entry would be disabled while it is - so a second
    // one could never be opened, and clicking the same one again could not even bring it to the front.
    [RelayCommand(AllowConcurrentExecutions = true, CanExecute = nameof(CanOpenDialog))]
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
            var wattPilotDialog = new WattPilotSettingsDialogViewModel(new DialogParameters { Title = $"{Loc.Settings}: {wattPilot.DisplayName}", WindowKey = device.Key }, device.Key, wattPilot);
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

        // The key is the device, so a head with windows opens the settings of each inverter in a window of its own
        // and never the same inverter twice.
        var dialog = new Gen24SettingsDialogViewModel(new DialogParameters { Title = $"{Loc.InverterSettings}: {name}", WindowKey = device.Key })
        {
            DeviceId = device.Key,
        };

        await dialog.ShowDialogAsync().ConfigureAwait(true);
    });

    /// <summary>
    /// Lets the logged in user change their own password. Unlike an administrator editing someone else's account
    /// in <see cref="UserManagementViewModel"/>, the server needs the current password rather than a role - see
    /// <c>IdentityController.ChangePassword</c> - and there is no user list entry to refresh afterwards. Reached
    /// from the flyout behind the user's own name in the menu bar, not from the Settings menu: it is about the
    /// account that is logged in, not a thing to configure.
    /// </summary>
    // Concurrently, because what this opens is a window on a head that has windows, and the command is pending
    // for as long as that window is open. Without this the menu entry would be disabled while it is - so a second
    // one could never be opened, and clicking the same one again could not even bring it to the front.
    [RelayCommand(AllowConcurrentExecutions = true, CanExecute = nameof(CanOpenDialog))]
    private Task ChangePassword() => TaskExceptionHandler(async () =>
    {
        var editor = new ChangePasswordViewModel(new DialogParameters { Title = Loc.ChangePassword });

        if (await editor.ShowDialogAsync().ConfigureAwait(true) is not { } request)
        {
            return;
        }

        BusyText = Loc.ChangePassword;
        var result = await webClient.ChangePassword(request).ConfigureAwait(true);

        if (result.Status != HttpStatusCode.OK)
        {
            await ShowHttpError(result).ConfigureAwait(true);
            return;
        }

        if (User is { } me)
        {
            // Changing the password ends every session of the user on the server, the bearer token this client
            // still carries included, and the password the client would log in again with is the old one. Log in
            // with the new one and persist it, the same way UserManagementViewModel.Edit does when an
            // administrator changes their own account.
            var login = await webClient.Login(me.UserName, request.NewPassword).ConfigureAwait(true);

            if (login.Status != HttpStatusCode.OK)
            {
                await ShowHttpError(login).ConfigureAwait(true);
                return;
            }

            if (await StoredConnection.SaveAsync(me.UserName, request.NewPassword).ConfigureAwait(true) is { } problem)
            {
                // The password did change; only remembering it for the next start did not.
                await problem.ShowServerProblem(ApiUri).ConfigureAwait(true);
                return;
            }
        }

        await new MessageBox
        {
            Text = Loc.PasswordChanged,
            Title = Loc.ChangePassword,
            Buttons = [Loc.Ok],
            Icon = new InfoIcon(),
        }.Show().ConfigureAwait(true);
    });

    /// <summary>
    /// Ends the session and shows the login dialog again, exactly the way <see cref="Initialize"/> starts the app:
    /// hides the menu bar and whatever view was on screen, tears <see cref="UpdateService"/> down so a stale
    /// dashboard cannot go on calling a server this client no longer has credentials for, and tells the server to
    /// drop its own copy of them. What <see cref="StoredConnection"/> remembers is forgotten too: the user name
    /// and the password leave the device with the user, and only the server address stays behind.
    /// </summary>
    // Not concurrently: it shows a modal confirmation and then tears the session down. The gate is the same
    // one the other three use - logging out with a settings dialog on the frame would leave that dialog queued
    // underneath the login.
    [RelayCommand(CanExecute = nameof(CanOpenDialog))]
    private Task Logout() => TaskExceptionHandler(async () =>
    {
        var answer = await new MessageBox
        {
            Text = Loc.ConfirmLogout,
            Title = Loc.LogOut,
            Buttons = [Loc.LogOut, Loc.Cancel],
            Icon = new WarningIcon(),
        }.Show().ConfigureAwait(true);

        if (answer?.Index != 0)
        {
            return;
        }

        IsReady = false;

        // Everything the session put on screen goes with it. On a head that gives pages and dialogs windows of
        // their own, those would otherwise stay up showing the devices of a user who has just logged out.
        pagePresenter.CloseAll();
        dialogPresenter.CloseAll();
        firmwareNotice.Reset();

        MainViewContent = null;
        User = null;

        // Guarded on its own: a cache that cannot be written must be reported, since the credentials are still
        // on the device then, but it must not stop the logout halfway and leave the app without a login box.
        await TaskExceptionHandler(StoredConnection.ForgetAsync).ConfigureAwait(true);

        BusyText = Loc.LogOut;
        await UpdateService.StopAsync().ConfigureAwait(true);
        await webClient.Logout().ConfigureAwait(true);

        await LoginAndStartAsync(followStartupPath: false, tryCachedLogin: false).ConfigureAwait(true);
    });

    /// <summary>
    /// The price chart. Not a device and not a setting, so it has a button of its own in the menu bar, shown once
    /// the server has sent price data - a server that collects none has nothing to show.
    /// </summary>
    // Concurrently, because what this opens is a window on a head that has windows, and the command is pending
    // for as long as that window is open. Without this the menu entry would be disabled while it is - so a second
    // one could never be opened, and clicking the same one again could not even bring it to the front.
    [RelayCommand(AllowConcurrentExecutions = true, CanExecute = nameof(CanOpenDialog))]
    private Task ShowEnergyChart() => TaskExceptionHandler(async () =>
    {
        await new EnergyChartViewModel(new DialogParameters { Title = Loc.ElectricityPrice, IsResizeable = true }).ShowDialogAsync().ConfigureAwait(true);
    });

    /// <summary>
    /// The Solar.web chart: the history of the PV system as the portal shows it. Shown once the server has answered a
    /// Solar.web request - a server without a Solar.web account has nothing to show.
    /// </summary>
    // Concurrently, for the same reason as ShowEnergyChart: the command is pending for as long as the window is open.
    [RelayCommand(AllowConcurrentExecutions = true, CanExecute = nameof(CanOpenDialog))]
    private Task ShowSolarWebChart() => TaskExceptionHandler(async () =>
    {
        await new SolarWebChartViewModel(new DialogParameters { Title = Loc.SolarWeb, IsResizeable = true }).ShowDialogAsync().ConfigureAwait(true);
    });

    /// <summary>
    /// The server's OpenAPI document, in a browser tab of its own - a new tab in the browser head, the default
    /// browser elsewhere. The tab cannot carry the bearer token, so its address carries a one-time ticket that the
    /// server swaps for a cookie; see <see cref="IWebClientService.GetBrowserTabUri"/>.
    /// </summary>
    [RelayCommand]
    private Task ShowOpenApiDocument() => TaskExceptionHandler(async () =>
    {
        var result = await webClient.GetBrowserTabUri(OpenApiDocumentPath).ConfigureAwait(true);

        if (result is not { Status: HttpStatusCode.OK, Payload: { } uri })
        {
            await ShowHttpError(result).ConfigureAwait(true);
            return;
        }

        // The ticket is worth nothing after a few seconds, so it is fetched on the click and not before; that is
        // quick enough for the browser to still count the tab as opened by the user rather than as a pop-up.
        await IoC.GetRegistered<IUriLauncher>().LaunchAsync(uri).ConfigureAwait(true);
    });

    /// <summary>Where <c>MapOpenApi</c> serves the document, relative to the root of the server.</summary>
    private const string OpenApiDocumentPath = "openapi/v1.json";

    /// <summary>
    /// A firmware status from the server, on the hub's thread or from the catch-up. Where it names outdated firmware the
    /// user has not been told about, a message box says so and offers the changelog; the same status again says nothing.
    /// </summary>
    private void OnSolarWebFirmwareStatusChanged(object? sender, SolarWebFirmwareStatus status)
    {
        var fresh = firmwareNotice.Take(status);

        if (fresh.Count == 0)
        {
            return;
        }

        Dispatcher.UIThread.Post(() => ShowFirmwareNotice(fresh));
    }

    /// <summary>Reports its own failures: a posted callback has nobody else to report to.</summary>
    private void ShowFirmwareNotice(IReadOnlyList<SolarWebFirmwareComponent> outdated) => HandleTaskExceptions(() => TaskExceptionHandler(async () =>
    {
        var answer = await new MessageBox
        {
            Title = Loc.FirmwareUpdateAvailable,
            Text = Loc.FirmwareUpdateAvailableText,
            ItemList = outdated.Select(FirmwareUpdateNotice.Describe).ToList(),
            Buttons = [Loc.ShowChangelog, Loc.Close],
            DefaultButtonIndex = 1,
            Icon = new InfoIcon(),
        }.Show().ConfigureAwait(true);

        if (answer?.Index != 0 || IoC.TryGetRegistered<IUriLauncher>() is not { } launcher)
        {
            return;
        }

        foreach (var url in outdated.Select(c => c.ChangelogUrl).Where(u => u != null).Distinct())
        {
            await launcher.LaunchAsync(new Uri(url!)).ConfigureAwait(true);
        }
    }));

    /// <summary>
    /// The power flow page: the sources, the house and every metered consumer, with the power moving between
    /// them. A page like a detail view, not a dialog like the price chart beside it in the View menu: inside the
    /// main view on the browser and the phones, a window of its own on the desktop. One of it, so the key is fixed.
    /// </summary>
    [RelayCommand]
    private Task ShowPowerFlow() => ShowPowerFlowView(updatesAddress: true);

    /// <param name="updatesAddress">
    /// As in <see cref="ShowDetails(IKeyedDevice, bool)"/>: false while following the back or forward button.
    /// </param>
    private Task ShowPowerFlowView(bool updatesAddress) => TaskExceptionHandler(() =>
    {
        pagePresenter.Show<PowerFlowView>(PowerFlowView.PageKey, Loc.PowerFlow, _ => { });

        if (updatesAddress)
        {
            uriService.SetPath(ViewPath.PowerFlow);
        }

        return Task.CompletedTask;
    });

    /// <summary>
    /// Shows whatever <paramref name="path"/> names: the power flow page, the detail view of a device of this
    /// installation, or the dashboard where it names neither. The one place that turns an address into a view, so
    /// that a startup link and the back button cannot come to disagree about what an address means.
    /// </summary>
    private Task ShowPath(string? path, bool updatesAddress) => ViewPath.IsPowerFlow(path)
        ? ShowPowerFlowView(updatesAddress)
        : ViewPath.Find(UpdateService.DetailDevices, path) is { } device
            ? ShowDetails(device, updatesAddress)
            : ShowDashboardView(updatesAddress);

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

        // What the menu entry of the device says, which is what the window of a head that gives pages windows is
        // called. Inside the main view there is no title to show and it is ignored.
        var title = device.ToString() ?? Loc.Unknown;

        switch (device.Device)
        {
            case Gen24System gen24System:
                pagePresenter.Show<InverterDetailsView>(device.Key, title, view => view.ViewModel.Gen24System = gen24System);
                break;

            // The battery view needs the inverter, because the net state of charge and the net capacity live there.
            // It must not be looked up from the Gen24Storage of the menu entry: Gen24System.CopyFrom replaces
            // Sensors on every update, so that object is a stale snapshot by the time the entry is clicked.
            case Gen24Storage when UpdateService.BatteryGen24System is { } batteryInverter:
                pagePresenter.Show<BatteryDetailsView>(device.Key, title, view => view.ViewModel.Gen24System = batteryInverter);
                break;

            // Same here: the meter of the menu entry is replaced on every update, so the view binds the live
            // UpdateService.SmartMeter instead of the object handed in - there is nothing to hand it.
            case Gen24PowerMeter3P:
                pagePresenter.Show<SmartMeterDetailsView>(device.Key, title, _ => { });
                break;

            case WattPilot wattPilot:
                pagePresenter.Show<WattPilotDetailsView>(device.Key, title, view => view.ViewModel.WattPilot = wattPilot);
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

        _ = ShowPath(path, updatesAddress: false);
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
