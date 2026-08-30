using Avalonia;
using Avalonia.Controls;
using Avalonia.iOS;
using Avalonia.Media;
using De.Hochstaetter.HomeAutomationClient;
using Foundation;
using HomeAutomationClient.iOS.Platform;
using Microsoft.Extensions.DependencyInjection;
using UIKit;

namespace HomeAutomationClient.iOS;

// The UIApplicationDelegate for the application. This class is responsible for launching the 
// User Interface of the application, as well as listening (and optionally responding) to 
// application events from iOS.
[Register("AppDelegate")]
#pragma warning disable CA1711 // Identifiers should not have incorrect suffix
public partial class AppDelegate : AvaloniaAppDelegate<App>
#pragma warning restore CA1711 // Identifiers should not have incorrect suffix
{
    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        App.ServiceCollection = new ServiceCollection();
        App.ServiceCollection.AddSingleton<ICache>(new Cache());

        // iOS has no accent color of the operating system: what looks like one is the tint color of the app
        // itself. PlatformStartup.AccentColor therefore stays null and the app paints its accents with the
        // AppAccentColor of App.axaml. Set it here only if this head ever gets a tint color that iOS itself
        // decides - a color of our own belongs in App.axaml, where every platform without one reads it.
        return base.CustomizeAppBuilder(builder)
            .WithInterFont();
    }
}