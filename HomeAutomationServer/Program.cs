using System.Net;
using System.Runtime.CompilerServices;
using De.Hochstaetter.Fronius.Models.Settings;
using De.Hochstaetter.HomeAutomationServer.Crypto;
using De.Hochstaetter.HomeAutomationServer.Models.Settings;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace De.Hochstaetter.HomeAutomationServer;

internal class Program
{
    private static SunSpecMeterService? server;
    private static ILogger? logger;
    private static IReadOnlyList<string> arguments = null!;

    private static void Main(string[] args)
    {
        Program.arguments = args;

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel
            .Verbose()
            .WriteTo
            .Console(formatProvider: CultureInfo.InvariantCulture)
            .CreateLogger();

        Settings? settings = null;
        Exception? settingsLoadException = null;

        try
        {
            settings = Settings.Load();
        }
        catch (FileNotFoundException ex)
        {
            settings = new();
            settings.FritzBoxConnections.Add(new WebConnection { BaseUrl = "http://192.168.178.xxx", UserName = "SomeName", Password = "Swordfish"});
            settings.ModbusMappings.Add(new ModbusMapping());
            settings.Save();
            settingsLoadException = ex;
        }
        catch (Exception ex)
        {
            settingsLoadException = ex;
        }

        var serviceCollection = new ServiceCollection();

        serviceCollection
            .AddTransient<IFritzBoxService, FritzBoxService>()
            .AddSingleton<IAesKeyProvider, AesKeyProvider>()
            .AddSingleton<SunSpecMeterService>()
            .AddTransient<ISunSpecMeterClient, SunSpecMeterClient>()
            .AddLogging(builder => builder.AddSerilog())
            ;

        if (settings != null)
        {
            serviceCollection.AddSingleton(settings);
        }

        var serviceProvider = serviceCollection.BuildServiceProvider();
        IoC.Update(serviceProvider);
        logger = IoC.Get<ILogger<Program>>();

        switch (settingsLoadException)
        {
            case FileNotFoundException:
                logger.LogWarning($"{Settings.SettingsFileName} does not exist. Created a default file.");
                break;

            case not null:
                logger.LogCritical($"{Settings.SettingsFileName} could not be loaded. Must exit");
                Environment.ExitCode = settingsLoadException.HResult;
                return;
        }

        MainAsync().GetAwaiter().GetResult();
    }

    private static async Task MainAsync()
    {

        server = IoC.Get<SunSpecMeterService>();
        var settings = IoC.Get<Settings>();

        logger.LogInformation("Starting server");
        await server.StartAsync(new IPEndPoint(IPAddress.Parse(settings.ServerIpAddress), settings.ServerPort)).ConfigureAwait(false);

        var fritzBoxService = IoC.Get<IFritzBoxService>();
        fritzBoxService.Connection = new WebConnection { BaseUrl = "http://192.168.44.11", UserName = "FroniusMonitor", Password = arguments[0] };

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
