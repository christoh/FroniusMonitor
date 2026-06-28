using System.Net.NetworkInformation;
using System.Reflection;
using De.Hochstaetter.Fronius.Models.Settings;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace De.Hochstaetter.FroniusMonitor;

public partial class App
{
    private static readonly Mutex mutex = new(true, $"{Environment.UserName}_HomeAutomationControlCenter");
    private static IElectricityPriceService? electricityPriceService;
    public static bool HaveSettings { get; set; } = true;
    public static readonly IServiceCollection ServiceCollection = new ServiceCollection();
    public static Timer? SolarSystemQueryTimer { get; set; }

    // Captured before we ever override the culture, so "match Windows language" (null) can be restored at runtime.
    public static CultureInfo OsUiCulture { get; } = CultureInfo.CurrentUICulture;

    static App()
    {
        if (!mutex.WaitOne(0, false))
        {
            MessageBox.Show(string.Format(Loc.IsAlreadyRunning, Loc.AppName), Loc.Error, MessageBoxButton.OK, MessageBoxImage.Error);
            Environment.Exit(666);
        }
    }


    public static string AppName => "FroniusMonitor";
    public static string PerUserDataDir => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Hochstätter", AppName);
    public static string SettingsFileName => Path.Combine(PerUserDataDir, "Settings.fms");
    public static string LogFileName => Path.Combine(PerUserDataDir, "Log.txt");
    public static string VersionString => $"{ThisAssembly.Git.SemVer.Major}.{ThisAssembly.Git.SemVer.Minor}.{ThisAssembly.Git.SemVer.Patch}{ThisAssembly.Git.SemVer.DashLabel}";
    public static string ShortVersionString => $"{ThisAssembly.Git.SemVer.Major}.{ThisAssembly.Git.SemVer.Minor}";
    public static DateTimeOffset CommitTimeUtc { get; } = DateTimeOffset.Parse(ThisAssembly.Git.CommitDate);
    public static string CommitTimeString => $"{CommitTimeUtc.UtcDateTime} UTC";
    public static string CopyrightMessage => FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).LegalCopyright ?? string.Empty;
    public static string BranchName => ThisAssembly.Git.Branch;

    // ReSharper disable once HeuristicUnreachableCode
    public static string GitCommitId => ThisAssembly.Git.IsDirty ? "(Developer Build)" : ThisAssembly.Git.Commit;

    public static Settings Settings { get; set; } = null!;

    public static void ApplyLanguage(string? language)
    {
        var culture = string.IsNullOrWhiteSpace(language) ? OsUiCulture : new CultureInfo(language);
        Loc.Culture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CultureInfo.CurrentUICulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;
        Wpf.Localization.LocalizationSource.Instance.Culture = culture;
        CultureNotifier.NotifyCultureChanged();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var context = SynchronizationContext.Current ?? throw new InvalidOperationException($"No {nameof(SynchronizationContext)} in {nameof(OnStartup)}");

        DispatcherUnhandledException += OnUnhandledException;

        IoC.Update(ServiceCollection.AddSingleton<IAesKeyProvider, AesKeyProvider>().BuildServiceProvider());
        Directory.CreateDirectory(PerUserDataDir);

        try
        {
            Settings.Load().GetAwaiter().GetResult();
        }
        catch
        {
            HaveSettings = false;

            Settings = new()
            {
                DriftFileName = Path.Combine(PerUserDataDir, "Drifts.xml"),
            };

            Settings.Save().GetAwaiter().GetResult();
        }

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel
#if DEBUG
            .Debug()
#else
            .Information()
#endif
            .Enrich.WithComputed("SourceContextName", "Substring(SourceContext, LastIndexOf(SourceContext, '.') + 1)")
            .WriteTo.File
            (
                LogFileName,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] ({SourceContextName:l}) {Message:lj}{NewLine}{Exception}",
                formatProvider: CultureInfo.InvariantCulture,
                rollingInterval: RollingInterval.Day,
                retainedFileTimeLimit: TimeSpan.FromDays(7)
            )
            //applyThemeToRedirectedOutput: true,
            //theme: AnsiConsoleTheme.Sixteen
            .CreateLogger();


        var injector = ServiceCollection
                .AddLogging(b => b.AddSerilog())
                .AddScoped<IGen24Service, Gen24Service>()
                .AddSingleton<IFritzBoxService, FritzBoxService>()
                .AddSingleton(context)
                .AddSingleton<IDataCollectionService, DataCollectionService>()
                .AddSingleton<MainWindow>()
                .AddSingleton<MainViewModel>()
                .AddSingleton<IGen24JsonService, Gen24JsonService>()
                .AddSingleton<IWattPilotService, WattPilotService>()
                .AddSingleton<IToshibaHvacService, ToshibaHvacService>()
                .AddSingleton<ISmartMeterImportService, BayernWerkImportService>()
                .AddSingleton<SettingsBase>(Settings)
                .AddTransient<IElectricityPriceService>(_ =>
                {
                    var newType = Settings.ElectricityPrice.Service switch
                    {
                        ElectricityPriceService.WattPilot => typeof(WattPilotElectricityService),
                        ElectricityPriceService.Awattar => typeof(AwattarService),
                        _ => throw new NotSupportedException($"Unknown electricity price service. Must be any of {string.Join(", ", Enum.GetNames(typeof(ElectricityPriceService)))}"),
                    };

                    if (electricityPriceService is IDisposable disposable && electricityPriceService.GetType() != newType)
                    {
                        disposable.Dispose();
                    }

                    if (electricityPriceService?.GetType() == newType)
                    {
                        return electricityPriceService;
                    }

                    electricityPriceService = Activator.CreateInstance(newType) as IElectricityPriceService ?? throw new InvalidCastException($"Cannot cast {newType.Name} to {nameof(IElectricityPriceService)}");

                    if (electricityPriceService.CanSetPriceRegion && electricityPriceService.GetSupportedPriceZones().GetAwaiter().GetResult().Contains(Settings.ElectricityPrice.PriceRegion))
                    {
                        electricityPriceService.PriceRegion = Settings.ElectricityPrice.PriceRegion;
                    }

                    return electricityPriceService;
                })
                .AddTransient<EventLogView>()
                .AddTransient<BatteryDetailsView>()
                .AddTransient<SmartMeterDetailsView>()
                .AddTransient<SelfConsumptionOptimizationView>()
                .AddTransient<ModbusView>()
                .AddTransient<SettingsView>()
                .AddTransient<WattPilotSettingsView>()
                .AddTransient<PriceView>()
                .AddTransient<PriceComponentsView>()
                .AddTransient<AboutView>()
                .AddTransient<WattPilotDetailsView>()
                .AddTransient<InverterSettingsView>()
                .AddTransient<InverterDetailsView>()
                .AddTransient<NewConnectedInverterView>()
                .AddTransient<EventLogViewModel>()
                .AddTransient<SelfConsumptionOptimizationViewModel>()
                .AddTransient<ModbusViewModel>()
                .AddTransient<SettingsViewModel>()
                .AddTransient<WattPilotSettingsViewModel>()
                .AddTransient<PriceViewModel>()
                .AddTransient<InverterSettingsViewModel>()
                .AddTransient<InverterDetailsViewModel>()
                .AddTransient<BatteryDetailsViewModel>()
                .AddTransient<WattPilotDetailsViewModel>()
                .AddTransient<SmartMeterDetailsViewModel>()
                .AddTransient<NewConnectedInverterViewModel>()
                .BuildServiceProvider()
            ;

        IoC.Update(injector);
        IoC.Get<IDataCollectionService>().FroniusUpdateRate = Settings.FroniusUpdateRate;

        try
        {
            ApplyLanguage(Settings.Language);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, Loc.Warning, MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        NetworkChange.NetworkAddressChanged += OnNetworkAddressChanged;
        var mainWindow = IoC.Get<MainWindow>();
        //#if DEBUG
        //mainWindow.WindowState = WindowState.Minimized;
        //#endif
        mainWindow.Show();
    }

    private static async void OnNetworkAddressChanged(object? sender, EventArgs e)
    {
        try
        {
            var hvacService = IoC.Get<IDataCollectionService>().HvacService;

            if (!hvacService.IsConnected)
            {
                await hvacService.Stop().ConfigureAwait(false);
            }
        }
        catch
        {
            // async void must be caught
        }
    }

    private static void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        e.Handled = true;
        e.Dispatcher.InvokeAsync(() => MessageBox.Show(e.Exception.ToString(), Loc.Warning, MessageBoxButton.OK, MessageBoxImage.Error));
    }

    private void ValidationErrorVisibilityChanged(object sender, DependencyPropertyChangedEventArgs _)
    {
        if (sender is DependencyObject d && d.GetVisualParent<ItemsControl>()?.DataContext is ViewModelBase viewModelBase)
        {
            viewModelBase.NotifyOfPropertyChange(nameof(ViewModelBase.HasVisibleNotifiedValidationErrors));
            viewModelBase.NotifyOfPropertyChange(nameof(ViewModelBase.VisibleNotifiedValidationErrors));
        }
    }
}
