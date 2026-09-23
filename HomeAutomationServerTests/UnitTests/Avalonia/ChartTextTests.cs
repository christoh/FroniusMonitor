using System.Collections;
using System.Globalization;
using System.Resources;
using Avalonia.Controls;
using Avalonia.VisualTree;
using De.Hochstaetter.Fronius.Localization;
using De.Hochstaetter.HomeAutomationClient.Controls;
using De.Hochstaetter.HomeAutomationClient.Views.Dialogs;
using ScottPlot.Avalonia;

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests;

/// <summary>
/// That ScottPlot draws the accented letters of the translations as they are written. Pinned on 2026-09-23, when
/// resource strings were found double encoded - <c>PrÃ©vision</c> for <c>Prévision</c> - and the question was
/// whether the charts needed them that way. They do not: ScottPlot hands the .NET string to Skia as it is, in the
/// app's own Inter, so a double encoded string is drawn exactly as garbled as it reads.
/// </summary>
/// <remarks>
/// Headless Avalonia because the font is an <c>avares://</c> asset, which only a running Avalonia can open. The
/// headless platform draws nothing of its own; the pixels compared here are ScottPlot's, rendered through Skia.
/// </remarks>
[Collection(AvaloniaCollection.Name)]
public sealed class ChartTextTests
{
    /// <summary>The price chart's plot, with the font registered the way the app registers it.</summary>
    private static async Task<AvaPlot> CreatePlotAsync()
    {
        var view = new EnergyChartView();
        new Window { Width = 800, Height = 400, Content = view }.Show();
        await HeadlessAvalonia.SettleAsync();
        return view.GetVisualDescendants().OfType<AvaPlot>().Single();
    }

    private static byte[] Draw(AvaPlot plot, string title)
    {
        plot.Plot.Title(title);
        return plot.Plot.GetImage(400, 200).GetImageBytes();
    }

    [Fact]
    public Task The_chart_font_has_a_glyph_for_every_character_of_every_translation() => HeadlessAvalonia.RunAsync(async () =>
    {
        HeadlessAvalonia.Reset();
        await CreatePlotAsync();

        Assert.Equal(InterFontResolver.FamilyName, ScottPlot.Fonts.Default);
        var typeface = ScottPlot.Fonts.GetTypeface(ScottPlot.Fonts.Default, bold: false, italic: false);
        Assert.Equal(InterFontResolver.FamilyName, typeface.FamilyName);

        var used = SupportedCultures.All
            .Select(culture => Resources.ResourceManager.GetResourceSet(culture.Name == SupportedCultures.NeutralLanguage ? CultureInfo.InvariantCulture : culture, createIfNotExists: true, tryParents: false))
            .OfType<ResourceSet>()
            .SelectMany(set => set.Cast<DictionaryEntry>())
            .Select(entry => entry.Value)
            .OfType<string>()
            .SelectMany(text => text)
            .Where(character => character > '\x7f')
            .ToHashSet();

        // Proof that the translations were read at all, and that the font can say no - without either, an
        // empty list of missing glyphs would prove nothing.
        Assert.Superset(new HashSet<char> { 'é', 'è', 'ä', 'ß', 'Δ' }, used);
        Assert.False(typeface.ContainsGlyphs("\u4E2D"));

        Assert.Empty(used.Where(character => !typeface.ContainsGlyphs(character.ToString())).Order());
    });

    /// <summary>
    /// The accent is drawn, and drawn as itself: not left out (as <c>e</c>), not as the box of a missing glyph
    /// (as a Chinese character, which Inter has no glyph for), and not as another accent (as <c>è</c>).
    /// </summary>
    [Fact]
    public Task An_accented_letter_is_drawn_as_its_own_glyph() => HeadlessAvalonia.RunAsync(async () =>
    {
        HeadlessAvalonia.Reset();
        var plot = await CreatePlotAsync();

        var accented = Draw(plot, "Prévision");

        Assert.NotEqual(Draw(plot, "Prevision"), accented);
        Assert.NotEqual(Draw(plot, "Pr\u4E2Dvision"), accented);
        Assert.NotEqual(Draw(plot, "Prèvision"), accented);
        Assert.NotEqual(Draw(plot, "PrÃ©vision"), accented);
        // The same text twice is the same picture, or the inequalities above would prove nothing.
        Assert.Equal(accented, Draw(plot, "Prévision"));
    });
}
