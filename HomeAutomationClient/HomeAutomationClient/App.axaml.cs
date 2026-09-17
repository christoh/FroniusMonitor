using De.Hochstaetter.HomeAutomationClient.Services;
using De.Hochstaetter.HomeAutomationClient.Services.Presentation;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Gen24JsonService = De.Hochstaetter.Fronius.Services.Gen24JsonService;
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
        //// Temporary: to change the culture in non-WebAssembly heads.
        //Thread.CurrentThread.CurrentUICulture = new CultureInfo("de-CH");

        ServiceCollection ??= new ServiceCollection();

        // Only the browser head brings an address bar of its own; everywhere else the addresses are collected.
        ServiceCollection.TryAddSingleton<IUriService, FakeUriService>();

        // Without AddLogging there is no ILoggerFactory, so any constructor asking for an ILogger<T> cannot be
        // resolved and activating the service throws. Fronius has used ILogger throughout for a long time; this
        // head simply never registered a factory for it. Where the records go is up to the head - see
        // PlatformStartup.ConfigureLogging.
        ServiceCollection.AddLogging(builder =>
        {
            if (PlatformStartup.ConfigureLogging is { } configureLogging)
            {
                configureLogging(builder);
                return;
            }

            builder.AddDebug();
        });

        ServiceCollection
            .AddSingleton<MainView>()
            .AddSingleton<MainViewModel>()
            .AddTransient<GaugeTestView>()
            .AddTransient<GaugeTestViewModel>()
            .AddTransient<LinearGaugeTestView>()
            .AddTransient<LinearGaugeTestViewModel>()
            .AddSingleton<DashboardView>()
            .AddSingleton<DashboardViewModel>()
            // One for the app: the house block on the dashboard follows the update service for as long as the app runs.
            .AddSingleton<HouseViewModel>()
            // The detail views and their view models are transient, because the desktop head shows one window per
            // device and each of those windows needs a page and a view model of its own. A head with one view at a
            // time gets the single instance it had before from the presenter, which keeps one page per view type.
            .AddTransient<InverterDetailsView>()
            .AddTransient<InverterDetailsViewModel>()
            .AddTransient<BatteryDetailsView>()
            .AddTransient<BatteryDetailsViewModel>()
            .AddTransient<SmartMeterDetailsView>()
            .AddTransient<SmartMeterDetailsViewModel>()
            .AddTransient<WattPilotDetailsView>()
            .AddTransient<WattPilotDetailsViewModel>()
            // The power flow page is a page like the detail views - a window of its own on the desktop, the main
            // view elsewhere - and follows the update service only while it is loaded.
            .AddTransient<PowerFlowView>()
            .AddTransient<PowerFlowViewModel>()

            .AddTransient<HomeAutomationServerConnection>()

            .AddSingleton<IServerBasedAesKeyProvider, AesKeyProvider>()
            .AddSingleton<IAesKeyProvider, IAesKeyProvider>(provider => IoC.GetRegistered<IServerBasedAesKeyProvider>())
            .AddSingleton<IWebClientService, WebClientService>()

            // The settings models reach for this through IoC when they build an update token. The client only needs
            // it to tell whether anything was changed at all; what actually gets written is worked out server side.
            .AddSingleton<IGen24JsonService, Gen24JsonService>()
            .AddSingleton<IGen24LocalizationService, Gen24LocalizationService>()
            .AddSingleton<IUpdateService, UpdateService>()
            // The same instance under the narrow contract the Toshiba view model takes, so a test can fake that
            // contract while the app sends over the one hub connection.
            .AddSingleton<IToshibaHvacCommander>(provider => provider.GetRequiredService<IUpdateService>())
            // One per air conditioner on the dashboard; ToshibaHvacControl resolves it and hands it the device.
            .AddTransient<ToshibaHvacViewModel>()
            .AddSingleton<IUriLauncher, UriLauncher>()
            ;

        RegisterPresenters(ServiceCollection);

        var serviceProvider = ServiceCollection.BuildServiceProvider();
        IoC.Update(serviceProvider);

        switch (ApplicationLifetime)
        {
            case IClassicDesktopStyleApplicationLifetime desktop:
                // Avoid duplicate validations from both Avalonia and the CommunityToolkit.
                // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
                //DisableAvaloniaDataAnnotationValidation();
                desktop.MainWindow = IoC.Get<MainWindow>();
                // The detail pages and the dialogs are windows of their own here, and the default is to keep the
                // app alive while any window is open. Closing the main window is what ends the app, so an open
                // detail page cannot leave it running with nothing the user can see.
                desktop.ShutdownMode = ShutdownMode.OnMainWindowClose;
                break;

            case ISingleViewApplicationLifetime singleViewPlatform:
                singleViewPlatform.MainView = IoC.Get<MainView>();
                break;
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>
    /// Where dialogs and detail pages appear. The desktop head gives each of them a window of its own; every other
    /// head has one window and shows them inside the main view, which is the only thing a browser can do at all.
    /// </summary>
    /// <remarks>
    /// The lifetime is what says which head this is, and it is set before this runs. Registered under both
    /// contracts as one instance, because the two share the list of what is open: a logout closes all of it, and
    /// the same key must not be handed a second window by the other half.
    /// </remarks>
    private void RegisterPresenters(IServiceCollection services)
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime)
        {
            services
                .AddSingleton<WindowPresenter>()
                .AddSingleton<IDialogPresenter>(provider => provider.GetRequiredService<WindowPresenter>())
                .AddSingleton<IPagePresenter>(provider => provider.GetRequiredService<WindowPresenter>());

            return;
        }

        services
            .AddSingleton<MainViewPresenter>()
            .AddSingleton<IDialogPresenter>(provider => provider.GetRequiredService<MainViewPresenter>())
            .AddSingleton<IPagePresenter>(provider => provider.GetRequiredService<MainViewPresenter>());
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