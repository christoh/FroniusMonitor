using De.Hochstaetter.Fronius.Models.Settings;

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
            .AddSingleton<IDataCollectionService, DataCollectionService>()
            .AddSingleton<IGen24Service, Gen24Service>()
            .AddSingleton<IFritzBoxService, FritzBoxService>()
            .AddSingleton<IAesKeyProvider, AesKeyProvider>()
            .AddSingleton<IWattPilotService, WattPilotService>()
            .AddSingleton<Settings>()
            .AddSingleton<IGen24JsonService, Gen24JsonService>()
            .AddSingleton<AppShell>()
            .AddSingleton<Overview>()
            .AddSingleton<OverviewViewModel>()
            .AddSingleton<SettingsPage>()
            .AddSingleton<SettingsViewModel>()
            .AddSingleton<SettingsBase>(new Settings())
            .AddSingleton<IToshibaHvacService, ToshibaHvacService>()
            .AddSingleton(SynchronizationContext.Current ?? throw new InvalidOperationException("No Context"))
        ;

        return builder;
    }
}
