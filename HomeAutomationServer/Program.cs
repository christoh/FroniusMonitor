using De.Hochstaetter.Fronius.Models.Settings;
using De.Hochstaetter.HomeAutomationServer.Crypto;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace De.Hochstaetter.HomeAutomationServer;

internal class Program
{
    private static SunSpecMeterService? server;
    private static ILogger? logger;

    private static void Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel
            .Verbose()
            .WriteTo
            .Console(formatProvider: CultureInfo.InvariantCulture)
            .CreateLogger();

        var serviceCollection = new ServiceCollection();

        serviceCollection
            .AddScoped<IGen24Service, Gen24Service>()
            .AddSingleton<IFritzBoxService, FritzBoxService>()
            .AddSingleton<IAesKeyProvider, AesKeyProvider>()
            .AddSingleton<IDataCollectionService, DataCollectionService>()
            .AddSingleton<IGen24JsonService, Gen24JsonService>()
            .AddSingleton<IWattPilotService, WattPilotService>()
            .AddSingleton<IToshibaHvacService, ToshibaHvacService>()
            .AddSingleton<SunSpecMeterService>()
            .AddTransient<ISunSpecMeterClient, SunSpecMeterClient>()
            .AddLogging(builder => builder.AddSerilog())
            .BuildServiceProvider()
            ;

        var serviceProvider = serviceCollection.BuildServiceProvider();
        IoC.Update(serviceProvider);

        logger = IoC.Get<ILogger<Program>>();

        logger.LogInformation($"Arguments: {string.Join(" ", args)}");

        server = IoC.Get<SunSpecMeterService>();
        logger.LogInformation("Starting server");
        server.StartAsync().GetAwaiter().GetResult();

        var fritzBoxService = IoC.Get<IFritzBoxService>();
        fritzBoxService.Connection = new WebConnection { BaseUrl = "http://192.168.44.11", UserName = "FroniusMonitor", Password = args[0] };

        while (true)
        {
            Thread.Sleep(5000);
            IReadOnlyList<IPowerMeter1P> powerMeters;

            try
            {
                powerMeters = fritzBoxService.GetFritzBoxDevices().GetAwaiter().GetResult().DevicesWithPowerMeter;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
                continue;
            }

            UpdateServer(powerMeters, 2, "087610004279"); // Lampe Küchenschrank
            UpdateServer(powerMeters, 3, "087610004275"); // Lampe Sofa Wohnzimmer
            UpdateServer(powerMeters, 4, "087610004280"); // Computer Bettina
            UpdateServer(powerMeters, 5, "116300301014"); // Fernseher Wohnzimmer
            UpdateServer(powerMeters, 6, "116300301015"); // Computer Christoph
            UpdateServer(powerMeters, 7, "116300301009"); // Stehlampe Medienzimmer
        }
    }

    private static void UpdateServer(IReadOnlyList<IPowerMeter1P> powerMeters, byte modbusAddress, string serialNumber)
    {
        var powerMeter = powerMeters.SingleOrDefault(p => string.Equals(p.SerialNumber, serialNumber, StringComparison.Ordinal));

        if (powerMeter == null)
        {
            logger?.LogWarning($"Power meter '{serialNumber}' not found");
            return;
        }

        server?.UpdateMeter(powerMeter, modbusAddress);
    }
}
