using System.Collections;
using System.Globalization;
using System.Resources;
using System.Text.RegularExpressions;
using De.Hochstaetter.Fronius.Localization;

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests;

/// <summary>
/// What can go wrong in a translation without anybody noticing.
/// </summary>
/// <remarks>
/// <para>
/// A culture file holds an entry only where its translation differs from the neutral English one - German has no
/// <c>Status</c> and Italian no <c>Austria</c>, because those are the same word. So "every key in every language"
/// is not the rule and cannot be tested. These two things can be:
/// </para>
/// <para>
/// A key that exists only in a translation is dead: a typo in the name, or a string that was renamed in the
/// neutral file and left behind in the others. Nothing ever reads it and nothing complains. And a translation
/// that loses a <c>{0}</c> silently drops whatever was to be put there - or, the other way round, invents a
/// placeholder that <c>string.Format</c> is never given and throws.
/// </para>
/// </remarks>
public class LocalizationTests
{
    private static readonly string[] cultures = ["de", "de-ch", "de-li", "fr", "gsw", "it", "rm"];

    private static IReadOnlyDictionary<string, string> Strings(CultureInfo culture, bool tryParents)
    {
        var set = Resources.ResourceManager.GetResourceSet(culture, createIfNotExists: true, tryParents: tryParents);
        Assert.NotNull(set);

        return set.Cast<DictionaryEntry>()
            .Where(entry => entry.Value is string)
            .ToDictionary(entry => (string)entry.Key, entry => (string)entry.Value!);
    }

    private static IReadOnlySet<string> Placeholders(string text) =>
        Regex.Matches(text, @"\{(\d+)").Select(match => match.Groups[1].Value).ToHashSet();

    [Theory]
    [InlineData("de")]
    [InlineData("de-ch")]
    [InlineData("de-li")]
    [InlineData("fr")]
    [InlineData("gsw")]
    [InlineData("it")]
    [InlineData("rm")]
    public void A_translation_never_names_a_string_that_does_not_exist(string culture)
    {
        var neutral = Strings(CultureInfo.InvariantCulture, tryParents: true);
        var translated = Strings(CultureInfo.GetCultureInfo(culture), tryParents: false);

        var orphans = translated.Keys.Where(key => !neutral.ContainsKey(key)).OrderBy(key => key, StringComparer.Ordinal).ToList();

        Assert.Empty(orphans);
    }

    [Theory]
    [InlineData("de")]
    [InlineData("de-ch")]
    [InlineData("de-li")]
    [InlineData("fr")]
    [InlineData("gsw")]
    [InlineData("it")]
    [InlineData("rm")]
    public void A_translation_keeps_the_placeholders_of_the_string_it_translates(string culture)
    {
        var neutral = Strings(CultureInfo.InvariantCulture, tryParents: true);
        var translated = Strings(CultureInfo.GetCultureInfo(culture), tryParents: false);

        var mismatches = translated
            .Where(entry => neutral.TryGetValue(entry.Key, out var english) && !Placeholders(english).SetEquals(Placeholders(entry.Value)))
            .Select(entry => $"{entry.Key}: '{neutral[entry.Key]}' -> '{entry.Value}'")
            .OrderBy(message => message, StringComparer.Ordinal)
            .ToList();

        Assert.Empty(mismatches);
    }

    [Theory]
    [InlineData("de")]
    [InlineData("fr")]
    [InlineData("gsw")]
    [InlineData("it")]
    [InlineData("rm")]
    public void The_busy_texts_of_the_settings_dialog_are_translated(string culture)
    {
        // These two were English in four languages until somebody noticed. They are what the dialog shows while it
        // is talking to an inverter, so they are the most visible strings it has.
        var translated = Strings(CultureInfo.GetCultureInfo(culture), tryParents: false);

        Assert.Contains(nameof(Resources.ReadingInverterSettings), translated.Keys);
        Assert.Contains(nameof(Resources.SavingSettings), translated.Keys);
        Assert.Contains("{0}", translated[nameof(Resources.SavingSettings)]);
    }
}
