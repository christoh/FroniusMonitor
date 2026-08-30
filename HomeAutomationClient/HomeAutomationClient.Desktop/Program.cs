using System;
using Avalonia;
using De.Hochstaetter.HomeAutomationClient.Desktop.Platform;
using De.Hochstaetter.HomeAutomationClient.Misc;
using Microsoft.Extensions.DependencyInjection;

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
        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}