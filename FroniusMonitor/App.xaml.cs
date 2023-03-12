using Loc = De.Hochstaetter.Fronius.Localization.Resources;

namespace De.Hochstaetter.FroniusMonitor;

public partial class App
{
    public const double ZoomFactor = 1.025;
    public static string AppName => "FroniusMonitor";
    public static string PerUserDataDir => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Hochstätter", AppName);
    public static string SettingsFileName => Path.Combine(PerUserDataDir, "Settings.fms");
    public static Timer? SolarSystemQueryTimer;
    public static bool HaveSettings = true;

    public static readonly IUnityContainer Container = new UnityContainer();

    public static Settings Settings { get; set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        Fronius.IoC.Injector = new IoC();

        DispatcherUnhandledException += OnUnhandledException;

        Container
            .RegisterSingleton<IWebClientService, WebClientService>()
            .RegisterSingleton<ISolarSystemService, SolarSystemService>()
            .RegisterSingleton<MainWindow>()
            .RegisterSingleton<MainViewModel>()
            .RegisterSingleton<IGen24JsonService, Gen24JsonService>()
            .RegisterSingleton<IAesKeyProvider, AesKeyProvider>()
            .RegisterSingleton<IWattPilotService, WattPilotService>()
            .RegisterSingleton<IToshibaAirConditionService, ToshibaAirConditionService>()
            .RegisterType<EventLogView>()
            .RegisterType<EventLogViewModel>()
            .RegisterType<SelfConsumptionOptimizationViewModel>()
            .RegisterType<SelfConsumptionOptimizationView>()
            .RegisterType<ModbusView>()
            .RegisterType<ModbusViewModel>()
            .RegisterType<SettingsView>()
            .RegisterType<SettingsViewModel>()
            .RegisterType<WattPilotSettingsView>()
            .RegisterType<WattPilotSettingsViewModel>()
            ;

        Directory.CreateDirectory(PerUserDataDir);

        try
        {
            Settings.Load().GetAwaiter().GetResult();
        }
        catch
        {
            HaveSettings = false;
            Settings = new();
            Settings.Save().GetAwaiter().GetResult();
        }

        Container.RegisterInstance<SettingsBase>(Settings);
        IoC.Get<ISolarSystemService>().FroniusUpdateRate = Settings.FroniusUpdateRate;

        if (!string.IsNullOrWhiteSpace(Settings.Language))
        {
            try
            {
                Thread.CurrentThread.CurrentUICulture = new CultureInfo(Settings.Language);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Loc.Warning, MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        IoC.Get<MainWindow>().Show();
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
