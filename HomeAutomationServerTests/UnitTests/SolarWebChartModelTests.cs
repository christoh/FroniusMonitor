using System.Globalization;
using De.Hochstaetter.HomeAutomationClient.Models;

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests;

/// <summary>
/// What the client makes of a Solar.web chart: columns per day, month or year stacked in order with the missing
/// values filled, or areas and lines over a day with the state of charge on its own axis; Solar.web's colours where
/// it sent them, and the bubbles left out.
/// </summary>
public sealed class SolarWebChartModelTests
{
    private static readonly DateTime sep1 = new(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void A_month_chart_is_categorical_with_one_column_per_day_stacked_in_series_order()
    {
        var chart = new SolarWebChart
        {
            Interval = SolarWebInterval.Month,
            Title = "September 2026",
            SumValue = "1.205,36 kWh",
            Series =
            [
                Series("FromGenToGrid", "column", "kWh", "#999999", (sep1, 57.89), (sep1.AddDays(1), 70.05)),
                Series("FromGenToBatt", "column", "kWh", "#93CF82", (sep1, 8.07)),
                Series("PvForecastTruncated", "column", "kWh", null, (sep1.AddDays(19), 32.69)),
                Series("BattOperatingState", "bubble", "%", "#3CAE2B", (sep1, 73.1)),
            ],
        };

        var model = SolarWebChartModel.Build(chart);

        Assert.True(model.IsCategorical);
        Assert.Equal("September 2026", model.Title);
        Assert.Equal("1.205,36 kWh", model.SumValue);
        Assert.Equal(["1", "2", "20"], model.CategoryLabels);
        Assert.Equal("kWh", model.LeftAxisUnit);
        Assert.False(model.HasRightAxis);

        // The bubbles are not drawn; the three columns stack by Solar.web's known indices - the battery (2) under the
        // grid (1) under the forecast (0) - and get a value for every category.
        Assert.Equal(["FromGenToBatt", "FromGenToGrid", "PvForecastTruncated"], model.Series.Select(s => s.Id));
        Assert.All(model.Series, s => Assert.Equal(SolarWebSeriesKind.StackedColumn, s.Kind));
        Assert.Equal([8.07, null, null], model.Series[0].Values);
        Assert.Equal([57.89, 70.05, null], model.Series[1].Values);
        Assert.Equal([null, null, 32.69], model.Series[2].Values);

        // The known ids have their colours whatever Solar.web sent: grey for the grid, and the forecast in Solar.web's
        // yellow although it came with a pattern.
        Assert.Equal(HaColor.FromArgb(255, 0x99, 0x99, 0x99), model.Series[1].Color);
        Assert.Equal(HaColor.FromArgb(255, 0xF7, 0xC0, 0x02), model.Series[2].Color);
        Assert.True(model.Series[2].IsForecast);
        Assert.False(model.Series[1].IsForecast);

        // The axis reaches over the highest stack: the second day's 70.05 stands alone and beats 57.89 + 8.07 on the first.
        Assert.Equal(70.05 * 1.05, model.LeftAxisMaximum, 6);
    }

    [Fact]
    public void The_series_stack_by_highcharts_index_descending_with_unindexed_ones_left_where_they_were()
    {
        var chart = new SolarWebChart
        {
            Interval = SolarWebInterval.Month,
            Series =
            [
                Indexed(Series("FromGenToBatt", "column", "kWh", null, (sep1, 1)), 2),
                Indexed(Series("FromGenToGrid", "column", "kWh", null, (sep1, 1)), 1),
                Indexed(Series("FromGenToConsumer", "column", "kWh", null, (sep1, 1)), 6),
                Indexed(Series("PvForecastTruncated", "column", "kWh", null, (sep1, 1)), 0),
            ],
        };

        // Direct consumption at the bottom, the forecast on top, as Solar.web draws it.
        Assert.Equal(["FromGenToConsumer", "FromGenToBatt", "FromGenToGrid", "PvForecastTruncated"], SolarWebChartModel.Build(chart).Series.Select(s => s.Id));

        // Without indices - a chart cached before they were kept - the known ids take the indices Solar.web gives them.
        chart.Series.ForEach(s => s.Index = null);
        Assert.Equal(["FromGenToConsumer", "FromGenToBatt", "FromGenToGrid", "PvForecastTruncated"], SolarWebChartModel.Build(chart).Series.Select(s => s.Id));

        // Unknown ids without an index stay where Solar.web listed them, below every known one.
        var unknown = new SolarWebChart { Interval = SolarWebInterval.Month, Series = [Series("B", "column", "kWh", null, (sep1, 1)), Series("A", "column", "kWh", null, (sep1, 1)), Indexed(Series("C", "column", "kWh", null, (sep1, 1)), 0)] };
        Assert.Equal(["C", "B", "A"], SolarWebChartModel.Build(unknown).Series.Select(s => s.Id));
    }

    [Fact]
    public void Year_and_overall_charts_label_their_columns_by_month_and_by_year()
    {
        var year = SolarWebChartModel.Build(new SolarWebChart { Interval = SolarWebInterval.Year, Series = [Series("FromGenToGrid", "column", "MWh", null, (new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), 1), (new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc), 2))] });
        var all = SolarWebChartModel.Build(new SolarWebChart { Interval = SolarWebInterval.All, Series = [Series("FromGenToGrid", "column", "MWh", null, (new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc), 1), (new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), 2))] });

        Assert.Equal([CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(1), CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(9)], year.CategoryLabels);
        Assert.Equal(["2021", "2026"], all.CategoryLabels);
        Assert.Equal("MWh", all.LeftAxisUnit);
    }

    [Fact]
    public void A_day_chart_runs_over_local_time_with_areas_stacked_lines_gapped_and_the_state_of_charge_on_the_right()
    {
        var midnight = new DateTime(2026, 9, 18, 22, 0, 0, DateTimeKind.Utc);

        var chart = new SolarWebChart
        {
            Interval = SolarWebInterval.Day,
            Period = new DateOnly(2026, 9, 19),
            Title = "19.09.2026",
            SumValue = "72,78 kWh",
            Series =
            [
                Series("FromGenToBatt", "areaspline", "W", "#93CF82", (midnight, 0), (midnight.AddMinutes(5), 100)),
                Series("FromGenToGrid", "areaspline", "W", "#999999", (midnight, 50), (midnight.AddMinutes(5), 200)),
                Series("ToConsumer", "spline", "W", "#3CAE2B", (midnight, 315), (midnight.AddMinutes(5), null), (midnight.AddMinutes(10), 178)),
                Series("StateOfCharge", "spline", "%", "#3CAE2B", (midnight, 79), (midnight.AddMinutes(10), 73)),
                // Tomorrow's forecast comes with today's chart; it is not seen and does not stretch the axis.
                Series("PvForecastTruncated", "areaspline", "W", null, (midnight.AddDays(1).AddHours(12), 99999)),
            ],
        };

        var model = SolarWebChartModel.Build(chart);

        Assert.False(model.IsCategorical);
        Assert.Equal("W", model.LeftAxisUnit);
        Assert.True(model.HasRightAxis);
        // Exactly the chart's day, in the client's local time, whatever Solar.web sent.
        Assert.Equal(new DateTime(2026, 9, 19, 0, 0, 0, DateTimeKind.Local), model.AxisStart);
        Assert.Equal(DateTimeKind.Local, model.AxisStart.Kind);
        Assert.Equal(new DateTime(2026, 9, 20, 0, 0, 0, DateTimeKind.Local), model.AxisEnd);
        Assert.True(model.Series.Single(s => s.Id == "PvForecastTruncated").IsForecast);

        var consumption = model.Series.Single(s => s.Id == "ToConsumer");
        Assert.Equal(SolarWebSeriesKind.StackedArea, model.Series.Single(s => s.Id == "FromGenToBatt").Kind);
        Assert.Equal(SolarWebSeriesKind.Line, consumption.Kind);
        Assert.False(consumption.OnRightAxis);
        Assert.True(model.Series.Single(s => s.Id == "StateOfCharge").OnRightAxis);

        // The missing value is a gap, not a zero.
        Assert.True(double.IsNaN(consumption.Points[1].Value));
        Assert.Equal(178, consumption.Points[2].Value);

        // The axis reaches over the highest of the stacked areas (100 + 200) and the lines (315): the line wins here.
        Assert.Equal(315 * 1.05, model.LeftAxisMaximum, 6);
    }

    [Fact]
    public void Known_series_keep_their_colours_unknown_ones_take_solar_webs_or_a_fallback_nobody_else_has()
    {
        var chart = new SolarWebChart
        {
            Interval = SolarWebInterval.Day,
            Series =
            [
                // Solar.web draws the consumption light blue; the developer wants it orange.
                Series("ToConsumer", "spline", "W", "#70AFCD", (sep1, 1)),
                Series("StateOfCharge", "spline", "%", "#3CAE2B", (sep1, 1)),
                Series("Unknown1", "spline", "W", "#123456", (sep1, 1)),
                Series("Unknown2", "spline", "W", null, (sep1, 1)),
                Series("Unknown3", "spline", "W", null, (sep1, 1)),
            ],
        };

        var model = SolarWebChartModel.Build(chart);

        Assert.Equal(HaColor.FromArgb(255, 0xFF, 0x8C, 0x00), model.Series[0].Color);
        Assert.Equal(HaColor.FromArgb(255, 0x3C, 0xAE, 0x2B), model.Series[1].Color);
        Assert.Equal(HaColor.FromArgb(255, 0x12, 0x34, 0x56), model.Series[2].Color);
        // The first fallback is Solar.web's yellow, the second its grey; neither is taken by the series above.
        Assert.Equal(HaColor.FromArgb(255, 0xF7, 0xC0, 0x02), model.Series[3].Color);
        Assert.Equal(HaColor.FromArgb(255, 0x99, 0x99, 0x99), model.Series[4].Color);
        Assert.Equal(5, model.Series.Select(s => s.Color).Distinct().Count());
    }

    [Fact]
    public void An_empty_chart_still_builds()
    {
        var model = SolarWebChartModel.Build(new SolarWebChart { Interval = SolarWebInterval.Month, Title = "Oktober 2026" });

        Assert.True(model.IsCategorical);
        Assert.False(model.HasSeries);
        Assert.Empty(model.CategoryLabels);
        Assert.Equal(1, model.LeftAxisMaximum);
        Assert.Equal(string.Empty, model.LeftAxisUnit);
    }

    private static SolarWebSeries Indexed(SolarWebSeries series, int index)
    {
        series.Index = index;
        return series;
    }

    private static SolarWebSeries Series(string id, string type, string unit, string? color, params (DateTime Time, double? Value)[] points) => new()
    {
        Id = id,
        Name = id,
        ChartType = type,
        Unit = unit,
        Color = color,
        Points = [.. points.Select(p => new SolarWebPoint { TimeUtc = p.Time, Value = p.Value })],
    };
}
