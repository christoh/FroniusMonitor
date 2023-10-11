using System.Net;
using System.Text.RegularExpressions;
using De.Hochstaetter.Fronius.Extensions;
using De.Hochstaetter.Fronius.Models.Settings;
using De.Hochstaetter.HomeAutomationServer.Crypto;
using De.Hochstaetter.HomeAutomationServer.DataCollectors;

namespace De.Hochstaetter.HomeAutomationServer;

internal class Program
{
    private static SunSpecMeterService? server;
    private static ILogger? logger;
    private static IReadOnlyList<string> Arguments { get; set; } = null!;

    private static void Main(string[] args)
    {
        Arguments = args;

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
            
            settings.FritzBoxConnections.Add(new WebConnection
            {
                BaseUrl = "http://192.168.178.xxx",
                UserName = string.Empty,
                Password = string.Empty,
            });
            
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
            .AddSingleton<FritzBoxDataCollector>()
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
                logger.LogCritical($"{Settings.SettingsFileName} could not be loaded. Must exit.");
                Environment.ExitCode = settingsLoadException.HResult;
                return;
        }

        RunServicesAsync().GetAwaiter().GetResult();

        while (true)
        {
            Thread.Sleep(5000);
        }
    }

    private static async Task RunServicesAsync()
    {
        logger = IoC.Get<ILogger<Program>>();
        server = IoC.Get<SunSpecMeterService>();
        var settings = IoC.Get<Settings>();

        if (Arguments.Count > 0)
        {
            var regex = new Regex(@"^(?<UserName>(?!:).+):(?<Password>(?!:).+)$");
            var match = regex.Match(Arguments[0]);

            if (match.Success)
            {
                var userName = match.Groups["UserName"].Value;
                var password = match.Groups["Password"].Value;

                settings.FritzBoxConnections.Where(c => c.UserName == userName).Apply(c => { c.Password = password; });

                await settings.SaveAsync().ConfigureAwait(false);
                Environment.Exit(0);
                return;
            }
        }

        logger.LogInformation($"Starting server on {settings.ServerIpAddress}:{settings.ServerPort}");
        await server.StartAsync(new IPEndPoint(IPAddress.Parse(settings.ServerIpAddress), settings.ServerPort)).ConfigureAwait(false);
        var fritzBoxDataCollector = IoC.Get<FritzBoxDataCollector>();
        await fritzBoxDataCollector.StartAsync();
    }
}
