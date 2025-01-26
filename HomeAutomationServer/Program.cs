﻿using System.Security.Cryptography;
using System.Text.Json.Serialization;
using De.Hochstaetter.Fronius.Crypto;
using De.Hochstaetter.HomeAutomationServer.Models.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.OpenApi.Models;
using Settings = De.Hochstaetter.HomeAutomationServer.Models.Settings.Settings;

namespace De.Hochstaetter.HomeAutomationServer;

internal partial class Program
{
    private static ModbusServerService? server;

    [GeneratedRegex("^(?<UserName>(?!:).+):(?<Password>(?!:).+)$", RegexOptions.Compiled)]
    private static partial Regex PasswordRegex();

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
            .WriteTo.Console(outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] ({SourceContextName:l}) {Message:lj}{NewLine}{Exception}", formatProvider: CultureInfo.InvariantCulture)
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
            .AddTransient<ISunSpecClient, SunSpecClient>()
            .AddLogging(b => b.AddSerilog())
            ;

        if (settings != null)
        {
            builder.Services
                .Configure<FritzBoxDataCollectorParameters>(f =>
                {
                    f.Connections = settings.FritzBoxConnections;
                    f.RefreshRate = TimeSpan.FromSeconds(15);
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
                    g.RefreshRate = TimeSpan.FromSeconds(30);
                    g.ConfigRefreshRate = TimeSpan.FromMinutes(10.1);
                })
                .Configure<UserList>(u =>
                {
                    u.Users = settings.Users;
                });
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
        
        //builder.Services.AddAuthentication()
        //    .AddScheme<UserList, MyAuthenticationHandler>("MyAuthenticationSchemeName", options => {});

        
        var app = builder.Build();
        //if (app.Environment.IsDevelopment())
        //{
        app.MapOpenApi();
        //}

        app.UseHttpsRedirection();
        //app.UseAuthentication();
        //app.UseAuthorization();

        //app.UseCors();
        //app.UseAuthentication();
        //app.UseAuthorization();

        app.MapControllers();
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

        var tracker = IoC.Get<SettingsChangeTracker>();
        tracker.SettingsChanged += (_, _) => _ = settings.SaveAsync();

        foreach (var arg in args)
        {
            var match = PasswordRegex().Match(arg);

            if (!match.Success)
            {
                continue;
            }

            var userName = match.Groups["UserName"].Value;
            var password = match.Groups["Password"].Value;

            settings.FritzBoxConnections.Where(c => c.UserName == userName).Apply(c =>
            {
                c.Password = password;
                c.PasswordChecksum = c.CalculatedChecksum;
            });

            settings.Gen24Connections.Where(c => c.UserName == userName).Apply(c =>
            {
                c.Password = password;
                c.PasswordChecksum = c.CalculatedChecksum;
            });
        }

        if (args.Length > 0)
        {
            await settings.SaveAsync().ConfigureAwait(false);
            return 0;
        }

        await server.StartAsync().ConfigureAwait(false);
        var fritzBoxDataCollector = IoC.Get<FritzBoxDataCollector>();
        await fritzBoxDataCollector.StartAsync().ConfigureAwait(false);
        //await IoC.Get<SunSpecDataCollector>().StartAsync().ConfigureAwait(false);
        await IoC.Get<Gen24DataCollector>().StartAsync().ConfigureAwait(false);
        //await Task.Delay(TimeSpan.FromSeconds(30));
        //await IoC.Get<SunSpecDataCollector>().StopAsync().ConfigureAwait(false);
        //await IoC.Get<Gen24DataCollector>().StopAsync().ConfigureAwait(false);
        //await fritzBoxDataCollector.StopAsync().ConfigureAwait(false);
        // Configure the HTTP request pipeline.
        await app.RunAsync().ConfigureAwait(false);
        return 0;
    }
}
