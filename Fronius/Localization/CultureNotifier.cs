namespace De.Hochstaetter.Fronius.Localization;

/// <summary>
/// Notifies subscribers that the application UI language changed at runtime, so cached,
/// language-dependent data (e.g. inverter UI/channel/config localization downloaded per language)
/// can be invalidated and re-fetched in the new language.
/// </summary>
public static class CultureNotifier
{
    public static event EventHandler? CultureChanged;

    public static void NotifyCultureChanged() => CultureChanged?.Invoke(null, EventArgs.Empty);
}
