namespace De.Hochstaetter.Fronius.Contracts;

/// <summary>
///     The three things Awattar (tado° Energy) answers: the day-ahead market prices, the sun and wind production
///     of the price zone, and the components of the end user tariff for a postal code. The first two need no
///     token; the third wants the bearer from <see cref="EnergyDataSettings" />.
/// </summary>
/// <remarks>
///     Awattar allows about a hundred requests a day, so nothing here is called on a whim: the collector asks
///     only in the windows where new data is expected, and yesterday and older comes from the history database.
/// </remarks>
public interface IAwattarClient
{
    /// <summary>The market prices of every slot that starts in <paramref name="fromUtc" /> to <paramref name="toUtc" />.</summary>
    Task<IReadOnlyList<EnergyPricePoint>> GetMarketPricesAsync(AwattarCountry region, DateTime fromUtc, DateTime toUtc, CancellationToken token = default);

    /// <summary>The sun and wind production, measured or forecast, of every slot in the span.</summary>
    Task<IReadOnlyList<GridProductionPoint>> GetProductionsAsync(AwattarCountry region, DateTime fromUtc, DateTime toUtc, CancellationToken token = default);

    /// <summary>The components of the tariff on <paramref name="day" />, without Awattar's own service fee.</summary>
    Task<IReadOnlyList<EnergyPriceComponent>> GetPriceComponentsAsync(EnergyDataSettings settings, DateOnly day, CancellationToken token = default);
}
