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