namespace De.Hochstaetter.HomeAutomationServer.Models.Settings;

/// <summary>
///     What <see cref="Services.DataCollectors.EnergyDataCollector" /> is configured with: the sources and how often
///     each of them is asked. The windows are local time of <see cref="EnergyDataSettings.TimeZoneId" />.
/// </summary>
public class EnergyDataCollectorParameters
{
    /// <summary>The sources; <see langword="null" /> means nothing is collected.</summary>
    public EnergyDataSettings? Settings { get; set; }

    /// <summary>
    ///     When Awattar is asked for tomorrow's prices, and how often, until it has them. EPEX publishes the
    ///     day-ahead result around 13:00 and Awattar has it shortly after; it is asked every quarter of an hour
    ///     from noon until three, and left alone as soon as tomorrow is complete.
    /// </summary>
    public TimeOnly PriceWindowStart { get; set; } = new(12, 0);

    public TimeOnly PriceWindowEnd { get; set; } = new(15, 0);

    public TimeSpan PriceRetryInterval { get; set; } = TimeSpan.FromMinutes(15);

    /// <summary>
    ///     When tomorrow's sun and wind forecast is read again. Awattar updates it in the evening, so it is read
    ///     every quarter of an hour between six and eight. The first read comes with the prices.
    /// </summary>
    public TimeOnly ProductionWindowStart { get; set; } = new(18, 0);

    public TimeOnly ProductionWindowEnd { get; set; } = new(20, 0);

    /// <summary>How often the DWD forecast and the DWD measurements are read.</summary>
    public TimeSpan WeatherRefreshRate { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>How often the collector looks at the clock to decide what is due.</summary>
    public TimeSpan TickInterval { get; set; } = TimeSpan.FromMinutes(1);
}
