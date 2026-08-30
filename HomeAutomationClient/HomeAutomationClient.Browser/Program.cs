using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Browser;
using De.Hochstaetter.Fronius.Localization;
using De.Hochstaetter.HomeAutomationClient.Contracts;
using De.Hochstaetter.HomeAutomationClient.Browser.Platform;
using De.Hochstaetter.HomeAutomationClient.Misc;
using Microsoft.Extensions.DependencyInjection;

namespace De.Hochstaetter.HomeAutomationClient.Browser;

internal sealed partial class Program
{
    [JSImport("INTERNAL.loadSatelliteAssemblies")]
    public static partial Task LoadSatelliteAssemblies(string[] culturesToLoad);

    [JSImport("getUserLanguages", "culture")]
    [return: JSMarshalAs<JSType.Array<JSType.String>>]
    private static partial string[] GetUserLanguages();

    [JSImport("getSupportedCultures", "culture")]
    [return: JSMarshalAs<JSType.Array<JSType.String>>]
    private static partial string[] GetSupportedCultures();

    private static async Task Main(string[] args)
    {
        var cache = new Cache();
        #if DEBUG
        await cache.AddOrUpdateAsync(CacheKeys.ApiUri, "https://home.hochstaetter.de/api/");
        await cache.AddOrUpdateAsync(CacheKeys.HubUri, "https://home.hochstaetter.de/hub");
        #else
        // args[0] is the base address of the app, and document.baseURI always ends with a slash. Anything else
        // would make Uri resolve api/ and hub against the parent of the last segment.
        var appRoot = new Uri(args[0].EndsWith('/') ? args[0] : args[0] + "/");
        await cache.AddOrUpdateAsync(CacheKeys.ApiUri, new Uri(appRoot, "api/").ToString());
        await cache.AddOrUpdateAsync(CacheKeys.HubUri, new Uri(appRoot, "hub").ToString());
        #endif
        App.ServiceCollection = new ServiceCollection();
        App.ServiceCollection.AddSingleton<ICache>(cache);
        App.ServiceCollection.AddSingleton<IUriService>(await UriService.CreateAsync());

        // PlatformStartup.AccentColor stays null here, so the app keeps the accent color of the Fluent theme,
        // which is what Avalonia gives the browser anyway. A browser never hands the accent color of the OS to a
        // page: Chromium answers the CSS system color AccentColor with its built-in #0075FF, and paints even its
        // own form controls with it, whatever the browser is themed with. Firefox and Safari do answer with the
        // real one, but an accent color that only some browsers follow is not worth the moving parts.

        await LoadSatelliteAssembliesForBrowserLanguageAsync();
        await BuildAvaloniaApp().WithInterFont().StartBrowserAppAsync("out");
    }

    /// <summary>
    /// Downloads the satellite assemblies for the language of the browser, and only those. Loading all of them
    /// costs every user the translations of every other user.
    /// </summary>
    private static async Task LoadSatelliteAssembliesForBrowserLanguageAsync()
    {
        string[] cultures;

        try
        {
            // The module URL is resolved relative to the .NET runtime in _framework, not to the document.
            await JSHost.ImportAsync("culture", "../culture.js");
            cultures = GetSatelliteCultures(GetUserLanguages(), GetSupportedCultures());
        }
        catch (Exception exception)
        {
            // Without the languages of the browser the user gets the neutral culture, which is still readable.
            Console.WriteLine("Could not read the languages of the browser: " + exception.Message);
            return;
        }

        if (cultures.Length == 0)
        {
            return;
        }

        await LoadSatelliteAssemblies(cultures);

        // The runtime takes the UI culture from the first language of the browser. Where we serve a later one,
        // because we have no translation for the earlier ones, the download would stay unused without this.
        var uiCulture = new CultureInfo(cultures[0]);
        CultureInfo.DefaultThreadCurrentUICulture = uiCulture;
        CultureInfo.CurrentUICulture = uiCulture;
    }

    /// <summary>
    /// Picks the satellite assemblies for the first language of the browser that we have a translation for.
    /// A specific culture wins over its neutral one ("de-CH" over "de"), and a language we do not have in its
    /// specific form falls back to the neutral one ("it-IT" gets "it"). Where we have nothing to offer at all,
    /// the result is empty and the user sees the neutral culture rather than a language nobody asked for.
    /// </summary>
    internal static string[] GetSatelliteCultures(IEnumerable<string> browserLanguages, IReadOnlyCollection<string> supportedCultures)
    {
        foreach (var language in browserLanguages)
        {
            if (IsNeutral(language))
            {
                return [];
            }

            if (Supported(language, supportedCultures) is { } culture)
            {
                // A specific culture needs its neutral one as well: .NET falls back from de-CH to de and only
                // then to the neutral culture, so a string that de-CH does not translate would turn English.
                return NeutralOf(culture) is { } parent && Supported(parent, supportedCultures) is { } neutral ? [culture, neutral] : [culture];
            }

            if (NeutralOf(language) is { } languageParent && Supported(languageParent, supportedCultures) is { } neutralCulture)
            {
                return [neutralCulture];
            }
        }

        return [];
    }

    private static bool IsNeutral(string language) => string.Equals(language, SupportedCultures.NeutralLanguage, StringComparison.OrdinalIgnoreCase) || string.Equals(NeutralOf(language), SupportedCultures.NeutralLanguage, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// The culture as the build spells it, or null where it ships no translation for it. The spelling matters:
    /// the loader of the runtime matches its argument against the cultures of the build with ===.
    /// </summary>
    private static string? Supported(string culture, IReadOnlyCollection<string> supportedCultures) => supportedCultures.FirstOrDefault(supported => string.Equals(supported, culture, StringComparison.OrdinalIgnoreCase));

    private static string? NeutralOf(string culture) => culture.IndexOf('-') is var dash and > 0 ? culture[..dash] : null;

    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>();
    }
}
