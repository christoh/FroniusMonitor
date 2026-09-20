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
    ///     Whether the period was over at <paramref name="nowUtc" /> for at least <paramref name="finalAfter" />, so
    ///     that its chart does not change any more. A day ends at local midnight of <paramref name="zone" />.
    /// </summary>
    public static bool IsFinal(SolarWebInterval interval, DateOnly period, DateTimeOffset nowUtc, TimeZoneInfo zone, TimeSpan finalAfter)
    {
        if (End(interval, period) is not { } end)
        {
            return false;
        }

        var endUtc = TimeZoneInfo.ConvertTimeToUtc(end.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified), zone);
        return nowUtc.UtcDateTime >= endUtc + finalAfter;
    }
}
