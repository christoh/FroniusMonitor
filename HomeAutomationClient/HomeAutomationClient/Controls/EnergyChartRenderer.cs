using De.Hochstaetter.Fronius.Models;
using ScottPlot;
using Color = ScottPlot.Color;
using ScottPlot.AxisPanels;

namespace De.Hochstaetter.HomeAutomationClient.Controls;

/// <summary>The two colors of the theme a chart has to follow; everything else in the chart has a color of its own.</summary>
public sealed record EnergyChartPalette(HaColor Foreground, HaColor Background);

/// <summary>
/// Draws an <see cref="EnergyChartModel"/> with ScottPlot. The shape is the WPF chart's: price bars on the left
/// axis with their values written over them, the sun and wind production of the grid hanging from the top on a
/// GW axis at the right, and - new here - the DWD radiation and wind speed as lines on two more axes at the right,
/// measured solid and forecast dashed.
/// </summary>
public static class EnergyChartRenderer
{
    private static readonly Color pricePositive = Color.FromHex("#20B2AA"); // LightSeaGreen, as in the WPF chart
    private static readonly Color priceNegative = Color.FromHex("#FF7F50"); // Coral
    private static readonly Color solar = Color.FromHex("#FFD000");
    private static readonly Color wind = Color.FromHex("#00BFFF"); // DeepSkyBlue
    private static readonly Color radiation = Color.FromHex("#FF8C00"); // DarkOrange
    private static readonly Color windSpeed = Color.FromHex("#8A2BE2"); // BlueViolet

    public static void Render(Plot plot, EnergyChartModel model, EnergyChartPalette palette)
    {
        var foreground = ToColor(palette.Foreground);
        var background = ToColor(palette.Background);

        plot.FigureBackground.Color = background;
        plot.DataBackground.Color = background;
        plot.Axes.Color(foreground);
        plot.Grid.MajorLineColor = foreground.WithAlpha(0.15);
        plot.Grid.MinorLineColor = foreground.WithAlpha(0.06);
        plot.Legend.BackgroundColor = background;
        plot.Legend.FontColor = foreground;
        plot.Legend.OutlineColor = foreground.WithAlpha(0.5);
        plot.Legend.ShadowColor = ScottPlot.Colors.Transparent;

        plot.Title(model.Title);
        plot.Axes.Title.Label.ForeColor = foreground;
        plot.Axes.Title.Label.FontSize = 18;
        plot.Axes.Title.Label.Bold = true;

        // Hours only, as the WPF axis had it: the day is in the title bar of the dialog and the date on every
        // tick left no room for the hours.
        var timeAxis = plot.Axes.DateTimeTicksBottom();
        timeAxis.TickGenerator = new ScottPlot.TickGenerators.DateTimeAutomatic { LabelFormatter = time => time.ToString("HH:mm", CultureInfo.CurrentCulture) };
        Caption(plot.Axes.Left, "ct/kWh", foreground);

        AddBars(plot, model.PositivePrices, pricePositive, foreground, plot.Axes.Left, model.PriceLegend);
        AddBars(plot, model.NegativePrices, priceNegative, foreground, plot.Axes.Left, model.NegativePriceLegend);

        plot.Axes.SetLimitsX(model.AxisStart.ToOADate(), model.AxisEnd.ToOADate());
        plot.Axes.SetLimitsY(model.PriceAxisMinimum, model.PriceAxisMaximum, plot.Axes.Left);

        if (model.HasProductions)
        {
            var productionAxis = plot.Axes.Right;
            Caption(productionAxis, "GW", foreground);
            productionAxis.TickLabelStyle.ForeColor = foreground;
            // The values hang from the top as negatives; the axis is captioned without the sign, as OxyPlot's
            // "#,0;#,0" format did.
            productionAxis.TickGenerator = new ScottPlot.TickGenerators.NumericAutomatic { LabelFormatter = value => Math.Abs(value).ToString("N0", CultureInfo.CurrentCulture) };
            AddBars(plot, model.Solar, solar, foreground, productionAxis, model.SolarLegend);
            AddBars(plot, model.Wind, wind, foreground, productionAxis, model.WindLegend);
            plot.Axes.SetLimitsY(model.ProductionAxisMinimum, 0, productionAxis);
        }
        else
        {
            plot.Axes.Right.IsVisible = false;
        }

        if (model.HasWeather)
        {
            if (model.RadiationMeasured.Count + model.RadiationForecast.Count > 0)
            {
                var axis = AddRightAxis(plot, "W/m²", foreground);
                AddLine(plot, model.RadiationMeasured, radiation, axis, model.RadiationLegend, dashed: false);
                AddLine(plot, model.RadiationForecast, radiation, axis, $"{model.RadiationLegend} - {model.ForecastLegend}", dashed: true);
                plot.Axes.SetLimitsY(0, model.RadiationAxisMaximum, axis);
            }

            if (model.WindSpeedMeasured.Count + model.WindSpeedForecast.Count > 0)
            {
                var axis = AddRightAxis(plot, "m/s", foreground);
                AddLine(plot, model.WindSpeedMeasured, windSpeed, axis, model.WindSpeedLegend, dashed: false);
                AddLine(plot, model.WindSpeedForecast, windSpeed, axis, $"{model.WindSpeedLegend} - {model.ForecastLegend}", dashed: true);
                plot.Axes.SetLimitsY(0, model.WindSpeedAxisMaximum, axis);
            }
        }

        // Outside the data area, as the WPF legend was: inside it covers the production bars, which hang exactly
        // where a legend would sit. Below rather than beside, because the right edge already carries three axes.
        plot.Legend.FontSize = 11;
        plot.Legend.Orientation = Orientation.Horizontal;
        plot.ShowLegend(Edge.Bottom);
    }

    private static void AddBars(Plot plot, IReadOnlyList<ChartBar> bars, Color fill, Color foreground, IYAxis axis, string legend)
    {
        if (bars.Count == 0)
        {
            return;
        }

        var plottable = plot.Add.Bars(bars.Select(b => new Bar
        {
            Position = b.Start.ToOADate() + (b.End - b.Start).TotalDays / 2,
            Size = (b.End - b.Start).TotalDays * 0.92,
            ValueBase = b.Base,
            Value = b.Value,
            FillColor = fill,
            LineColor = foreground.WithAlpha(0.6),
            LineWidth = 0.5f,
            Label = b.Label ?? string.Empty,
        }).ToList());

        plottable.Axes.YAxis = axis;
        plottable.LegendText = legend;
        plottable.ValueLabelStyle.ForeColor = foreground;
        plottable.ValueLabelStyle.FontSize = 11;
    }

    private static void AddLine(Plot plot, IReadOnlyList<ChartPoint> points, Color color, IYAxis axis, string legend, bool dashed)
    {
        if (points.Count == 0)
        {
            return;
        }

        var scatter = plot.Add.Scatter(points.Select(p => p.Time.ToOADate()).ToArray(), points.Select(p => p.Value).ToArray());
        scatter.Axes.YAxis = axis;
        scatter.Color = color;
        scatter.LineWidth = 2;
        scatter.MarkerSize = 4;
        scatter.LinePattern = dashed ? LinePattern.Dashed : LinePattern.Solid;
        scatter.LegendText = legend;
    }

    private static RightAxis AddRightAxis(Plot plot, string label, Color foreground)
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

    /// <summary>The left and right axes come typed as their interfaces, which have no caption; every one of them is an AxisBase.</summary>
    private static void Caption(IAxis axis, string text, Color color)
    {
        if (axis is AxisBase axisBase)
        {
            axisBase.LabelText = text;
            axisBase.LabelFontColor = color;
        }
    }

    private static Color ToColor(HaColor color) => new(color.R, color.G, color.B, color.A);
}
