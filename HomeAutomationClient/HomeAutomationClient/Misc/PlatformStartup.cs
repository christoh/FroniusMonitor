using De.Hochstaetter.Fronius.Models;
using Microsoft.Extensions.Logging;

namespace De.Hochstaetter.HomeAutomationClient.Misc;

/// <summary>
/// Values that the platform heads determine before Avalonia starts.
/// </summary>
/// <remarks>
/// The startup code of a head runs before <see cref="App"/> is initialized and must not use Avalonia types, so a
/// color arrives as an <see cref="HaColor"/>. <see cref="App"/> turns it into a resource once the framework is up.
/// Everything stays <see langword="null"/> when the platform has nothing to offer or its detection failed; the app
/// then falls back to what the Fluent theme provides.
/// </remarks>
public static class PlatformStartup
{
    /// <summary>
    /// The accent color of the operating system, as far as the head could detect it. Windows exposes it to
    /// Avalonia itself, so the desktop head leaves this <see langword="null"/> and sets
    /// <see cref="AccentColorFollowsOs"/> instead.
    /// </summary>
    public static HaColor? AccentColor { get; set; }

    /// <summary>
    /// True where Avalonia fills the accent palette from the operating system on its own and keeps it up to date
    /// while the app runs - Windows. <see cref="App"/> then leaves the palette alone: neither with
    /// <see cref="AccentColor"/> nor with the AppAccentColor of the app, because any override would freeze the
    /// accent color at what it happened to be when the app started.
    /// </summary>
    public static bool AccentColorFollowsOs { get; set; }

    /// <summary>
    /// How the head wants log records written. A head that leaves this <see langword="null"/> gets
    /// <c>AddDebug</c>, which is as much as a browser or a phone can usefully do with them. The desktop head sets
    /// Serilog with a log file, the same way <c>FroniusMonitor</c> does.
    /// </summary>
    /// <remarks>
    /// This is only the provider. <see cref="App"/> calls <c>AddLogging</c> either way, because without a factory
    /// no constructor asking for an <see cref="ILogger{T}"/> can be resolved at all.
    /// </remarks>
    public static Action<ILoggingBuilder>? ConfigureLogging { get; set; }
}
