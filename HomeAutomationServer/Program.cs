using System.IO.Compression;
using System.Text.Json.Serialization;
using De.Hochstaetter.Fronius.Crypto;
using De.Hochstaetter.HomeAutomationServer.Hubs;
using De.Hochstaetter.HomeAutomationServer.Models.Authorization;
using De.Hochstaetter.HomeAutomationServer.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.ResponseCompression;
using Serilog.Sinks.SystemConsole.Themes;
using AuthenticationService = De.Hochstaetter.HomeAutomationServer.Services.AuthenticationService;
using Settings = De.Hochstaetter.HomeAutomationServer.Models.Settings.Settings;

namespace De.Hochstaetter.HomeAutomationServer;

internal class Program
{
    private static ModbusServerService? server;

    private static ILogger? logger;

    private static async Task<int> Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Logging.ClearProviders();

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel
#if DEBUG
            .Debug()
#else
            .Information()
#endif
            .Enrich.WithComputed("SourceContextName", "Substring(SourceContext, LastIndexOf(SourceContext, '.') + 1)")
            .WriteTo.Console
            (
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] ({SourceContextName:l}) {Message:lj}{NewLine}{Exception}",
                formatProvider: CultureInfo.InvariantCulture,
                applyThemeToRedirectedOutput: true,
                theme: AnsiConsoleTheme.Sixteen
            )
            .CreateLogger();

        Settings? settings = null;
        Exception? settingsLoadException = null;

        builder.Services.AddSingleton<IAesKeyProvider, AesKeyProvider>();

        var provider = builder.Services.BuildServiceProvider();
        IoC.Update(provider);

        try
        {
            settings = await Settings.LoadAsync().ConfigureAwait(false);
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
            await settings.SaveAsync().ConfigureAwait(false);
            settingsLoadException = ex;
        }
        catch (Exception ex)
        {
            settingsLoadException = ex;
        }

        builder.Services
            .AddOptions()
            .AddTransient<IFritzBoxService, FritzBoxService>()
            .AddTransient<IGen24Service, Gen24Service>()
            .AddSingleton<IGen24JsonService, Gen24JsonService>()
            .AddSingleton<ModbusServerService>()
            .AddSingleton<SettingsChangeTracker>()
            .AddSingleton<IDataControlService, DataControlService>()
            .AddSingleton<SunSpecDataCollector>()
            .AddSingleton<FritzBoxDataCollector>()
            .AddSingleton<Gen24DataCollector>()
            .AddSingleton<SignalRDispatcher>()
            .AddTransient<ISunSpecClient, SunSpecClient>()
            .AddLogging(b => b.AddSerilog())
            .AddCors(o => o.AddDefaultPolicy(p => p.SetIsOriginAllowed(_ => true)
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials()))
            .AddResponseCompression(o =>
            {
                o.EnableForHttps = true;
                o.Providers.Clear();
                o.Providers.Add(new BrotliCompressionProvider(new BrotliCompressionProviderOptions { Level = CompressionLevel.SmallestSize }));
                o.Providers.Add(new GzipCompressionProvider(new GzipCompressionProviderOptions { Level = CompressionLevel.SmallestSize }));
            })
            ;

        var supportedCultures = new List<CultureInfo>
        {
            CultureInfo.InvariantCulture, new("de"), new("de-CH"), new("de-LI"),
        };

        if (settings != null)
        {
            if (settings.WebServerSettings.Urls is { Length: > 0 })
            {
                builder.WebHost.UseUrls(settings.WebServerSettings.Urls);
            }

            builder.Services.AddSingleton(settings);

            builder.Services.AddLocalization();

            builder.Services.Configure<RequestLocalizationOptions>(options =>
            {
                options.DefaultRequestCulture = new RequestCulture(CultureInfo.InvariantCulture);
                options.SupportedCultures = supportedCultures;
                options.SupportedUICultures = supportedCultures;
            });

            builder.Services
                .Configure<FritzBoxDataCollectorParameters>(f =>
                {
                    f.Connections = settings.FritzBoxConnections;
                    f.RefreshRate = TimeSpan.FromSeconds(3);
                })
                .Configure<ModbusServerServiceParameters>(m =>
                {
                    m.EndPoint = new IPEndPoint(IPAddress.Parse(settings.ServerIpAddress), settings.ServerPort);
                    m.Mappings = settings.ModbusMappings;
                    m.AutoMap = true;
                })
                .Configure<SunSpecClientParameters>(s =>
                {
                    s.ModbusConnections = settings.SunSpecClients;
                    s.RefreshRate = TimeSpan.FromSeconds(1);
                })
                .Configure<Gen24DataCollectorParameters>(g =>
                {
                    g.Connections = settings.Gen24Connections;
                    g.RefreshRate = TimeSpan.FromSeconds(5);
                    g.ConfigRefreshRate = TimeSpan.FromMinutes(10.1);
                })
                .Configure<UserList>(u => { u.Users = settings.Users; });
        }

        builder.Services.AddControllers()
            .AddJsonOptions(o =>
            {
                o.JsonSerializerOptions.NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals;
                o.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                o.JsonSerializerOptions.IgnoreReadOnlyProperties = true;
                o.JsonSerializerOptions.IgnoreReadOnlyFields = true;
            });

        builder.Services.AddOpenApi();
        builder.Services.AddAuthentication().AddScheme<AuthenticationSchemeOptions, AuthenticationService>("Basic", null);
        builder.Services.AddSignalR()
            .AddJsonProtocol(o =>
            {
                o.PayloadSerializerOptions.NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals;
                o.PayloadSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                o.PayloadSerializerOptions.IgnoreReadOnlyProperties = true;
                o.PayloadSerializerOptions.IgnoreReadOnlyFields = true;
            });

        //builder.Services.AddAuthentication()
        //    .AddScheme<UserList, MyAuthenticationHandler>("MyAuthenticationSchemeName", options => {});

        var app = builder.Build();
        app.UseResponseCompression();
        app.MapOpenApi().RequireAuthorization(r => r.RequireRole("Developer"));

        app.UseRequestLocalization(options =>
        {
            options.DefaultRequestCulture = new RequestCulture(CultureInfo.InvariantCulture);
            options.SupportedCultures = supportedCultures;
            options.SupportedUICultures = supportedCultures;
        });

        app.MapControllers();
        app.UseCors();
        app.MapHub<HomeAutomationHub>("/hub");
        IoC.Update(app.Services);

        logger = IoC.Get<ILogger<Program>>();

        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            var logLevel = e.IsTerminating ? LogLevel.Critical : LogLevel.Error;
            var senderName = s.GetType().Name;

            if (e.ExceptionObject is not Exception ex)
            {
                logger.Log(logLevel, "Unhandled exception in {SenderName}: {Object}", senderName, e.ExceptionObject.ToString());
            }
            else
            {
                logger.Log(logLevel, ex, "Unhandled exception in {SenderName}", senderName);
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

        await server.StartAsync().ConfigureAwait(false);
        var fritzBoxDataCollector = IoC.Get<FritzBoxDataCollector>();
        await fritzBoxDataCollector.StartAsync().ConfigureAwait(false);
        await IoC.Get<SunSpecDataCollector>().StartAsync().ConfigureAwait(false);
        await IoC.Get<Gen24DataCollector>().StartAsync().ConfigureAwait(false);
        await IoC.Get<SignalRDispatcher>().StartAsync().ConfigureAwait(false);
        //await Task.Delay(TimeSpan.FromSeconds(30));
        //await IoC.Get<SunSpecDataCollector>().StopAsync().ConfigureAwait(false);
        //await IoC.Get<Gen24DataCollector>().StopAsync().ConfigureAwait(false);
        //await fritzBoxDataCollector.StopAsync().ConfigureAwait(false);
        // Configure the HTTP request pipeline.
        await settings.SaveAsync().ConfigureAwait(false);
        await app.RunAsync().ConfigureAwait(false);
        return 0;
    }
}
