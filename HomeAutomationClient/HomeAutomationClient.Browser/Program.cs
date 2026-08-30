using System;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Browser;
using De.Hochstaetter.Fronius.Models;
using De.Hochstaetter.HomeAutomationClient.Browser.Platform;
using De.Hochstaetter.HomeAutomationClient.Misc;
using Microsoft.Extensions.DependencyInjection;

namespace De.Hochstaetter.HomeAutomationClient.Browser;

internal sealed partial class Program
{
    [JSImport("INTERNAL.loadSatelliteAssemblies")]
    public static partial Task LoadSatelliteAssemblies(string[] culturesToLoad);

    [JSImport("getAccentColor", "accent")]
    private static partial string? GetAccentColor();

    private static async Task Main(string[] args)
    {
        var cache = new Cache();
        #if DEBUG
        await cache.AddOrUpdateAsync(CacheKeys.ApiUri, "https://home.hochstaetter.de/api/");
        await cache.AddOrUpdateAsync(CacheKeys.HubUri, "https://home.hochstaetter.de/hub");
        #else
        await cache.AddOrUpdateAsync(CacheKeys.ApiUri, args[0] + (args[0].EndsWith('/') ? string.Empty : "/") + "api/");
        await cache.AddOrUpdateAsync(CacheKeys.HubUri, args[0] + (args[0].EndsWith('/') ? string.Empty : "/") + "hub");
        #endif
        App.ServiceCollection = new ServiceCollection();
        App.ServiceCollection.AddSingleton<ICache>(cache);
        PlatformStartup.AccentColor = await GetOsAccentColorAsync();

        await LoadSatelliteAssemblies(["de", "de-CH", "de-LI", "it", "gsw", "fr", "rm"]);
        await BuildAvaloniaApp().WithInterFont().StartBrowserAppAsync("out");
    }

    /// <summary>
    /// Asks the browser for the accent color of the operating system. Only some browsers know it, so a
    /// <see langword="null"/> result is the normal case rather than an error; see accent.js for the details.
    /// </summary>
    private static async Task<HaColor?> GetOsAccentColorAsync()
    {
        try
        {
            // The module URL is resolved relative to the .NET runtime in _framework, not to the document.
            await JSHost.ImportAsync("accent", "../accent.js");
            return GetAccentColor() is { } accentColor && HaColor.TryParse(accentColor, null, out var color) ? color : null;
        }
        catch (Exception exception)
        {
            Console.WriteLine("Could not detect the accent color of the OS: " + exception.Message);
            return null;
        }
    }

    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>();
    }
}
