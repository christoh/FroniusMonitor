namespace De.Hochstaetter.HomeAutomationClient
{
    public partial class App : Application
    {
        public static IServiceCollection? ServiceCollection { get; set; }

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            ServiceCollection ??= new ServiceCollection();

            ServiceCollection
                .AddSingleton<MainView>()
                .AddSingleton<MainViewModel>()
                .AddTransient<GaugeTestView>()
                .AddTransient<GaugeTestViewModel>()
                .AddTransient<LinearGaugeTestView>()
                .AddTransient<LinearGaugeTestViewModel>()
                .AddTransient<UiDemoView>()
                .AddTransient<UiDemoViewModel>()

                .AddTransient<HomeAutomationServerConnection>()

                .AddSingleton<IServerBasedAesKeyProvider, AesKeyProvider>()
                .AddSingleton<IAesKeyProvider, IAesKeyProvider>(provider => IoC.GetRegistered<IServerBasedAesKeyProvider>())
                .AddSingleton<IWebClientService, WebClientService>()
                .AddSingleton<IGen24LocalizationService, Gen24LocalizationService>()
                ;

            var serviceProvider = ServiceCollection.BuildServiceProvider();
            IoC.Update(serviceProvider);

            switch (ApplicationLifetime)
            {
                case IClassicDesktopStyleApplicationLifetime desktop:
                    // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
                    // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
                    DisableAvaloniaDataAnnotationValidation();
                    desktop.MainWindow = IoC.Get<MainWindow>();
                    break;

                case ISingleViewApplicationLifetime singleViewPlatform:
                    singleViewPlatform.MainView = IoC.Get<MainView>();
                    break;
            }

            base.OnFrameworkInitializationCompleted();
        }

        private void DisableAvaloniaDataAnnotationValidation()
        {
            // Get an array of plugins to remove
            var dataValidationPluginsToRemove = BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

            // remove each entry found
            foreach (var plugin in dataValidationPluginsToRemove)
            {
                BindingPlugins.DataValidators.Remove(plugin);
            }
        }
    }
}
