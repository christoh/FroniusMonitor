namespace De.Hochstaetter.HomeAutomationClient.Models;

/// <summary>How a series of a Solar.web chart is drawn. Solar.web's own words for it are Highcharts' series types.</summary>
public enum SolarWebSeriesKind
{
    /// <summary>Highcharts <c>column</c>: one column per category, stacked on the columns of the series before it.</summary>
    StackedColumn,

    /// <summary>Highcharts <c>areaspline</c>: an area over time, stacked on the areas of the series before it.</summary>
    StackedArea,

    /// <summary>Highcharts <c>spline</c>: a line over time, not stacked.</summary>
    Line,
}

/// <summary>
/// One series of a Solar.web chart as the view draws it: in a categorical chart one value per category (a day of
/// the month, a month of the year, a year), in a chart over time one point per instant with <see cref="double.NaN"/>
/// where Solar.web sent none.
/// </summary>
public sealed record SolarWebChartSeries(string Id, string Name, HaColor Color, SolarWebSeriesKind Kind, bool OnRightAxis, IReadOnlyList<double?> Values, IReadOnlyList<ChartPoint> Points)
{
    /// <summary>Solar.web hatches its forecast where everything else is solid; the view draws it translucent for the same reason.</summary>
    public bool IsForecast => SolarWebSeries.IsForecastId(Id);
}

/// <summary>
/// A Solar.web chart as numbers and captions, worked out by the view model and drawn by the view. Nothing in here
/// knows the charting library: every time is local, the colours are <see cref="HaColor"/>s, and what is stacked on
/// what is decided here. That is what lets the same model be tested without a control.
/// </summary>
/// <remarks>
/// <para>
/// Two shapes, decided by the interval: the day chart is a chart <b>over time</b> - areas and lines over the
/// instants Solar.web sent - and the month, year and overall charts are <b>categorical</b>: one column per day,
/// month or year, stacked. Solar.web stamps those columns at midnight UTC of their date, so they are read as dates
/// and never converted to local time; the day chart's instants are.
/// </para>
/// <para>
/// A percentage series - the state of charge - goes on a right axis from 0 to 100; everything else shares the left
/// axis, whose unit is the one Solar.web gave those series (W, kWh, MWh, EUR). The battery state bubbles are left
/// out: a bubble with a text is a tooltip, and there are none here.
/// </para>
/// </remarks>
public sealed class SolarWebChartModel
{
    /// <summary>
    /// The colours of the series Solar.web is known to send, by id, which win over what Solar.web sends: the developer
    /// wants the two battery series green, direct consumption yellow, the consumption orange and the forecast light
    /// blue (Solar.web draws the consumption in that light blue and hatches the forecast in the production yellow),
    /// decided 2026-09-20; the rest are Solar.web's own. The ids are the same in every language.
    /// </summary>
    private static readonly Dictionary<string, HaColor> knownColors = new(StringComparer.Ordinal)
    {
        ["FromGenToBatt"] = HaColor.FromArgb(255, 0x93, 0xCF, 0x82),
        ["FromBattToConsumer"] = HaColor.FromArgb(255, 0x93, 0xCF, 0x82),
        ["StateOfCharge"] = HaColor.FromArgb(255, 0x3C, 0xAE, 0x2B),
        ["FromGenToConsumer"] = HaColor.FromArgb(255, 0xFA, 0xD9, 0x67),
        ["ToConsumer"] = HaColor.FromArgb(255, 0xFF, 0x8C, 0x00),
        ["FromGenToGrid"] = HaColor.FromArgb(255, 0x99, 0x99, 0x99),
        ["FromGridToConsumer"] = HaColor.FromArgb(255, 0x99, 0x99, 0x99),
        ["FromGen"] = HaColor.FromArgb(255, 0xF7, 0xC0, 0x02),
        ["FromGenToSomewhere"] = HaColor.FromArgb(255, 0xF7, 0xC0, 0x02),
        ["PvForecastTruncated"] = HaColor.FromArgb(255, 0x70, 0xAF, 0xCD),
        ["FromGenToWattPilot"] = HaColor.FromArgb(255, 0xAF, 0x79, 0xB5),
        ["Saving"] = HaColor.FromArgb(255, 0x6C, 0xBE, 0x58),
        ["Income"] = HaColor.FromArgb(255, 0xFA, 0xD9, 0x67),
        ["Expense"] = HaColor.FromArgb(255, 0x99, 0x99, 0x99),
    };

    /// <summary>
    /// Highcharts' indices Solar.web gives its series, seen 2026-09-20, for a chart that comes without them - one the
    /// server cached before it kept the index. The highest is at the bottom of the stack: direct consumption, then the
    /// Wattpilot, the battery, the grid, the forecast; in the consumption view direct consumption, the battery, the grid.
    /// </summary>
    private static readonly Dictionary<string, int> knownIndices = new(StringComparer.Ordinal)
    {
        ["FromGenToConsumer"] = 6,
        ["FromGenToWattPilot"] = 4,
        ["FromGenToSomewhere"] = 3,
        ["FromGenToBatt"] = 2,
        ["FromBattToConsumer"] = 1,
        ["FromGenToGrid"] = 1,
        ["FromGridToConsumer"] = 0,
        ["PvForecastTruncated"] = 0,
    };

    /// <summary>For a series that is neither known nor coloured by Solar.web.</summary>
    private static readonly HaColor[] fallbackColors =
    [
        HaColor.FromArgb(255, 0xF7, 0xC0, 0x02), // Solar.web's yellow
        HaColor.FromArgb(255, 0x99, 0x99, 0x99),
        HaColor.FromArgb(255, 0x93, 0xCF, 0x82),
        HaColor.FromArgb(255, 0xAF, 0x79, 0xB5),
        HaColor.FromArgb(255, 0x3C, 0xAE, 0x2B),
        HaColor.FromArgb(255, 0x00, 0xBF, 0xFF),
    ];

    public required string Title { get; init; }

    public required string SumValue { get; init; }

    public bool IsPremiumFeature { get; init; }

    /// <summary>True for a month, a year and the whole history; false for a day, which is drawn over time.</summary>
    public bool IsCategorical { get; init; }

    /// <summary>The tick label of each category, in order: the day of the month, the abbreviated month, the year.</summary>
    public IReadOnlyList<string> CategoryLabels { get; init; } = [];

    /// <summary>The span of the time axis of a day chart, local time: the chart's day from midnight to midnight.</summary>
    public DateTime AxisStart { get; init; }

    public DateTime AxisEnd { get; init; }

    public IReadOnlyList<SolarWebChartSeries> Series { get; init; } = [];

    /// <summary>The unit of the left axis: W, kWh, MWh or EUR, whichever the series have.</summary>
    public string LeftAxisUnit { get; init; } = string.Empty;

    public bool HasRightAxis => Series.Any(s => s.OnRightAxis);

    public bool HasSeries => Series.Count > 0;

    /// <summary>The top of the left axis: the highest stack, or the highest line, with a little room above it.</summary>
    public double LeftAxisMaximum { get; init; } = 1;

    public static SolarWebChartModel Build(SolarWebChart chart)
    {
        // Solar.web stacks the series with the highest Highcharts index at the bottom (reversedStacks), so that is the
        // order here, bottom first: direct consumption under the Wattpilot, the battery, the grid, the forecast. A
        // series without an index - a chart cached before the index was kept - takes the index Solar.web is known to
        // give it, and an unknown one stays where Solar.web listed it.
        var drawable = chart.Series.Where(s => Kind(s.ChartType) != null).OrderByDescending(StackIndex).ToList();
        var leftUnit = drawable.Where(s => s.Unit != "%").Select(s => s.Unit).GroupBy(u => u).OrderByDescending(g => g.Count()).Select(g => g.Key).FirstOrDefault() ?? string.Empty;

        return chart.Interval == SolarWebInterval.Day ? BuildOverTime(chart, drawable, leftUnit) : BuildCategorical(chart, drawable, leftUnit);
    }

    private static SolarWebChartModel BuildCategorical(SolarWebChart chart, List<SolarWebSeries> drawable, string leftUnit)
    {
        // The categories are every date any series has a value for, in order; a series without a value for one
        // of them gets a null there, so the columns of the other series still stand at the right place.
        var categories = drawable.SelectMany(s => s.Points).Select(p => DateOnly.FromDateTime(p.TimeUtc)).Distinct().Order().ToList();
        var index = categories.Select((c, i) => (c, i)).ToDictionary(x => x.c, x => x.i);

        var used = new HashSet<HaColor>();

        var series = drawable.Select(s =>
        {
            var values = new double?[categories.Count];

            foreach (var point in s.Points)
            {
                values[index[DateOnly.FromDateTime(point.TimeUtc)]] = point.Value;
            }

            return new SolarWebChartSeries(s.Id, s.Name, Color(s, used), Kind(s.ChartType)!.Value, s.Unit == "%", values, []);
        }).ToList();

        var stackTops = Enumerable.Range(0, categories.Count).Select(c => series.Where(s => s.Kind == SolarWebSeriesKind.StackedColumn && !s.OnRightAxis).Sum(s => s.Values[c] ?? 0)).ToList();
        var lineTops = series.Where(s => s.Kind != SolarWebSeriesKind.StackedColumn && !s.OnRightAxis).SelectMany(s => s.Values).Select(v => v ?? 0);

        return new SolarWebChartModel
        {
            Title = chart.Title,
            SumValue = chart.SumValue,
            IsPremiumFeature = chart.IsPremiumFeature,
            IsCategorical = true,
            CategoryLabels = categories.Select(c => Label(chart.Interval, c)).ToList(),
            Series = series,
            LeftAxisUnit = leftUnit,
            LeftAxisMaximum = Maximum(stackTops.Concat(lineTops)),
        };
    }

    private static SolarWebChartModel BuildOverTime(SolarWebChart chart, List<SolarWebSeries> drawable, string leftUnit)
    {
        var used = new HashSet<HaColor>();

        var series = drawable
            .Select(s => new SolarWebChartSeries(s.Id, s.Name, Color(s, used), Kind(s.ChartType)!.Value, s.Unit == "%", [],
                s.Points.OrderBy(p => p.TimeUtc).Select(p => new ChartPoint(Local(p.TimeUtc), p.Value ?? double.NaN)).ToList()))
            .ToList();

        // Exactly the chart's day, midnight to midnight, whatever Solar.web sent: today's chart carries the forecast of
        // the next two days as well, and that is looked at by stepping to those days, not by squeezing three days
        // into one chart. The axis is locked to this span, so points beyond it are simply not seen.
        var start = chart.Period.ToDateTime(TimeOnly.MinValue, DateTimeKind.Local);
        var end = start.AddDays(1);

        // The stacked areas add up at every instant; the lines stand on their own. NaN counts as nothing, and so
        // does whatever lies outside the day.
        var stackTops = series
            .Where(s => s.Kind == SolarWebSeriesKind.StackedArea && !s.OnRightAxis)
            .SelectMany(s => s.Points)
            .Where(p => p.Time >= start && p.Time < end)
            .GroupBy(p => p.Time)
            .Select(g => g.Sum(p => double.IsNaN(p.Value) ? 0 : p.Value));
        var lineTops = series.Where(s => s.Kind != SolarWebSeriesKind.StackedArea && !s.OnRightAxis).SelectMany(s => s.Points).Where(p => p.Time >= start && p.Time < end).Select(p => double.IsNaN(p.Value) ? 0 : p.Value);

        return new SolarWebChartModel
        {
            Title = chart.Title,
            SumValue = chart.SumValue,
            IsPremiumFeature = chart.IsPremiumFeature,
            IsCategorical = false,
            AxisStart = start,
            AxisEnd = end,
            Series = series,
            LeftAxisUnit = leftUnit,
            LeftAxisMaximum = Maximum(stackTops.Concat(lineTops)),
        };
    }

    /// <summary>What the series stacks by: its own index, else the one Solar.web is known to give the id, else nothing.</summary>
    public static int StackIndex(SolarWebSeries series) => series.Index ?? (knownIndices.TryGetValue(series.Id, out var known) ? known : int.MinValue);

    /// <summary>Highcharts' series type as a way of drawing, or <see langword="null"/> for a type that is not drawn (the bubbles).</summary>
    public static SolarWebSeriesKind? Kind(string chartType) => chartType switch
    {
        "column" => SolarWebSeriesKind.StackedColumn,
        "areaspline" or "area" => SolarWebSeriesKind.StackedArea,
        "spline" or "line" => SolarWebSeriesKind.Line,
        _ => null,
    };

    /// <summary>What a column is called on the axis: the day of the month, the month, or the year.</summary>
    public static string Label(SolarWebInterval interval, DateOnly date) => interval switch
    {
        SolarWebInterval.Month => date.Day.ToString(CultureInfo.CurrentCulture),
        SolarWebInterval.Year => CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(date.Month),
        _ => date.Year.ToString(CultureInfo.CurrentCulture),
    };

    /// <summary>
    /// The known colour of the series' id, else Solar.web's colour (<c>#RRGGBB</c>) where it sent one, else the first
    /// fallback colour no series of the chart has taken yet. <paramref name="used"/> collects the colours handed out, so
    /// two unknown series never share one by accident.
    /// </summary>
    public static HaColor Color(SolarWebSeries series, ISet<HaColor> used)
    {
        if (knownColors.TryGetValue(series.Id, out var known))
        {
            used.Add(known);
            return known;
        }

        if (series.Color is { Length: 7 } hex && hex[0] == '#'
            && byte.TryParse(hex.AsSpan(1, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var r)
            && byte.TryParse(hex.AsSpan(3, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var g)
            && byte.TryParse(hex.AsSpan(5, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var b))
        {
            var own = HaColor.FromArgb(255, r, g, b);
            used.Add(own);
            return own;
        }

        var fallback = fallbackColors.FirstOrDefault(c => !used.Contains(c), fallbackColors[used.Count % fallbackColors.Length]);
        used.Add(fallback);
        return fallback;
    }

    private static double Maximum(IEnumerable<double> values)
    {
        var max = values.DefaultIfEmpty(0).Max();
        return max <= 0 ? 1 : max * 1.05;
    }

    private static DateTime Local(DateTime utc) => DateTime.SpecifyKind(utc, DateTimeKind.Utc).ToLocalTime();
}
