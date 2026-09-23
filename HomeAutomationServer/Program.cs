using System.IO.Compression;
using System.Text.Json;
using De.Hochstaetter.Fronius.Crypto;
using De.Hochstaetter.HomeAutomationServer.Hubs;
using De.Hochstaetter.HomeAutomationServer.Models.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.ResponseCompression;
using Serilog.Sinks.SystemConsole.Themes;

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
            // Shows the shape of the Toshiba section; with an empty username nothing is collected.
            settings.ToshibaHvac = new ToshibaHvacSettings();
            // Shows the shape of the price chart section. Without a postal code and a bearer only the market
            // prices are collected, which need no account at all.
            settings.EnergyData = new EnergyDataSettings();
            // Shows the shape of the Solar.web section; with an empty user name or PvSystemId nothing is read.
            settings.SolarWeb = new SolarWebSettings();
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
            // Solar.web: one account, one cookie jar, so the client is a singleton; the charts are cached in a
            // second SQLite file and the controller reads the service under its contract - the same instance,
            // so the 429 back-off it keeps is the one every request sees.
            .AddSingleton<ISolarWebClient, SolarWebClient>()
            .AddSingleton<ISolarWebHistoryStore, SolarWebHistoryStore>()
            .AddSingleton<SolarWebService>()
            .AddSingleton<ISolarWebService>(services => services.GetRequiredService<SolarWebService>())
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
                    g.RefreshRate = settings.Gen24DataCollector.RefreshRate;
                    g.ConfigRefreshRate = settings.Gen24DataCollector.ConfigRefreshRate;
                    g.LogDirectory = settings.Gen24DataCollector.LogDirectory;
                })
                .Configure<WattPilotParameters>(w => { w.Connections = settings.WattPilotConnections; })
                .Configure<ToshibaHvacDataCollectorParameters>(t =>
                {
                    t.Connection = settings.ToshibaHvac;
                    t.AzureDeviceId = settings.ToshibaHvac?.AzureDeviceIdString ?? string.Empty;
                    t.MappingRefreshRate = TimeSpan.FromMinutes(Math.Max(1, settings.ToshibaHvac?.MappingRefreshMinutes ?? 30));
                })
                .Configure<EnergyDataCollectorParameters>(e => { e.Settings = settings.EnergyData; })
                .Configure<SolarWebParameters>(s =>
                {
                    s.Settings = settings.SolarWeb;
                    s.RefreshRate = TimeSpan.FromMinutes(Math.Max(1, settings.SolarWeb?.RefreshMinutes ?? 15));
                })
                .Configure<UserList>(u =>
                {
                    u.Users = settings.Users;
                    u.EnableGuestAccount = settings.EnableGuestAccount;
                    u.Authentication = settings.Authentication;
                });
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
        builder.Services.AddApiAuthentication();
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

        // The scheme is named because there is no default one to fall back on: two are registered, the API's and the hub's.
        app.MapOpenApi().RequireAuthorization(r => r.AddAuthenticationSchemes(ApiAuthenticationService.SchemeName).RequireRole("Developer"));

        app.UseRequestLocalization(options =>
        {
            options.DefaultRequestCulture = new RequestCulture(CultureInfo.InvariantCulture);
            options.SupportedCultures = supportedCultures;
            options.SupportedUICultures = supportedCultures;
        });

        // The browser client (HomeAutomationClient.Browser, published into wwwroot alongside its
        // .staticwebassets.endpoints.json manifest renamed to this assembly's - see the "Publish client" targets
        // in HomeAutomationServer.csproj and the Dockerfile). MapStaticAssets, not UseStaticFiles: the Avalonia
        // WebAssembly SDK ships every framework file (multi-megabyte assemblies, the Mono runtime, ICU data) both
        // raw and precompressed as .br/.gz, and only MapStaticAssets reads that manifest to hand back the
        // matching precompressed file for the request's Accept-Encoding - with the correct Content-Type and an
        // immutable Cache-Control, since the file names are content-hashed. UseStaticFiles knows none of that: it
        // would have served the ICU .dat files as 404s (unregistered extension) and left the multi-megabyte
        // assemblies for UseResponseCompression to recompress from scratch, at max compression, on every single
        // request - the runtime's own dotnet.native.wasm alone took 29 seconds to compress that way. Like the
        // other endpoints below, this needs no RequireAuthorization, so the client can be downloaded before
        // logging in - which matters, because logging in is only possible once the client has been downloaded.
        app.MapStaticAssets();

        app.MapControllers();
        // The hub has a scheme of its own: a browser cannot set an Authorization header on a WebSocket handshake,
        // so the connection authenticates with a short-lived ticket instead. See HubTicketService.
        app.MapHub<HomeAutomationHub>("/hub").RequireAuthorization(policy => policy.RequireHubTicket());

        // Lowest-priority endpoint: only requests that matched none of the above (i.e. the client's own routes)
        // land here, so a full page load or reload of e.g. /inverterdetails/Fronius/1234 still gets the client's
        // index.html instead of a 404. No effect where wwwroot has no client published into it.
        app.MapFallbackToFile("index.html");

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

        LogGuestAccount(settings, logger);
        LogAuthentication(settings.Authentication, logger);

        await server.StartAsync().ConfigureAwait(false);
        var fritzBoxDataCollector = IoC.Get<FritzBoxDataCollector>();
        await fritzBoxDataCollector.StartAsync().ConfigureAwait(false);
        await IoC.Get<SunSpecDataCollector>().StartAsync().ConfigureAwait(false);
        await IoC.Get<Gen24DataCollector>().StartAsync().ConfigureAwait(false);
        await IoC.Get<SignalRDispatcher>().StartAsync().ConfigureAwait(false);
        await IoC.Get<WattPilotDataCollector>().StartAsync().ConfigureAwait(false);
        await IoC.Get<ToshibaHvacDataCollector>().StartAsync().ConfigureAwait(false);
        await IoC.Get<EnergyDataCollector>().StartAsync().ConfigureAwait(false);
        await IoC.Get<SolarWebService>().StartAsync().ConfigureAwait(false);
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
    /// Says in the log what <see cref="Settings.EnableGuestAccount"/> decided, because an account anyone can log
    /// in to is not something to find out by accident - and warns where the user list has a user of that name,
    /// who cannot log in while the built-in guest reserves it and would otherwise be refused without a reason.
    /// </summary>
    internal static void LogGuestAccount(Settings settings, ILogger logger)
    {
        if (!settings.EnableGuestAccount)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("The built-in guest account is switched off in {FileName}", Settings.SettingsFileName);
            }

            return;
        }

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation
            (
                "The built-in guest account is on: anybody may log in as \"{UserName}\" and see the inverters. Set EnableGuestAccount to false in {FileName} to switch it off.",
                User.Guest.Username, Settings.SettingsFileName
            );
        }

        if (settings.Users.Any(user => User.IsGuest(user.Username)) && logger.IsEnabled(LogLevel.Warning))
        {
            logger.LogWarning
            (
                "{FileName} has a user named \"{UserName}\", which the built-in guest account hides: that user cannot log in while EnableGuestAccount is true. Rename the user, or switch the account off.",
                Settings.SettingsFileName, User.Guest.Username
            );
        }
    }

    /// <summary>
    /// Says in the log how long a bearer token lives, and warns about each of the debugging schemes that is
    /// switched on: they are meant to be switched off again, and a warning at every start is what reminds anybody.
    /// </summary>
    internal static void LogAuthentication(AuthenticationSettings settings, ILogger logger)
    {
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Bearer tokens are valid for {Minutes} minutes", settings.BearerTokenLifetime.TotalMinutes);
        }

        if (settings.EnableBasicAuthentication && logger.IsEnabled(LogLevel.Warning))
        {
            logger.LogWarning
            (
                "Basic authentication is on: a request may carry the user name and password with it. It is meant for debugging; set EnableBasicAuthentication to false in {FileName} to switch it off.",
                Settings.SettingsFileName
            );
        }

        if (settings.EnableCookieAuthentication && logger.IsEnabled(LogLevel.Warning))
        {
            logger.LogWarning
            (
                "Cookie authentication is on: logging in also sets a cookie holding the bearer token. It is meant for debugging; set EnableCookieAuthentication to false in {FileName} to switch it off.",
                Settings.SettingsFileName
            );
        }
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
