namespace FroniusPhone;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .RegisterServices()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        #if DEBUG
        builder.Logging.AddDebug();
        #endif
        return builder.Build();
    }

    private static MauiAppBuilder RegisterServices(this MauiAppBuilder builder)
    {
        builder.Services
            .AddSingleton<ISolarSystemService, SolarSystemService>()
            .AddSingleton<IWebClientService, WebClientService>()
            .AddSingleton<ISolarSystemService, SolarSystemService>()
            .AddSingleton<IAesKeyProvider, AesKeyProvider>()
            .AddSingleton<IWattPilotService, WattPilotService>()
            .AddSingleton<SettingsBase, Settings>()
            .AddSingleton<IGen24JsonService, Gen24JsonService>()
            .AddSingleton<AppShell>()
            .AddSingleton<Overview>()
            .AddSingleton<OverviewViewModel>()
            .AddSingleton<SettingsPage>()
            ;

        return builder;
    }
}
