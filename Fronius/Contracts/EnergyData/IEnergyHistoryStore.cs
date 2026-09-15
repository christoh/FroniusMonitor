namespace De.Hochstaetter.Fronius.Contracts;

/// <summary>
///     The history of everything the price chart shows, kept by the server so that a day that is over is never
///     asked of Awattar again. The server's implementation is a SQLite file; the contract lives here so that the
///     collector and the tests do not depend on it.
/// </summary>
/// <remarks>
///     Every write is an upsert on the natural key of the row - the source and the start of a slot, the day and
///     the name of a component, the station and the hour - so writing the same answer twice changes nothing and
///     a forecast that is issued again replaces the one before it.
/// </remarks>
public interface IEnergyHistoryStore
{
    /// <summary>Creates the file and the tables where they do not exist. Called once by the collector before anything else.</summary>
    Task InitializeAsync(CancellationToken token = default);

    Task UpsertPricesAsync(IEnumerable<EnergyPricePoint> prices, CancellationToken token = default);

    /// <summary>The prices of <paramref name="source" /> whose slot starts in <paramref name="fromUtc" /> to <paramref name="toUtc" />, in order of time.</summary>
    Task<IReadOnlyList<EnergyPricePoint>> GetPricesAsync(EnergyPriceSource source, DateTime fromUtc, DateTime toUtc, CancellationToken token = default);

    Task UpsertPriceComponentsAsync(DateOnly day, IEnumerable<EnergyPriceComponent> components, CancellationToken token = default);

    Task<IReadOnlyList<EnergyPriceComponent>> GetPriceComponentsAsync(DateOnly day, CancellationToken token = default);

    Task UpsertProductionsAsync(AwattarCountry region, IEnumerable<GridProductionPoint> productions, CancellationToken token = default);

    Task<IReadOnlyList<GridProductionPoint>> GetProductionsAsync(AwattarCountry region, DateTime fromUtc, DateTime toUtc, CancellationToken token = default);

    Task UpsertWeatherAsync(string stationId, IEnumerable<WeatherPoint> points, CancellationToken token = default);

    /// <summary>
    ///     Measurements and forecasts of the station in the span, in order of time. An hour that has both comes back
    ///     twice; the caller decides which one it wants.
    /// </summary>
    Task<IReadOnlyList<WeatherPoint>> GetWeatherAsync(string stationId, DateTime fromUtc, DateTime toUtc, CancellationToken token = default);
}
