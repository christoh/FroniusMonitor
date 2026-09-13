using System.IO.Compression;
using System.Text.Json;
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

namespace De.Hochstaetter.HomeAutomationServer;

internal class Program
{
    /// <summary>
    /// The administrator a server with no users at all is given, so that somebody can log in and create the real
    /// ones. Deliberately not localized: it is typed into a login box, and it has to read the same whatever
    /// language the server happens to run in.
    /// </summary>
    internal const string DefaultAdministratorName = "admin";

    /// <inheritdoc cref="DefaultAdministratorName"/>
    internal const string DefaultAdministratorPassword = "password";

    /// <summary>The exit code where the settings leave nobody able to administer this server.</summary>
    internal const int NoAdministratorExitCode = 2;

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
            // Shows the shape of the Toshiba section; with an empty user name nothing is collected.
            settings.ToshibaHvac = new ToshibaHvacSettings();
            // Shows the shape of the price chart section. Without a postal code and a bearer only the market
            // prices are collected, which need no account at all.
            settings.EnergyData = new EnergyDataSettings();
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
            //.AddSingleton<HomeAutomationHub>()
            .AddTransient<IGen24Service, Gen24Service>()
            .AddSingleton<IGen24JsonService, Gen24JsonService>()
            .AddTransient<IWattPilotService, WattPilotService>()
            .AddSingleton<ModbusServerService>()
            .AddSingleton<SettingsChangeTracker>()
            .AddSingleton<IDataControlService, DataControlService>()
            .AddSingleton<SunSpecDataCollector>()
            .AddSingleton<FritzBoxDataCollector>()
            .AddSingleton<Gen24DataCollector>()
            // The same instance under its contract, so a controller that writes a setting can have it read back
            // at once instead of waiting out the polling interval.
            .AddSingleton<IGen24ConfigRefresher>(services => services.GetRequiredService<Gen24DataCollector>())
            .AddSingleton<SignalRDispatcher>()
            .AddSingleton<WattPilotDataCollector>()
            // The same instance under its contract, so the hub can hand a client's settings to the service that
            // holds the connection to that charger.
            .AddSingleton<IWattPilotServices>(services => services.GetRequiredService<WattPilotDataCollector>())
            // One Toshiba account per server, so the service is the singleton the collector runs and the hub sends
            // commands through; the store puts the bearer token into Settings.xml.
            .AddSingleton<IToshibaHvacSessionStore, ToshibaHvacSessionStore>()
            .AddSingleton<IToshibaHvacService, ToshibaHvacService>()
            .AddSingleton<ToshibaHvacDataCollector>()
            // The price chart: Awattar and the DWD are read by the collector, the history is one SQLite file, and
            // the controller reads the collector under its contract - the same instance, so the current data the
            // hub pushed is the data the controller answers.
            .AddSingleton<IAwattarClient, AwattarClient>()
            .AddSingleton<IDwdWeatherClient, DwdWeatherClient>()
            .AddSingleton<IEnergyHistoryStore, EnergyHistoryStore>()
            .AddSingleton<EnergyDataCollector>()
            .AddSingleton<IEnergyDataService>(services => services.GetRequiredService<EnergyDataCollector>())
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

        // The satellite assemblies of Fronius are the list of languages; nothing is enumerated by hand here.
        List<CultureInfo> supportedCultures = [CultureInfo.InvariantCulture, .. SupportedCultures.Satellites];

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
                    f.RefreshRate = TimeSpan.FromSeconds(2);
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
                    g.RefreshRate = TimeSpan.FromSeconds(2);
                    g.ConfigRefreshRate = TimeSpan.FromMinutes(5.1);
                })
                .Configure<WattPilotParameters>(w => { w.Connections = settings.WattPilotConnections; })
                .Configure<ToshibaHvacDataCollectorParameters>(t =>
                {
                    t.Connection = settings.ToshibaHvac;
                    t.AzureDeviceId = settings.ToshibaHvac?.AzureDeviceIdString ?? string.Empty;
                    t.MappingRefreshRate = TimeSpan.FromMinutes(Math.Max(1, settings.ToshibaHvac?.MappingRefreshMinutes ?? 30));
                })
                .Configure<EnergyDataCollectorParameters>(e => { e.Settings = settings.EnergyData; })
                .Configure<UserList>(u => { u.Users = settings.Users; });
        }

        builder.Services.AddControllers()
            .AddJsonOptions(o =>
            {
                o.JsonSerializerOptions.NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals;
                o.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                o.JsonSerializerOptions.IgnoreReadOnlyProperties = true;
                o.JsonSerializerOptions.IgnoreReadOnlyFields = true;
                o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
            });

        builder.Services.AddOpenApi();
        builder.Services.AddAuthentication().AddScheme<AuthenticationSchemeOptions, AuthenticationService>("Basic", null);
        builder.Services.AddHubTicketAuthentication();
        builder.Services.AddHomeAutomationSignalR();

        //builder.Services.AddAuthentication()
        //    .AddScheme<UserList, MyAuthenticationHandler>("MyAuthenticationSchemeName", options => {});

        var app = builder.Build();
        app.UseResponseCompression();

        // CORS has to run before authorization, and naming the two here is the only way to get that order:
        // WebApplication adds UseAuthentication and UseAuthorization by itself once the services are there, and it
        // adds them ahead of every middleware this method registers. A preflight carries no credentials, so the
        // authorization middleware answered the OPTIONS of everything RequireAuthorization covers - the hub,
        // OpenApi - with a bare 401 before UseCors was ever reached, and a 401 without CORS headers reaches a
        // browser as nothing more than "TypeError: Failed to fetch". That is why a client on a foreign origin
        // could talk to the controllers, whose endpoints carry no authorization metadata, and only failed on the
        // hub. Naming them suppresses the automatic ones.
        app.UseCors();
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapOpenApi().RequireAuthorization(r => r.RequireRole("Developer"));

        app.UseRequestLocalization(options =>
        {
            options.DefaultRequestCulture = new RequestCulture(CultureInfo.InvariantCulture);
            options.SupportedCultures = supportedCultures;
            options.SupportedUICultures = supportedCultures;
        });

        app.MapControllers();
        // The hub has a scheme of its own: a browser cannot set an Authorization header on a WebSocket handshake,
        // so the connection authenticates with a short lived ticket instead. See HubTicketService.
        app.MapHub<HomeAutomationHub>("/hub").RequireAuthorization(policy => policy.RequireHubTicket());

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

        if (EnsureAdministratorExists(settings, logger) is { } noAdministratorExitCode)
        {
            return noAdministratorExitCode;
        }

        await server.StartAsync().ConfigureAwait(false);
        var fritzBoxDataCollector = IoC.Get<FritzBoxDataCollector>();
        await fritzBoxDataCollector.StartAsync().ConfigureAwait(false);
        await IoC.Get<SunSpecDataCollector>().StartAsync().ConfigureAwait(false);
        await IoC.Get<Gen24DataCollector>().StartAsync().ConfigureAwait(false);
        await IoC.Get<SignalRDispatcher>().StartAsync().ConfigureAwait(false);
        await IoC.Get<WattPilotDataCollector>().StartAsync().ConfigureAwait(false);
        await IoC.Get<ToshibaHvacDataCollector>().StartAsync().ConfigureAwait(false);
        await IoC.Get<EnergyDataCollector>().StartAsync().ConfigureAwait(false);
        //await Task.Delay(TimeSpan.FromSeconds(30));
        //await IoC.Get<SunSpecDataCollector>().StopAsync().ConfigureAwait(false);
        //await IoC.Get<Gen24DataCollector>().StopAsync().ConfigureAwait(false);
        //await fritzBoxDataCollector.StopAsync().ConfigureAwait(false);
        // Configure the HTTP request pipeline.
        await settings.SaveAsync().ConfigureAwait(false);
        await app.RunAsync().ConfigureAwait(false);
        return 0;
    }

    /// <summary>
    /// Makes sure somebody can log in and administer this server.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Every way of creating or repairing a user goes through <c>IdentityController</c>, and each of those
    /// endpoints asks for <see cref="Roles.Administrator"/>. A list of users with nobody in that role can
    /// therefore never be put right from a client, so saying so and stopping is more use than serving something
    /// nobody can manage - the fix is a text editor and <see cref="Settings.SettingsFileName"/>.
    /// </para>
    /// <para>
    /// No users at all is a different thing: that is a fresh installation rather than a mistake, and it gets the
    /// default administrator that makes the first login possible.
    /// </para>
    /// </remarks>
    /// <returns>
    /// The exit code where the server must not start, or <see langword="null"/> where it may. A created
    /// administrator is added to <paramref name="settings"/>, whose set of users is the very one the
    /// <c>UserList</c> options hand to the authentication scheme, and <see cref="Main"/> writes it back before
    /// the server runs.
    /// </returns>
    internal static int? EnsureAdministratorExists(Settings settings, ILogger logger)
    {
        if (settings.Users.Count == 0)
        {
            var administrator = new User { Username = DefaultAdministratorName, Roles = Roles.Administrator };
            administrator.SetPassword(DefaultAdministratorPassword);
            settings.Users.Add(administrator);

            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning
                (
                    "No users are defined in {FileName}, so a default administrator has been created: user name \"{UserName}\", password \"{Password}\". Change that password.",
                    Settings.SettingsFileName, DefaultAdministratorName, DefaultAdministratorPassword
                );
            }

            return null;
        }

        if (settings.Users.Any(user => user.Roles.HasFlag(Roles.Administrator)))
        {
            return null;
        }

        if (logger.IsEnabled(LogLevel.Error))
        {
            logger.LogError
            (
                "None of the {UserCount} users in {FileName} has the {Role} role, so nobody could ever manage this server. Give one of them that role, or remove them all to have a default administrator created on the next start. Must exit.",
                settings.Users.Count, Settings.SettingsFileName, nameof(Roles.Administrator)
            );
        }

        return NoAdministratorExitCode;
    }
}
