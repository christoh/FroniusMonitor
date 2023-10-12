using De.Hochstaetter.Fronius.Models.Settings;
using Microsoft.Extensions.DependencyInjection;

namespace De.Hochstaetter.FroniusMonitor;

public partial class App
{
    public const double ZoomFactor = 1.025;
    private static readonly Mutex mutex = new(true, $"{Environment.UserName}_HomeAutomationControlCenter");
    public static bool HaveSettings = true;
    public static readonly IServiceCollection ServiceCollection = new ServiceCollection();
    public static Timer? SolarSystemQueryTimer;

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

    public static Settings Settings { get; set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

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
                DriftFileName = Path.Combine(PerUserDataDir, "Drifts.xml")
            };

            Settings.Save().GetAwaiter().GetResult();
        }

        var injector = ServiceCollection
            .AddScoped<IGen24Service, Gen24Service>()
            .AddSingleton<IFritzBoxService, FritzBoxService>()
            .AddSingleton(SynchronizationContext.Current!)
            .AddSingleton<IDataCollectionService, DataCollectionService>()
            .AddSingleton<MainWindow>()
            .AddSingleton<MainViewModel>()
            .AddSingleton<IGen24JsonService, Gen24JsonService>()
            .AddSingleton<IWattPilotService, WattPilotService>()
            .AddSingleton<IToshibaHvacService, ToshibaHvacService>()
            .AddSingleton<SettingsShared>(Settings)
            .AddTransient<EventLogView>()
            .AddTransient<SelfConsumptionOptimizationView>()
            .AddTransient<ModbusView>()
            .AddTransient<SettingsView>()
            .AddTransient<WattPilotSettingsView>()
            .AddTransient<InverterSettingsView>()
            .AddTransient<EventLogViewModel>()
            .AddTransient<SelfConsumptionOptimizationViewModel>()
            .AddTransient<ModbusViewModel>()
            .AddTransient<SettingsViewModel>()
            .AddTransient<WattPilotSettingsViewModel>()
            .AddTransient<InverterSettingsViewModel>()
            .BuildServiceProvider()
            ;

        IoC.Update(injector);
        IoC.Get<IDataCollectionService>().FroniusUpdateRate = Settings.FroniusUpdateRate;

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

        SystemEvents.PowerModeChanged += OnPowerModeChanged;
        IoC.Get<MainWindow>().Show();
    }

    private static void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
    {
        switch (e.Mode)
        {
            case PowerModes.Resume:
                IoC.Get<IDataCollectionService>().HvacService.Stop();
                break;
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
