using ScottPlot;
using ScottPlot.AxisRules;
using ScottPlot.TickGenerators;
using Color = ScottPlot.Color;

namespace De.Hochstaetter.HomeAutomationClient.Controls;

/// <summary>
/// Draws a <see cref="SolarWebChartModel"/> with ScottPlot the way Solar.web draws it: stacked columns per day,
/// month or year, or stacked areas and lines over the day, in Solar.web's colours, with a percentage axis at the
/// right for the state of charge.
/// </summary>
public static class SolarWebChartRenderer
{
    public static void Render(Plot plot, SolarWebChartModel model, EnergyChartPalette palette)
    {
        var (foreground, _) = ChartTheme.Apply(plot, palette);
        plot.Title(model.SumValue.Length > 0 ? $"{model.Title} - {model.SumValue}" : model.Title);

        if (model.IsCategorical)
        {
            RenderCategorical(plot, model, foreground);
        }
        else
        {
            RenderOverTime(plot, model, foreground);
        }

        // Set and locked: a rule is applied on every render, so whatever re-scales the axes of the live control between
        // two drawings - the second chart of the dialog came up with -10..10 on the left, and nothing here is ever
        // negative - the ranges stay what the model says. Interaction is off anyway, so locking costs nothing.
        ChartTheme.Caption(plot.Axes.Left, model.LeftAxisUnit, foreground);
        plot.Axes.SetLimitsY(0, model.LeftAxisMaximum, plot.Axes.Left);
        plot.Axes.Rules.Add(new LockedVertical(plot.Axes.Left, 0, model.LeftAxisMaximum));

        if (model.HasRightAxis)
        {
            ChartTheme.Caption(plot.Axes.Right, "%", foreground);
            plot.Axes.SetLimitsY(0, 100, plot.Axes.Right);
            plot.Axes.Rules.Add(new LockedVertical(plot.Axes.Right, 0, 100));
        }
        else
        {
            plot.Axes.Right.IsVisible = false;
        }

        ChartTheme.LegendBelow(plot);
    }

    /// <summary>One column per category, the series stacked in their order; a line series is drawn over the columns.</summary>
    private static void RenderCategorical(Plot plot, SolarWebChartModel model, Color foreground)
    {
        var count = model.CategoryLabels.Count;
        var bases = new double[count];

        // The columns first, then the lines over them, whatever order the stack puts them in.
        foreach (var series in model.Series.OrderBy(s => s.Kind == SolarWebSeriesKind.Line ? 1 : 0))
        {
            var color = Fill(series);

            if (series.Kind == SolarWebSeriesKind.Line)
            {
                var xs = Enumerable.Range(0, count).Where(i => series.Values[i] != null).Select(i => (double)i).ToArray();
                var ys = Enumerable.Range(0, count).Where(i => series.Values[i] != null).Select(i => series.Values[i]!.Value).ToArray();
                AddLine(plot, xs, ys, color, series.OnRightAxis, series.Name);
                continue;
            }

            var bars = new List<Bar>();

            for (var i = 0; i < count; i++)
            {
                if (series.Values[i] is not { } value || value == 0)
                {
                    continue;
                }

                // A percentage stacks on nothing: it has an axis of its own.
                var valueBase = series.OnRightAxis ? 0 : bases[i];
                bars.Add(new Bar { Position = i, ValueBase = valueBase, Value = valueBase + value, Size = 0.75, FillColor = color, LineWidth = 0 });

                if (!series.OnRightAxis)
                {
                    bases[i] += value;
                }
            }

            if (bars.Count == 0)
            {
                continue;
            }

            var plottable = plot.Add.Bars(bars);
            plottable.Axes.YAxis = series.OnRightAxis ? plot.Axes.Right : plot.Axes.Left;
            plottable.LegendText = series.Name;
        }

        // The Axes.Color call replaces nothing here, unlike DateTimeTicksBottom does below, so it may come first.
        plot.Axes.Color(foreground);
        plot.Axes.Bottom.TickGenerator = new NumericManual(Enumerable.Range(0, count).Select(i => (double)i).ToArray(), model.CategoryLabels.ToArray());
        plot.Axes.Bottom.MinorTickStyle.Length = 0;
        plot.Axes.SetLimitsX(-0.6, Math.Max(count, 1) - 0.4);
        plot.Axes.Rules.Add(new LockedHorizontal(plot.Axes.Bottom, -0.6, Math.Max(count, 1) - 0.4));
    }

    /// <summary>Stacked areas over the day, each on top of the ones before it, and the lines over them.</summary>
    private static void RenderOverTime(Plot plot, SolarWebChartModel model, Color foreground)
    {
        // The time axis first, before anything is added: DateTimeTicksBottom replaces the bottom axis, and a plottable
        // added before that keeps the replaced one - an axis nobody sets, which the render then autoscales, and the
        // left axis with it. That is what put -10..10 on the left axis on the second chart of the dialog (2026-09-20).
        // Hours only, as the price chart has it: the day is in the title.
        var timeAxis = plot.Axes.DateTimeTicksBottom();
        timeAxis.TickGenerator = new DateTimeAutomatic { LabelFormatter = time => time.ToString("HH:mm", CultureInfo.CurrentCulture) };

        // Every instant any series has, so the areas stack at the same x positions whatever each series sent.
        var times = model.Series.SelectMany(s => s.Points).Select(p => p.Time).Distinct().Order().ToArray();
        var xs = times.Select(t => t.ToOADate()).ToArray();
        var index = times.Select((t, i) => (t, i)).ToDictionary(x => x.t, x => x.i);
        var lower = new double[times.Length];

        // The areas first, then the lines over them: the state of charge has the highest index, which would put it
        // under every area otherwise.
        foreach (var series in model.Series.OrderBy(s => s.Kind == SolarWebSeriesKind.Line ? 1 : 0))
        {
            var color = Fill(series);

            if (series.Kind == SolarWebSeriesKind.Line)
            {
                // A gap in the data is a gap in the line: one scatter per stretch of values, the legend on the first.
                var first = true;

                foreach (var stretch in Stretches(series.Points))
                {
                    AddLine(plot, stretch.Select(p => p.Time.ToOADate()).ToArray(), stretch.Select(p => p.Value).ToArray(), color, series.OnRightAxis, first ? series.Name : null);
                    first = false;
                }

                continue;
            }

            var upper = (double[])lower.Clone();

            foreach (var point in series.Points.Where(p => !double.IsNaN(p.Value)))
            {
                upper[index[point.Time]] += point.Value;
            }

            var fill = plot.Add.FillY(xs, lower, upper);
            fill.FillStyle.Color = color.WithAlpha(series.IsForecast ? 0.4 : 0.85);
            fill.LineStyle.Width = 0;
            fill.MarkerStyle.IsVisible = false;
            fill.LegendText = series.Name;
            lower = upper;
        }

        // Coloured after DateTimeTicksBottom, which the axes were created by.
        plot.Axes.Color(foreground);
        plot.Axes.SetLimitsX(model.AxisStart.ToOADate(), model.AxisEnd.ToOADate());
        plot.Axes.Rules.Add(new LockedHorizontal(plot.Axes.Bottom, model.AxisStart.ToOADate(), model.AxisEnd.ToOADate()));
    }

    /// <summary>The series' colour, translucent for the forecast, which Solar.web hatches instead.</summary>
    private static Color Fill(SolarWebChartSeries series) => series.IsForecast ? ChartTheme.ToColor(series.Color).WithAlpha(0.45) : ChartTheme.ToColor(series.Color);

    private static void AddLine(Plot plot, double[] xs, double[] ys, Color color, bool onRightAxis, string? legend)
    {
        if (xs.Length == 0)
        {
            return;
        }

        var scatter = plot.Add.Scatter(xs, ys);
        scatter.Axes.YAxis = onRightAxis ? plot.Axes.Right : plot.Axes.Left;
        scatter.Color = color;
        scatter.LineWidth = 2;
        scatter.MarkerSize = 0;

        if (legend != null)
        {
            scatter.LegendText = legend;
        }
    }

    /// <summary>The runs of points between the gaps (NaN values), each at least one point long.</summary>
    private static IEnumerable<List<ChartPoint>> Stretches(IReadOnlyList<ChartPoint> points)
    {
        var current = new List<ChartPoint>();

        foreach (var point in points)
        {
            if (double.IsNaN(point.Value))
            {
                if (current.Count > 0)
                {
                    yield return current;
                    current = [];
                }

                continue;
            }

            current.Add(point);
        }

        if (current.Count > 0)
        {
            yield return current;
        }
    }
}
