using System.Net.Sockets;

namespace De.Hochstaetter.HomeAutomationServer;

internal partial class Program
{
    private static ModbusServerService? server;

    [GeneratedRegex("^(?<UserName>(?!:).+):(?<Password>(?!:).+)$", RegexOptions.Compiled)]
    private static partial Regex PasswordRegex();

    private static readonly Regex regex = PasswordRegex();
    private static ILogger? logger;


    private static async Task<int> Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel
#if DEBUG
            .Debug()
#else
            .Information()
#endif
            .Enrich.WithComputed("SourceContextName", "Substring(SourceContext, LastIndexOf(SourceContext, '.') + 1)")
            .WriteTo.Console(outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] ({SourceContextName:l}) {Message:lj}{NewLine}{Exception}", formatProvider: CultureInfo.InvariantCulture)
            .CreateLogger();

        Settings? settings = null;
        Exception? settingsLoadException = null;

        try
        {
            settings = await Settings.LoadAsync().ConfigureAwait(true);
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
            await settings.SaveAsync().ConfigureAwait(true);
            settingsLoadException = ex;
        }
        catch (Exception ex)
        {
            settingsLoadException = ex;
        }

        IServiceCollection serviceCollection = new ServiceCollection();

        serviceCollection
            .AddOptions()
            .AddTransient<IFritzBoxService, FritzBoxService>()
            .AddSingleton<IAesKeyProvider, AesKeyProvider>()
            .AddSingleton<FritzBoxDataCollector>()
            .AddSingleton<ModbusServerService>()
            .AddSingleton<IDataControlService, DataControlService>()
            .AddTransient<ISunSpecClient, SunSpecClient>()
            .AddLogging(builder => builder.AddSerilog())
            ;

        if (settings != null)
        {
            serviceCollection
                .Configure<FritzBoxParameters>(f =>
                {
                    f.Connections = settings.FritzBoxConnections;
                    f.RefreshRate = TimeSpan.FromSeconds(60);
                })
                .Configure<ModbusServerServiceParameters>(m =>
                {
                    m.EndPoint = new IPEndPoint(IPAddress.Parse(settings.ServerIpAddress), settings.ServerPort);
                    m.Mappings = settings.ModbusMappings;
                })
                ;
        }

        var serviceProvider = serviceCollection.BuildServiceProvider();
        IoC.Update(serviceProvider);
        logger = IoC.Get<ILogger<Program>>();

        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            var logLevel = e.IsTerminating ? LogLevel.Critical : LogLevel.Error;
            var senderName = s.GetType().Name;

            if (e.ExceptionObject is not Exception ex)
            {
                logger.Log(logLevel, "{SenderName}, {Object}", senderName, e.ExceptionObject.ToString());
            }
            else
            {
                logger.Log(logLevel, ex, senderName);
            }
        };

        server = IoC.Get<ModbusServerService>();

        switch (settingsLoadException)
        {
            case FileNotFoundException:
                logger.LogWarning("{FileName} does not exist. Created a default file.", Settings.SettingsFileName);
                break;

            case not null:
                logger.LogCritical("{FileName} could not be loaded. Must exit.", Settings.SettingsFileName);
                Environment.ExitCode = settingsLoadException.HResult;
                return settingsLoadException.HResult;
        }

        if (settings == null)
        {
            return 1;
        }

        if (args is [var arg0, ..])
        {
            var match = regex.Match(arg0);

            if (match.Success)
            {
                var userName = match.Groups["UserName"].Value;
                var password = match.Groups["Password"].Value;
                settings.FritzBoxConnections.Where(c => c.UserName == userName).Apply(c => { c.Password = password; });
                await settings.SaveAsync().ConfigureAwait(false);
                return 0;
            }

            return 2;
        }

        await server.StartAsync().ConfigureAwait(true);
        var fritzBoxDataCollector = IoC.Get<FritzBoxDataCollector>();
        await fritzBoxDataCollector.StartAsync().ConfigureAwait(true);

        while (true)
        {
            await Task.Delay(TimeSpan.FromMinutes(5)).ConfigureAwait(true);
        }
    }
}
