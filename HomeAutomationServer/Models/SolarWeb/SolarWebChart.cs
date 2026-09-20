namespace De.Hochstaetter.HomeAutomationServer.Models.SolarWeb;

/// <summary>
///     One chart as Solar.web's <c>GetChartNew</c> answers it, reduced to what a chart needs: the series with their
///     points. The colours, the axis layout and the tooltip formats of the answer are Highcharts configuration and
///     are not kept.
/// </summary>
/// <remarks>
///     <para>
///         The times are what Solar.web sends, milliseconds since the epoch turned into UTC. In a <see cref="SolarWebInterval.Day" />
///         chart they are real instants, five minutes apart, and the day starts at local midnight of the PV system
///         (22:00 UTC in summer). In the other charts they are midnight UTC of the day, the first of the month or
///         the year a column stands for, whatever the system's time zone - a date written as an instant. Read them
///         as dates there, not as times.
///     </para>
///     <para>
///         The series names are in the language Solar.web chose for the request; <see cref="SolarWebSeries.Id" />
///         is the stable key, <c>FromGenToGrid</c> or <c>StateOfCharge</c>, say.
///     </para>
/// </remarks>
public sealed class SolarWebChart
{
    public string PvSystemId { get; set; } = string.Empty;

    public SolarWebInterval Interval { get; set; }

    public SolarWebView View { get; set; }

    /// <summary>The first day of the span, as <see cref="SolarWebPeriod.Normalize" /> names it.</summary>
    public DateOnly Period { get; set; }

    /// <summary>When Solar.web answered this, UTC.</summary>
    public DateTime FetchedUtc { get; set; }

    /// <summary>True where Solar.web says the view needs a Premium subscription the account does not have.</summary>
    public bool IsPremiumFeature { get; set; }

    /// <summary>Solar.web's caption of the span, <c>19.09.2026</c>, <c>September 2026</c>, <c>2026</c> or <c>Gesamt</c>.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Solar.web's total over the span as it prints it, <c>72,78 kWh</c> or <c>116,39 EUR</c>.</summary>
    public string SumValue { get; set; } = string.Empty;

    public List<SolarWebSeries> Series { get; set; } = [];
}

/// <summary>One series of a chart: a stacked column or area, a line, or the battery states as bubbles.</summary>
public sealed class SolarWebSeries
{
    /// <summary>Solar.web's id of the series, the same in every language: <c>FromGenToGrid</c>, <c>ToConsumer</c>, <c>Saving</c>.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>The legend text in the language of the answer.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>The axis the series is drawn on, which is its unit: <c>W</c>, <c>%</c>, <c>kWh</c>, <c>MWh</c>, <c>EUR</c>.</summary>
    public string Unit { get; set; } = string.Empty;

    /// <summary>Highcharts' series type: <c>column</c>, <c>areaspline</c>, <c>spline</c>, <c>bubble</c>.</summary>
    public string ChartType { get; set; } = string.Empty;

    public List<SolarWebPoint> Points { get; set; } = [];
}

/// <summary>
///     One point: a time and a value, and for the battery state series a text as well (<c>Normalbetrieb</c>).
///     Solar.web sends <c>[time, value]</c> or <c>[time, value, text]</c>.
/// </summary>
public sealed class SolarWebPoint
{
    /// <summary>See the remarks on <see cref="SolarWebChart" /> for what this means per interval.</summary>
    public DateTime TimeUtc { get; set; }

    public double? Value { get; set; }

    public string? Text { get; set; }
}
