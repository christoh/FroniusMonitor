namespace De.Hochstaetter.Fronius.Contracts;

/// <summary>
///     The open data of the German weather service (DWD) for one station: the MOSMIX point forecast and the
///     hourly measurements of the last day. Both are plain files on <c>opendata.dwd.de</c>, no token, no limit
///     worth mentioning.
/// </summary>
public interface IDwdWeatherClient
{
    /// <summary>The latest MOSMIX_L forecast of the station: about ten days in hourly steps, issued every six hours.</summary>
    Task<DwdForecast> GetForecastAsync(string stationId, CancellationToken token = default);

    /// <summary>
    ///     The measurements of the station over the last day, one per hour. Not every station is in the measurement
    ///     feed; one that is not answers 404, which comes back as an empty list.
    /// </summary>
    Task<IReadOnlyList<WeatherPoint>> GetObservationsAsync(string stationId, CancellationToken token = default);
}
