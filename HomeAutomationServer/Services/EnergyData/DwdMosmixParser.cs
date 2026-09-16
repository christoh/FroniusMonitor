using System.IO.Compression;
using System.Xml.Linq;

namespace De.Hochstaetter.HomeAutomationServer.Services.EnergyData;

/// <summary>
///     Reads a MOSMIX point forecast: a KMZ (a zip with one KML in it) whose <c>ExtendedData</c> lists the time
///     steps once and then, per element, a whitespace separated string with one value per time step. A missing
///     value is <c>-</c>.
/// </summary>
/// <remarks>
///     Which elements are read, and where their value is filed, is <see cref="WeatherPoint" />'s rule: <c>Rad1h</c>
///     is the irradiance of the hour <em>before</em> the time step and goes to the slot that starts an hour earlier;
///     <c>FF</c>, <c>TTT</c> and <c>N</c> are the state at the time step and go to the slot that starts there.
///     Units are converted here, so a <see cref="WeatherPoint" /> never carries a kJ or a Kelvin.
/// </remarks>
public static class DwdMosmixParser
{
    private static readonly XNamespace dwd = "https://opendata.dwd.de/weather/lib/pointforecast_dwd_extension_V1_0.xsd";
    private static readonly XNamespace kml = "http://www.opengis.net/kml/2.2";

    /// <summary>Reads a KMZ stream.</summary>
    public static DwdForecast Parse(Stream kmz)
    {
        using var archive = new ZipArchive(kmz, ZipArchiveMode.Read, leaveOpen: true);
        var entry = archive.Entries.FirstOrDefault(e => e.Name.EndsWith(".kml", StringComparison.OrdinalIgnoreCase)) ?? throw new InvalidDataException("The KMZ holds no KML");
        using var stream = entry.Open();
        return ParseKml(XDocument.Load(stream));
    }

    /// <summary>Reads the KML itself, for a test that has the XML at hand.</summary>
    public static DwdForecast ParseKml(XDocument document)
    {
        var root = document.Root ?? throw new InvalidDataException("Empty KML");
        var definition = root.Descendants(dwd + "ProductDefinition").FirstOrDefault() ?? throw new InvalidDataException("No product definition in the KML");
        var issueTime = ParseTime(definition.Element(dwd + "IssueTime")?.Value);
        var steps = definition.Descendants(dwd + "TimeStep").Select(s => ParseTime(s.Value)).ToList();

        if (steps.Count == 0)
        {
            throw new InvalidDataException("No time steps in the KML");
        }

        var placemark = root.Descendants(kml + "Placemark").FirstOrDefault() ?? throw new InvalidDataException("No station in the KML");
        var forecasts = placemark.Descendants(dwd + "Forecast").ToDictionary(f => (string?)f.Attribute(dwd + "elementName") ?? string.Empty, f => Values(f.Element(dwd + "value")?.Value, steps.Count));

        var points = new SortedDictionary<DateTime, WeatherPoint>();

        // A mean over the hour before the step: filed under the hour it is the mean of.
        Apply(forecasts, "Rad1h", steps, TimeSpan.FromHours(-1), (p, v) => p.GlobalRadiationWattsPerSquareMeter = v * 1000 / 3600);

        // The state at the step.
        Apply(forecasts, "FF", steps, TimeSpan.Zero, (p, v) => p.WindSpeedMetersPerSecond = v);
        Apply(forecasts, "TTT", steps, TimeSpan.Zero, (p, v) => p.TemperatureCelsius = v - 273.15);
        Apply(forecasts, "N", steps, TimeSpan.Zero, (p, v) => p.CloudCoverPercent = v);

        return new DwdForecast
        {
            StationId = placemark.Element(kml + "name")?.Value.Trim() ?? string.Empty,
            StationName = placemark.Element(kml + "description")?.Value.Trim(),
            IssueTime = issueTime,
            Points = [.. points.Values],
        };

        void Apply(Dictionary<string, double?[]> all, string element, List<DateTime> times, TimeSpan offset, Action<WeatherPoint, double> set)
        {
            if (!all.TryGetValue(element, out var values))
            {
                return;
            }

            for (var i = 0; i < times.Count && i < values.Length; i++)
            {
                if (values[i] is not { } value)
                {
                    continue;
                }

                var slot = times[i] + offset;

                if (!points.TryGetValue(slot, out var point))
                {
                    point = new WeatherPoint { Time = slot, IsForecast = true };
                    points[slot] = point;
                }

                set(point, value);
            }
        }
    }

    private static double?[] Values(string? text, int count)
    {
        var result = new double?[count];

        if (text == null)
        {
            return result;
        }

        var i = 0;

        foreach (var token in text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (i >= count)
            {
                break;
            }

            result[i++] = double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ? value : null;
        }

        return result;
    }

    private static DateTime ParseTime(string? text)
    {
        return DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var time)
            ? time
            : throw new InvalidDataException($"'{text}' is not a time");
    }
}
