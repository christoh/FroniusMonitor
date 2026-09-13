namespace De.Hochstaetter.Fronius.Services.EnergyData;

/// <summary>
///     Reads the hourly measurements of a DWD station (<c>weather_reports/poi/&lt;station&gt;-BEOB.csv</c>): semicolon
///     separated, decimal comma, <c>---</c> for a missing value, the first line the column names, the second the
///     units, then one row per hour with the date as <c>dd.MM.yy</c> and the time in UTC, newest first.
/// </summary>
/// <remarks>
///     The columns are found by name, not by position - the DWD has added columns before. The units row is read
///     for the wind, which the file gives in km/h while the forecast gives m/s; both end up as m/s.
/// </remarks>
public static class DwdObservationParser
{
    private const string RadiationColumn = "global_radiation_last_hour";
    private const string WindColumn = "mean_wind_speed_during last_10_min_at_10_meters_above_ground";
    private const string TemperatureColumn = "dry_bulb_temperature_at_2_meter_above_ground";
    private const string CloudColumn = "cloud_cover_total";

    public static IReadOnlyList<WeatherPoint> Parse(TextReader reader)
    {
        var names = reader.ReadLine()?.Split(';') ?? throw new InvalidDataException("The observation file is empty");
        var units = reader.ReadLine()?.Split(';') ?? throw new InvalidDataException("The observation file has no units row");

        var radiation = Array.IndexOf(names, RadiationColumn);
        var wind = Array.IndexOf(names, WindColumn);
        var temperature = Array.IndexOf(names, TemperatureColumn);
        var cloud = Array.IndexOf(names, CloudColumn);
        var windFactor = wind >= 0 && wind < units.Length && units[wind].Contains("km", StringComparison.OrdinalIgnoreCase) ? 1 / 3.6 : 1;

        var points = new SortedDictionary<DateTime, WeatherPoint>();

        while (reader.ReadLine() is { } line)
        {
            var fields = line.Split(';');

            if (fields.Length < 2 || !TryParseTime(fields[0], fields[1], out var time))
            {
                continue;
            }

            // The feed is hourly for most stations; a ten minute row of the others has no hourly mean in it.
            if (time.Minute != 0)
            {
                continue;
            }

            if (Field(fields, radiation) is { } radiationValue)
            {
                Point(time.AddHours(-1)).GlobalRadiationWattsPerSquareMeter = radiationValue;
            }

            if (Field(fields, wind) is { } windValue)
            {
                Point(time).WindSpeedMetersPerSecond = windValue * windFactor;
            }

            if (Field(fields, temperature) is { } temperatureValue)
            {
                Point(time).TemperatureCelsius = temperatureValue;
            }

            if (Field(fields, cloud) is { } cloudValue)
            {
                Point(time).CloudCoverPercent = cloudValue;
            }
        }

        return [.. points.Values];

        WeatherPoint Point(DateTime slot)
        {
            if (!points.TryGetValue(slot, out var point))
            {
                point = new WeatherPoint { Time = slot, IsForecast = false };
                points[slot] = point;
            }

            return point;
        }
    }

    private static double? Field(string[] fields, int index)
    {
        if (index < 0 || index >= fields.Length)
        {
            return null;
        }

        var text = fields[index].Trim().Replace(',', '.');
        return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ? value : null;
    }

    private static bool TryParseTime(string date, string time, out DateTime result)
    {
        return DateTime.TryParseExact($"{date.Trim()} {time.Trim()}", "dd.MM.yy HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out result);
    }
}
