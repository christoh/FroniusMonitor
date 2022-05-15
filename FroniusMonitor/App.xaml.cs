namespace De.Hochstaetter.FroniusMonitor;

public partial class App
{
    public const double ZoomFactor = 1.025;
    public static string AppName => "FroniusMonitor";
    public static string PerUserDataDir => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Hochstätter", AppName);
    public static string SettingsFileName => Path.Combine(PerUserDataDir, "Settings.fms");

    public static readonly IUnityContainer Container = new UnityContainer();

    public static Settings Settings { get; set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        // Thread.CurrentThread.CurrentUICulture= new CultureInfo("de-CH");
        base.OnStartup(e);

        Container
            .RegisterSingleton<IWebClientService, WebClientService>()
            .RegisterSingleton<ISolarSystemService, SolarSystemService>()
            .RegisterSingleton<MainWindow>()
            .RegisterSingleton<MainViewModel>()
            .RegisterSingleton<IGen24JsonService, Gen24JsonService>()
            .RegisterType<EventLogView>()
            .RegisterType<EventLogViewModel>()
            .RegisterType<SelfConsumptionOptimizationViewModel>()
            .RegisterType<SelfConsumptionOptimizationView>()
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

        IoC.Get<MainWindow>().Show();
    }
}
