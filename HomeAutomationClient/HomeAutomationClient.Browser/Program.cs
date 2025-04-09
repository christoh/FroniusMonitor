using System.Threading.Tasks;
using Avalonia;
using Avalonia.Browser;
using De.Hochstaetter.HomeAutomationClient.Browser.Platform;
using Microsoft.Extensions.DependencyInjection;

namespace De.Hochstaetter.HomeAutomationClient.Browser;

internal sealed partial class Program
{
    private static Task Main(string[] args)
    {
        var cache = new Cache();
        #if DEBUG
        cache.AddOrUpdate("apiUri","https://home.hochstaetter.de/api/");
        #else
        cache.AddOrUpdate("apiUri", args[0] + (args[0].EndsWith('/') ? string.Empty : "/") + "api/");
        #endif
        App.ServiceCollection = new ServiceCollection();
        App.ServiceCollection.AddSingleton<ICache>(cache);

        return BuildAvaloniaApp()
            .WithInterFont()
            .StartBrowserAppAsync("out");
    }

    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>();
    }
}