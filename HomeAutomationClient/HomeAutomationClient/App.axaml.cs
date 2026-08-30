using De.Hochstaetter.HomeAutomationClient.Services;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
    /// The six shades that Fluent paints the pointer-over, pressed and disabled states of every accented control
    /// with. Windows delivers them together with the accent color and Avalonia passes them on; where we bring our
    /// own accent color, we have to build them.
    /// </summary>
    /// <remarks>
    /// The factors are the ones Windows itself applies, read off its palette for the accent color #FFDA3B01:
    /// a lighter shade raises the value to its maximum and takes the saturation down, a darker one scales the
    /// value down and keeps the saturation. They reproduce that palette to about one step of 255. Scaling rather
    /// than subtracting matters: a dark accent color would run into black otherwise.
    /// </remarks>
    private static readonly (string Key, double ValueFactor, double SaturationFactor)[] accentShades =
    [
        ("SystemAccentColorLight1", 1.165, 0.835),
        ("SystemAccentColorLight2", 1.165, 0.589),
        ("SystemAccentColorLight3", 1.165, 0.335),
        ("SystemAccentColorDark1", 0.7385, 1),
        ("SystemAccentColorDark2", 0.5505, 1),
        ("SystemAccentColorDark3", 0.3211, 1),
    ];

    /// <summary>
    /// Replaces the accent palette of the Fluent theme with the accent color of the OS, where the platform head
    /// could detect one. It is the whole palette and not a brush of our own, because the theme paints far more
    /// with it than our dialog title bar: the thumb of a slider, a check box, a focus rectangle, a progress bar.
    /// </summary>
    /// <remarks>
    /// Where a head detected nothing, we deliberately touch nothing. On Windows Avalonia fills the palette from
    /// the OS itself, including the six shades, and keeps it up to date while the app runs - an override here
    /// would freeze the accent color at the value it had when the app started.
    /// </remarks>
    private void SetAccentColor()
    {
        if (PlatformStartup.AccentColor is not { } platformColor)
        {
            return;
        }

        var accentColor = Color.FromArgb(platformColor.A, platformColor.R, platformColor.G, platformColor.B);
        Resources["SystemAccentColor"] = accentColor;

        foreach (var (key, valueFactor, saturationFactor) in accentShades)
        {
            Resources[key] = Shade(accentColor, valueFactor, saturationFactor);
        }
    }

    private static Color Shade(Color accentColor, double valueFactor, double saturationFactor)
    {
        var hsv = accentColor.ToHsv();
        return new HsvColor(hsv.A, hsv.H, Math.Clamp(hsv.S * saturationFactor, 0, 1), Math.Clamp(hsv.V * valueFactor, 0, 1)).ToRgb();
    }

    public override void OnFrameworkInitializationCompleted()
    {
        SetAccentColor();

        ServiceCollection ??= new ServiceCollection();

        // Only the browser head brings an address bar of its own; everywhere else the addresses are collected.
        ServiceCollection.TryAddSingleton<IUriService, FakeUriService>();

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