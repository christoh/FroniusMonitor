namespace De.Hochstaetter.HomeAutomationServer.Services.EnergyData;

/// <summary>
///     Reads the two DWD open data files of a station. The parsing is in <see cref="DwdMosmixParser" /> and
///     <see cref="DwdObservationParser" />, so it can be tested on a file without a network.
/// </summary>
public sealed class DwdWeatherClient(ILogger<DwdWeatherClient> logger) : IDwdWeatherClient, IDisposable
{
    private const string OpenData = "https://opendata.dwd.de/weather/";

    private readonly HttpClient client = new() { Timeout = TimeSpan.FromSeconds(30) };

    public async Task<DwdForecast> GetForecastAsync(string stationId, CancellationToken token = default)
    {
        var id = Uri.EscapeDataString(stationId);
        var uri = $"{OpenData}local_forecasts/mos/MOSMIX_L/single_stations/{id}/kml/MOSMIX_L_LATEST_{id}.kmz";

        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug("DWD request: {Uri}", uri);
        }

        using var response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(token).ConfigureAwait(false);
        var forecast = DwdMosmixParser.Parse(stream);
        forecast.StationId = stationId;
        return forecast;
    }

    public async Task<IReadOnlyList<WeatherPoint>> GetObservationsAsync(string stationId, CancellationToken token = default)
    {
        var uri = $"{OpenData}weather_reports/poi/{Uri.EscapeDataString(stationId)}-BEOB.csv";

        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug("DWD request: {Uri}", uri);
        }

        using var response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            // Not every MOSMIX station measures, and the ones that do not have no file. That is how the station
            // is, not an error, and the forecast is still worth having.
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("DWD station {StationId} has no measurement feed", stationId);
            }

            return [];
        }

        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(token).ConfigureAwait(false);
        // The file says nothing about its encoding; the DWD writes it as Latin 1 (the units say "Grad C").
        using var reader = new StreamReader(stream, Encoding.Latin1);
        return DwdObservationParser.Parse(reader);
    }

    public void Dispose() => client.Dispose();
}
