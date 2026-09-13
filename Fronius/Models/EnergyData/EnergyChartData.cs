namespace De.Hochstaetter.Fronius.Models.EnergyData;

/// <summary>
///     Everything the price chart of one span of time shows: the market prices, the components that make them an end
///     user price, the sun and wind production of the grid, and the weather at the configured station.
/// </summary>
/// <remarks>
///     <para>
///         It is an <see cref="IHaveUniqueId" /> although it is no device, on purpose: the server publishes the
///         current span - today and tomorrow - to <see cref="IDataControlService" /> under <see cref="DeviceId" />,
///         and everything that already exists for a device then happens for it too. <c>SignalRDispatcher</c>
///         broadcasts it as an <c>EnergyChartData</c> hub message on every change, <c>HomeAutomationHub.OnConnectedAsync</c>
///         replays it to a client that connects, and the controller serves it over HTTP.
///     </para>
///     <para>
///         Any other span is built on request from the history database - see <c>IEnergyDataService.GetDayAsync</c>.
///     </para>
/// </remarks>
public sealed class EnergyChartData : IHaveUniqueId
{
    /// <summary>The id the current span is published under in <see cref="IDataControlService" />.</summary>
    public const string DeviceId = "EnergyChartData";

    /// <summary>Start of the span, UTC.</summary>
    public DateTime From { get; set; }

    /// <summary>End of the span, UTC.</summary>
    public DateTime To { get; set; }

    /// <summary>The price zone the market prices and the productions belong to.</summary>
    public AwattarCountry PriceRegion { get; set; } = AwattarCountry.GermanyLuxembourg;

    /// <summary>The VAT rate that applies to the market price itself, 0.19 for 19 %.</summary>
    public decimal MarketVatRate { get; set; }

    /// <summary>Market prices in order of time. Each slot says which source it came from.</summary>
    public List<EnergyPricePoint> Prices { get; set; } = [];

    /// <summary>The end user tariff on top of the market price, as it applies at <see cref="From" />.</summary>
    public List<EnergyPriceComponent> PriceComponents { get; set; } = [];

    /// <summary>Sun and wind production of the price zone, in order of time. Empty without Awattar.</summary>
    public List<GridProductionPoint> Productions { get; set; } = [];

    /// <summary>Hourly weather at <see cref="WeatherStationName" />, in order of time. Empty without a DWD station.</summary>
    public List<WeatherPoint> Weather { get; set; } = [];

    /// <summary>The DWD station the weather is for, as the DWD names it, or <see langword="null" /> without weather.</summary>
    public string? WeatherStationName { get; set; }

    /// <summary>When this was assembled, UTC.</summary>
    public DateTime Timestamp { get; set; }

    /// <summary>The source of the first price, or Awattar where there is none. A span may mix sources.</summary>
    public EnergyPriceSource PriceSource => Prices.Count == 0 ? EnergyPriceSource.Awattar : Prices[0].Source;

    /// <summary>True where at least one slot came from the Wattpilot rather than from Awattar.</summary>
    public bool HasWattPilotPrices => Prices.Any(p => p.Source == EnergyPriceSource.WattPilot);

    #region IHaveUniqueId

    /// <summary>Always true: the data exists as long as the server does, even when every list is empty.</summary>
    public bool IsPresent => true;

    public string Manufacturer => "Home Automation Server";

    public string Model => nameof(EnergyChartData);

    public string SerialNumber => "current";

    #endregion
}
