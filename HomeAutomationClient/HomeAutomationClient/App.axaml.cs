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

        // AppAccentColor has one value per theme, and what SetAccentColor writes into the palette are plain
        // colors that apply to both. Nothing would pick the other value up on its own, so the palette is built
        // again whenever the user switches between light and dark.
        ActualThemeVariantChanged += (_, _) => SetAccentColor();
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
    /// Fills the accent palette of the Fluent theme: with the accent color of the OS where the platform head
    /// detected one, and with the AppAccentColor of App.axaml everywhere else. It is the whole palette and not a
    /// brush of our own, because the theme paints far more with it than our dialog title bar: the thumb of a
    /// slider, a check box, a focus rectangle, a progress bar.
    /// </summary>
    /// <remarks>
    /// On Windows nothing is touched at all - see <see cref="PlatformStartup.AccentColorFollowsOs"/>. The
    /// AppAccentColor is therefore the accent color of iOS, the browser, macOS and Linux, which have none of
    /// their own, while Windows follows its OS and Android its wallpaper.
    /// </remarks>
    private void SetAccentColor()
    {
        if (PlatformStartup.AccentColorFollowsOs)
        {
            return;
        }

        if (GetAccentColor() is not { } accentColor)
        {
            return;
        }

        Resources["SystemAccentColor"] = accentColor;

        foreach (var (key, valueFactor, saturationFactor) in accentShades)
        {
            Resources[key] = Shade(accentColor, valueFactor, saturationFactor);
        }
    }

    /// <summary>
    /// What the head detected, or the AppAccentColor of App.axaml. Null only where that resource is missing or is
    /// not a color, in which case the SystemAccentColor of the Fluent theme stays as it is.
    /// </summary>
    private Color? GetAccentColor()
    {
        if (PlatformStartup.AccentColor is { } platformColor)
        {
            return Color.FromArgb(platformColor.A, platformColor.R, platformColor.G, platformColor.B);
        }

        return Resources.TryGetResource("AppAccentColor", ActualThemeVariant, out var resource) && resource is Color appColor ? appColor : null;
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