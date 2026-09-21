using Avalonia.Controls;
using Avalonia.Styling;
using Avalonia.VisualTree;
using De.Hochstaetter.HomeAutomationClient.Controls;
using De.Hochstaetter.HomeAutomationClient.Views.Dialogs;
using ScottPlot.Avalonia;

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests;

/// <summary>
/// The Solar.web chart dialog's plot, before a chart has arrived from the server: it has to sit on the theme's
/// dialog background rather than ScottPlot's white, which is what it showed in dark mode until this was pinned.
/// </summary>
[Collection(AvaloniaCollection.Name)]
public sealed class SolarWebChartViewTests
{
    [Fact]
    public Task An_empty_plot_follows_dark_mode_before_a_chart_arrives() => HeadlessAvalonia.RunAsync(async () =>
    {
        HeadlessAvalonia.Reset();

        var view = new SolarWebChartView();
        var window = new Window
        {
            Width = 1160,
            Height = 520,
            RequestedThemeVariant = ThemeVariant.Dark,
            Content = view,
        };
        window.Show();
        await HeadlessAvalonia.SettleAsync();

        Assert.Equal(ThemeVariant.Dark, view.ActualThemeVariant);

        var plot = view.GetVisualDescendants().OfType<AvaPlot>().Single();
        var expected = ChartTheme.ToColor(ChartTheme.PaletteOf(view).Background);

        // ScottPlot's own default is white; the fallback of PaletteOf is white too, so both being white is the
        // bug, not an agreement. The dark dialog background is #002040.
        Assert.NotEqual(ScottPlot.Colors.White, expected);
        Assert.Equal(expected, plot.Plot.FigureBackground.Color);
        Assert.Equal(expected, plot.Plot.DataBackground.Color);
    });
}
