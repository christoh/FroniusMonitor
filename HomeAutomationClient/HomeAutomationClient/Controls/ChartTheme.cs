using ScottPlot;
using ScottPlot.AxisPanels;
using Color = ScottPlot.Color;

namespace De.Hochstaetter.HomeAutomationClient.Controls;

/// <summary>The two colors of the theme a chart has to follow; everything else in the chart has a color of its own.</summary>
public sealed record EnergyChartPalette(HaColor Foreground, HaColor Background);

/// <summary>
/// What every ScottPlot chart of the client does the same way: take the theme's two colours, colour the figure, the
/// grid and the legend with them, caption an axis, add a right axis. Shared by the price chart and the Solar.web
/// chart so that the two look like one application.
/// </summary>
public static class ChartTheme
{
    /// <summary>Colours the figure, the grid and the legend from the palette and answers the two colours as ScottPlot sees them.</summary>
    public static (Color Foreground, Color Background) Apply(Plot plot, EnergyChartPalette palette)
    {
        var foreground = ToColor(palette.Foreground);
        var background = ToColor(palette.Background);

        plot.FigureBackground.Color = background;
        plot.DataBackground.Color = background;
        plot.Grid.MajorLineColor = foreground.WithAlpha(0.15);
        plot.Grid.MinorLineColor = foreground.WithAlpha(0.06);
        plot.Legend.BackgroundColor = background;
        plot.Legend.FontColor = foreground;
        plot.Legend.OutlineColor = foreground.WithAlpha(0.5);
        plot.Legend.ShadowColor = ScottPlot.Colors.Transparent;
        plot.Axes.Title.Label.ForeColor = foreground;
        plot.Axes.Title.Label.FontSize = 18;
        plot.Axes.Title.Label.Bold = true;

        return (foreground, background);
    }

    /// <summary>The left and right axes come typed as their interfaces, which have no caption; every one of them is an AxisBase.</summary>
    public static void Caption(IAxis axis, string text, Color color)
    {
        if (axis is AxisBase axisBase)
        {
            axisBase.LabelText = text;
            axisBase.LabelFontColor = color;
        }
    }

    public static RightAxis AddRightAxis(Plot plot, string label, Color foreground)
    {
        var axis = plot.Axes.AddRightAxis();
        axis.LabelText = label;
        axis.LabelFontColor = foreground;
        axis.TickLabelStyle.ForeColor = foreground;
        axis.FrameLineStyle.Color = foreground;
        axis.MajorTickStyle.Color = foreground;
        axis.MinorTickStyle.Color = foreground;
        return axis;
    }

    /// <summary>The legend below the data area, horizontal, as every chart of the client has it.</summary>
    public static void LegendBelow(Plot plot)
    {
        plot.Legend.FontSize = 11;
        plot.Legend.Orientation = Orientation.Horizontal;
        plot.ShowLegend(Edge.Bottom);
    }

    public static Color ToColor(HaColor color) => new(color.R, color.G, color.B, color.A);

    /// <summary>A brush of the theme as a plain colour, so a renderer never sees an Avalonia type.</summary>
    public static HaColor ThemeColor(Control control, string key, HaColor fallback)
    {
        return control.TryFindResource(key, control.ActualThemeVariant, out var resource) && resource is ISolidColorBrush brush
            ? HaColor.FromArgb(brush.Color.A, brush.Color.R, brush.Color.G, brush.Color.B)
            : fallback;
    }

    /// <summary>The palette of the theme the control is drawn in: its foreground and the dialog background.</summary>
    public static EnergyChartPalette PaletteOf(Control control) => new(ThemeColor(control, "ForegroundBrush", HaColors.Black), ThemeColor(control, "DialogBackground", HaColors.White));
}
