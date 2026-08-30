using De.Hochstaetter.HomeAutomationClient.Services;
using InverterDetailsView = De.Hochstaetter.HomeAutomationClient.Views.InverterDetailsView;

namespace De.Hochstaetter.HomeAutomationClient;

public partial class App : Application
{
    public static IServiceCollection? ServiceCollection { get; set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    /// <summary>
    /// The platform heads detect the accent color of the operating system before Avalonia starts, because most of
    /// them need APIs that Avalonia does not expose. Where a head found none, the SystemAccentColor of the Fluent
    /// theme applies, which is the accent color of the OS on Windows and a fixed blue everywhere else.
    /// </summary>
    private void SetAccentColor()
    {
        Color? accentColor = null;

        if (PlatformStartup.AccentColor is { } platformColor)
        {
            accentColor = Color.FromArgb(platformColor.A, platformColor.R, platformColor.G, platformColor.B);
        }
        else if (this.TryFindResource("SystemAccentColor", ActualThemeVariant, out var systemAccent) && systemAccent is Color systemAccentColor)
        {
            accentColor = systemAccentColor;
        }

        if (accentColor is { } color)
        {
            Resources["AppAccentColor"] = color;
            Resources["AppAccentBrush"] = new SolidColorBrush(color);
        }
    }

    public override void OnFrameworkInitializationCompleted()
    {
        SetAccentColor();

        ServiceCollection ??= new ServiceCollection();

        ServiceCollection
            .AddSingleton<MainView>()
            .AddSingleton<MainViewModel>()
            .AddTransient<GaugeTestView>()
            .AddTransient<GaugeTestViewModel>()
            .AddTransient<LinearGaugeTestView>()
            .AddTransient<LinearGaugeTestViewModel>()
            .AddSingleton<DashboardView>()
            .AddSingleton<DashboardViewModel>()
            .AddSingleton<InverterDetailsView>()
            .AddSingleton<InverterDetailsViewModel>()
            .AddSingleton<BatteryDetailsView>()
            .AddSingleton<BatteryDetailsViewModel>()
            .AddSingleton<SmartMeterDetailsView>()
            .AddSingleton<SmartMeterDetailsViewModel>()
            .AddSingleton<WattPilotDetailsView>()
            .AddSingleton<WattPilotDetailsViewModel>()

            .AddTransient<HomeAutomationServerConnection>()

            .AddSingleton<IServerBasedAesKeyProvider, AesKeyProvider>()
            .AddSingleton<IAesKeyProvider, IAesKeyProvider>(provider => IoC.GetRegistered<IServerBasedAesKeyProvider>())
            .AddSingleton<IWebClientService, WebClientService>()
            .AddSingleton<IGen24LocalizationService, Gen24LocalizationService>()
            .AddSingleton<IUpdateService, UpdateService>()
            ;

        var serviceProvider = ServiceCollection.BuildServiceProvider();
        IoC.Update(serviceProvider);

        switch (ApplicationLifetime)
        {
            case IClassicDesktopStyleApplicationLifetime desktop:
                // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
                // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
                //DisableAvaloniaDataAnnotationValidation();
                desktop.MainWindow = IoC.Get<MainWindow>();
                break;

            case ISingleViewApplicationLifetime singleViewPlatform:
                singleViewPlatform.MainView = IoC.Get<MainView>();
                break;
        }

        base.OnFrameworkInitializationCompleted();
    }

    //private static void DisableAvaloniaDataAnnotationValidation()
    //{
    //    // Get an array of plugins to remove
    //    var dataValidationPluginsToRemove = BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

    //    // remove each entry found
    //    foreach (var plugin in dataValidationPluginsToRemove)
    //    {
    //        BindingPlugins.DataValidators.Remove(plugin);
    //    }
    //}
}