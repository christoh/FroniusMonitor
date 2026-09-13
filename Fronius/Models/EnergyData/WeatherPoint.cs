namespace De.Hochstaetter.Fronius.Models.EnergyData;

/// <summary>
///     The weather of one hour at one station of the German weather service (DWD): measured where the hour is over,
///     forecast where it is not. Every value may be missing - a station does not measure everything, and a forecast
///     element is not issued for every station.
/// </summary>
/// <remarks>
///     A quantity that is a mean over the last hour - the global radiation - is filed under the hour it is the mean
///     of, so the value the DWD reports at 15:00 for the hour before is the value of the slot starting at 14:00. An
///     instantaneous quantity - wind speed, temperature - is the value at the start of the slot. So the radiation of
///     a slot and the wind of a slot are read at different moments; that is what lines up a bar of radiation with
///     the hour it belongs to.
/// </remarks>
public sealed class WeatherPoint
{
    /// <summary>Start of the hour, UTC.</summary>
    public DateTime Time { get; set; }

    /// <summary>True for a forecast, false for a measurement.</summary>
    public bool IsForecast { get; set; }

    /// <summary>Mean global horizontal irradiance over the hour, W/m².</summary>
    public double? GlobalRadiationWattsPerSquareMeter { get; set; }

    /// <summary>Wind speed 10 m above ground, m/s.</summary>
    public double? WindSpeedMetersPerSecond { get; set; }

    /// <summary>Air temperature 2 m above ground, °C.</summary>
    public double? TemperatureCelsius { get; set; }

    /// <summary>Total cloud cover, 0 to 100 %.</summary>
    public double? CloudCoverPercent { get; set; }

    public override string ToString() => FormattableString.Invariant($"{Time:yyyy-MM-dd HH:mm}Z {(IsForecast ? "forecast" : "measured")}: {GlobalRadiationWattsPerSquareMeter:N0} W/m², {WindSpeedMetersPerSecond:N1} m/s, {TemperatureCelsius:N1} °C");
}
