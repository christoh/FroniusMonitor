namespace De.Hochstaetter.HomeAutomationServer.Models.SolarWeb;

/// <summary>
///     The span of time one Solar.web chart covers, named by its first day: the day itself, the first of the month,
///     the first of January, or <see cref="DateOnly.MinValue" /> for the whole history. That first day is the key
///     the chart is cached under, so two requests for different days of the same month find the same row.
/// </summary>
public static class SolarWebPeriod
{
    /// <summary>The first day of the period that contains <paramref name="date" />.</summary>
    public static DateOnly Normalize(SolarWebInterval interval, DateOnly date) => interval switch
    {
        SolarWebInterval.Day => date,
        SolarWebInterval.Month => new DateOnly(date.Year, date.Month, 1),
        SolarWebInterval.Year => new DateOnly(date.Year, 1, 1),
        SolarWebInterval.All => DateOnly.MinValue,
        _ => throw new ArgumentOutOfRangeException(nameof(interval), interval, null),
    };

    /// <summary>
    ///     The first day after the period, or <see langword="null" /> for the whole history, which never ends.
    ///     <paramref name="period" /> is a value <see cref="Normalize" /> returned.
    /// </summary>
    public static DateOnly? End(SolarWebInterval interval, DateOnly period) => interval switch
    {
        SolarWebInterval.Day => period.AddDays(1),
        SolarWebInterval.Month => period.AddMonths(1),
        SolarWebInterval.Year => period.AddYears(1),
        SolarWebInterval.All => null,
        _ => throw new ArgumentOutOfRangeException(nameof(interval), interval, null),
    };

    /// <summary>
    ///     The period before <paramref name="period" />: the day, the month or the year before it, or
    ///     <see langword="null" /> for the whole history, which has none.
    /// </summary>
    public static DateOnly? Previous(SolarWebInterval interval, DateOnly period) => interval switch
    {
        SolarWebInterval.Day => period.AddDays(-1),
        SolarWebInterval.Month => period.AddMonths(-1),
        SolarWebInterval.Year => period.AddYears(-1),
        SolarWebInterval.All => null,
        _ => throw new ArgumentOutOfRangeException(nameof(interval), interval, null),
    };

    /// <summary>
    ///     The instant the period was over, in UTC, or <see langword="null" /> for the whole history. A day ends at
    ///     local midnight of <paramref name="zone" />.
    /// </summary>
    public static DateTime? EndUtc(SolarWebInterval interval, DateOnly period, TimeZoneInfo zone) =>
        End(interval, period) is { } end ? TimeZoneInfo.ConvertTimeToUtc(end.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified), zone) : null;

    /// <summary>
    ///     Whether a day chart holds the whole day: Solar.web's day is 288 slots of five minutes, and the day is there
    ///     once every measured series has a value in the last of them, 23:55 local. Solar.web can be hours behind the
    ///     inverter, so a chart read after midnight may still stop short of that. The forecast does not count, its
    ///     points are always there; nor do the battery state bubbles, which are events and not measurements; nor a
    ///     series without a single value in the whole day - a Wattpilot the system does not have.
    /// </summary>
    /// <remarks>
    ///     Every measured series, not any one of them: the series of a day are computed from the same data, but
    ///     nothing says they are padded alike, and a day whose consumption stops at 23:10 while the production
    ///     stands at zero until midnight is not over. The developer's rule (2026-09-22): the period is not complete
    ///     until the 23:55 data has arrived, and it is read again until then.
    /// </remarks>
    /// <param name="chart">A <see cref="SolarWebInterval.Day" /> chart.</param>
    /// <param name="endUtc">The day's <see cref="EndUtc" />.</param>
    public static bool IsDayComplete(SolarWebChart chart, DateTime endUtc)
    {
        var lastSlot = endUtc.AddMinutes(-5);
        var measured = MeasuredSeriesWithValues(chart).ToList();
        return measured.Count > 0 && measured.All(s => s.Points.Any(p => p.Value != null && p.TimeUtc >= lastSlot && p.TimeUtc < endUtc));
    }

    /// <summary>
    ///     The latest instant before <paramref name="endUtc" /> at which every measured series of a day chart has a
    ///     value - where the day's data ends so far - or <see langword="null" /> where no series has any. For the
    ///     log, so that somebody can see how far Solar.web is behind.
    /// </summary>
    public static DateTime? LastValueUtc(SolarWebChart chart, DateTime endUtc)
    {
        var lastPerSeries = MeasuredSeriesWithValues(chart)
            .Select(s => s.Points.Where(p => p.Value != null && p.TimeUtc < endUtc).Select(p => (DateTime?)p.TimeUtc).Max())
            .Where(last => last != null)
            .ToList();

        return lastPerSeries.Count == 0 ? null : lastPerSeries.Min();
    }

    private static IEnumerable<SolarWebSeries> MeasuredSeriesWithValues(SolarWebChart chart) =>
        chart.Series.Where(s => !s.IsForecast && !s.IsBubble && s.Points.Any(p => p.Value != null));
}
