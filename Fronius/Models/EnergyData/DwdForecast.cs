namespace De.Hochstaetter.Fronius.Models.EnergyData;

/// <summary>One MOSMIX forecast of one station, as <see cref="IDwdWeatherClient.GetForecastAsync" /> answers it.</summary>
public sealed class DwdForecast
{
    public string StationId { get; set; } = string.Empty;

    /// <summary>The station as the DWD names it, for example <c>MUENCHEN-FL.</c>; null where the file did not say.</summary>
    public string? StationName { get; set; }

    /// <summary>When the DWD issued the forecast, UTC.</summary>
    public DateTime IssueTime { get; set; }

    /// <summary>One point per forecast hour, in order of time, all with <see cref="WeatherPoint.IsForecast" /> set.</summary>
    public IReadOnlyList<WeatherPoint> Points { get; set; } = [];
}
