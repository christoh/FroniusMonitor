namespace De.Hochstaetter.Fronius.Localization;

/// <summary>
/// The languages this build has a translation for. Nobody keeps a list: the satellite assemblies next to
/// <c>Fronius.dll</c> are the list, so adding a <c>Resources.&lt;culture&gt;.resx</c> is all it takes to support
/// one more language.
/// </summary>
public static class SupportedCultures
{
    private static readonly Lazy<IReadOnlyList<CultureInfo>> satellites = new(FindSatellites);

    /// <summary>
    /// The language of the neutral culture: <c>Resources.resx</c>, the one without a culture in its name, is
    /// English. It has no satellite assembly, so it never appears in <see cref="Satellites"/>.
    /// </summary>
    public const string NeutralLanguage = "en";

    /// <summary>
    /// The neutral culture as a <see cref="CultureInfo"/>, for a user interface that offers it like any other
    /// language. Use <see cref="CultureInfo.InvariantCulture"/> where a framework wants the fallback instead.
    /// </summary>
    public static CultureInfo NeutralCulture => CultureInfo.GetCultureInfo(NeutralLanguage);

    /// <summary>
    /// Every culture with a satellite assembly, ordered by name, so that a language and its regional variants
    /// stay together (de, de-CH, de-LI). The neutral culture is not among them.
    /// </summary>
    public static IReadOnlyList<CultureInfo> Satellites => satellites.Value;

    /// <summary>
    /// The neutral culture followed by all satellite cultures - what a language chooser offers.
    /// </summary>
    public static IReadOnlyList<CultureInfo> All => [NeutralCulture, ..Satellites];

    private static IReadOnlyList<CultureInfo> FindSatellites()
    {
        var assembly = typeof(Resources).Assembly;
        var directory = Path.GetDirectoryName(assembly.Location);

        if (string.IsNullOrEmpty(directory))
        {
            // A single file deployment has no satellite directories to look at. Nobody publishes this assembly
            // that way today; should that change, the translations need a different inventory.
            return [];
        }

        var satelliteName = $"{Path.GetFileNameWithoutExtension(assembly.Location)}.resources.dll";

        return Directory.EnumerateDirectories(directory)
            .Where(cultureDirectory => File.Exists(Path.Combine(cultureDirectory, satelliteName)))
            .Select(cultureDirectory => TryGetCulture(Path.GetFileName(cultureDirectory)))
            .Where(culture => culture != null)
            .Select(culture => culture!)
            .OrderBy(culture => culture.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// A directory next to the assembly does not have to be a culture, so an unknown name is skipped rather than
    /// throwing. The name is also normalized on the way: the directory is called de-ch, the culture is de-CH.
    /// </summary>
    private static CultureInfo? TryGetCulture(string name)
    {
        try
        {
            return CultureInfo.GetCultureInfo(name);
        }
        catch (CultureNotFoundException)
        {
            return null;
        }
    }
}
