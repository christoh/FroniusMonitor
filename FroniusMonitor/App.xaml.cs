using De.Hochstaetter.FroniusMonitor.Models.CarCharging;

namespace De.Hochstaetter.FroniusMonitor;

public partial class App
{
    public const double ZoomFactor = 1.025;
    public static string AppName => "FroniusMonitor";
    public static string PerUserDataDir => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Hochstätter", AppName);
    public static string SettingsFileName => Path.Combine(PerUserDataDir, "Settings.fms");
    public static Timer? SolarSystemQueryTimer;

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
            Settings.Load().Wait();
        }
        catch
        {
            Settings = new();
            Settings.Save().Wait();
        }

        if (!string.IsNullOrWhiteSpace(Settings.Language))
        {
            try
            {
                Thread.CurrentThread.CurrentUICulture = new CultureInfo(Settings.Language);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Fronius.Localization.Resources.Warning, MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        IoC.Get<MainWindow>().Show();
    }

    private static void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        e.Handled = true;
        e.Dispatcher.InvokeAsync(() => MessageBox.Show(e.Exception.ToString(), Fronius.Localization.Resources.Warning, MessageBoxButton.OK, MessageBoxImage.Error));
    }
}
