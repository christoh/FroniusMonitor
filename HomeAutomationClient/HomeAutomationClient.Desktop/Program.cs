using System;
using Avalonia;
using De.Hochstaetter.HomeAutomationClient.Desktop.Platform;
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

        // PlatformStartup.AccentColor deliberately stays null on all three desktop systems, so that the
        // SystemAccentColor of the Fluent theme applies. On Windows that already is the accent color of the OS,
        // because Avalonia reads it itself. macOS keeps its accent color in NSColor.controlAccentColor, which a
        // plain .NET desktop app cannot reach, and Linux has none that all desktop environments agree on.
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