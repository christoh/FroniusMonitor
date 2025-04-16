using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Browser;
using De.Hochstaetter.HomeAutomationClient.Browser.Platform;
using Microsoft.Extensions.DependencyInjection;

namespace De.Hochstaetter.HomeAutomationClient.Browser;

internal sealed partial class Program
{
    [JSImport("INTERNAL.loadSatelliteAssemblies")]
    public static partial Task LoadSatelliteAssemblies(string[] culturesToLoad);
    
    private static async Task Main(string[] args)
    {
        var cache = new Cache();
#if DEBUG
        await cache.AddOrUpdateAsync("apiUri", "https://home.hochstaetter.de/api/");
#else
        await cache.AddOrUpdateAsync("apiUri", args[0] + (args[0].EndsWith('/') ? string.Empty : "/") + "api/");
#endif
        App.ServiceCollection = new ServiceCollection();
        App.ServiceCollection.AddSingleton<ICache>(cache);

        await LoadSatelliteAssemblies(["de", "de-CH", "de-LI"]);
        await BuildAvaloniaApp().WithInterFont().StartBrowserAppAsync("out");
    }

    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>();
    }
}