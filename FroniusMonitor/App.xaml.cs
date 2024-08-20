using De.Hochstaetter.Fronius.Models.Settings;
using Microsoft.Extensions.DependencyInjection;

namespace De.Hochstaetter.FroniusMonitor
{
    public partial class App
    {
        private static readonly Mutex mutex = new(true, $"{Environment.UserName}_HomeAutomationControlCenter");
        public static bool HaveSettings { get; set; } = true;
        public static readonly IServiceCollection ServiceCollection = new ServiceCollection();
        public static Timer? SolarSystemQueryTimer { get; set; }

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
                    DriftFileName = Path.Combine(PerUserDataDir, "Drifts.xml")
                };

                Settings.Save().GetAwaiter().GetResult();
            }

            var injector = ServiceCollection
                    .AddScoped<IGen24Service, Gen24Service>()
                    .AddSingleton<IFritzBoxService, FritzBoxService>()
                    .AddSingleton(context)
                    .AddSingleton<IDataCollectionService, DataCollectionService>()
                    .AddSingleton<MainWindow>()
                    .AddSingleton<MainViewModel>()
                    .AddSingleton<IGen24JsonService, Gen24JsonService>()
                    .AddSingleton<IWattPilotService, WattPilotService>()
                    .AddSingleton<IToshibaHvacService, ToshibaHvacService>()
                    .AddSingleton<IElectricityPriceService, WattPilotElectricityService>()
                    .AddSingleton<SettingsBase>(Settings)
                    .AddTransient<EventLogView>()
                    .AddTransient<BatteryDetailsView>()
                    .AddTransient<SmartMeterDetailsView>()
                    .AddTransient<SelfConsumptionOptimizationView>()
                    .AddTransient<ModbusView>()
                    .AddTransient<SettingsView>()
                    .AddTransient<WattPilotSettingsView>()
                    .AddTransient<PriceView>()
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
            var mainWindow = IoC.Get<MainWindow>();
            #if DEBUG
            mainWindow.WindowState = WindowState.Minimized;
            #endif
            mainWindow.Show();
        }

        private static async void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            switch (e.Mode)
            {
                case PowerModes.Resume:
                    await IoC.Get<IDataCollectionService>().HvacService.Stop().ConfigureAwait(false);
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
}
