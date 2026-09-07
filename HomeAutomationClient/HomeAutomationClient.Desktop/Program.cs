using System;
using System.Globalization;
using System.IO;
using Avalonia;
using De.Hochstaetter.HomeAutomationClient.Desktop.Platform;
using De.Hochstaetter.HomeAutomationClient.Misc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace De.Hochstaetter.HomeAutomationClient.Desktop;

internal sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        var cache = new Cache();
        App.ServiceCollection = new ServiceCollection();
        App.ServiceCollection.AddSingleton<ICache>(cache);

        // PlatformStartup.AccentColor deliberately stays null on all three desktop systems: none of them hands a
        // plain .NET app a color this head could read. On Windows it does not have to - Avalonia reads the accent
        // color of the OS itself and follows it while the app runs, which is what AccentColorFollowsOs says.
        // macOS keeps its accent color in NSColor.controlAccentColor, out of reach here, and Linux has none that
        // all desktop environments agree on, so both get the AppAccentColor of the app.
        PlatformStartup.AccentColorFollowsOs = OperatingSystem.IsWindows();
        PlatformStartup.ConfigureLogging = ConfigureSerilog();

        try
        {
            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }
        finally
        {
            // Otherwise whatever the file sink still holds when the app closes is lost, which is exactly the part
            // that matters after a crash.
            Log.CloseAndFlush();
        }
    }

    /// <summary>
    /// Log records of this head go to a rolling file, set up the way FroniusMonitor sets up its own. The heads
    /// without a file system worth writing to leave <see cref="PlatformStartup.ConfigureLogging"/> alone and get
    /// AddDebug instead.
    /// </summary>
    /// <remarks>
    /// Only Serilog is touched here, no Avalonia and nothing that needs a SynchronizationContext, so this is safe
    /// to run before AppMain.
    /// </remarks>
    private static Action<ILoggingBuilder> ConfigureSerilog()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel
#if DEBUG
            .Debug()
#else
            .Information()
#endif
            .Enrich.WithComputed("SourceContextName", "Substring(SourceContext, LastIndexOf(SourceContext, '.') + 1)")
            .WriteTo.File
            (
                // Next to the cache of this head, so everything per user sits in one directory.
                Path.Combine(Cache.DataDirectory, "Log.txt"),
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] ({SourceContextName:l}) {Message:lj}{NewLine}{Exception}",
                formatProvider: CultureInfo.InvariantCulture,
                rollingInterval: RollingInterval.Day,
                retainedFileTimeLimit: TimeSpan.FromDays(7)
            )
            .CreateLogger();

        return builder => builder.AddSerilog();
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}