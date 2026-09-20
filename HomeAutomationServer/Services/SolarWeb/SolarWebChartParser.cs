using System.Text.Json;
using System.Text.Json.Nodes;

namespace De.Hochstaetter.HomeAutomationServer.Services.SolarWeb;

/// <summary>
///     Reads the answer of <c>/Chart/GetChartNew</c>. It is a Highcharts configuration - <c>settings.series[]</c>
///     with <c>id</c>, <c>name</c>, <c>type</c>, <c>yAxis</c> and <c>data</c> - wrapped in a few fields of its own:
///     <c>isPremiumFeature</c>, <c>title</c>, <c>sumValue</c>, <c>navOptions</c>. Only the series and those fields
///     are kept.
/// </summary>
/// <remarks>
///     A point is <c>[time, value]</c> or <c>[time, value, text]</c>; the time is milliseconds since the epoch, the
///     value a number or <see langword="null" />. A series without <c>data</c> is kept with no points, so that the
///     legend is complete.
/// </remarks>
public static class SolarWebChartParser
{
    public static SolarWebChart Parse(string json, string pvSystemId, SolarWebInterval interval, SolarWebView view, DateOnly period, DateTime fetchedUtc)
    {
        var root = JsonNode.Parse(json) as JsonObject ?? throw new InvalidDataException("Solar.web answered no JSON object");

        var chart = new SolarWebChart
        {
            PvSystemId = pvSystemId,
            Interval = interval,
            View = view,
            Period = period,
            FetchedUtc = fetchedUtc,
            IsPremiumFeature = root["isPremiumFeature"]?.GetValue<bool>() ?? false,
            Title = root["title"]?.GetValue<string>() ?? string.Empty,
            SumValue = root["sumValue"]?.GetValue<string>() ?? string.Empty,
        };

        if (root["settings"]?["series"] is JsonArray series)
        {
            foreach (var node in series.OfType<JsonObject>())
            {
                chart.Series.Add(new SolarWebSeries
                {
                    Id = node["id"]?.GetValue<string>() ?? string.Empty,
                    Name = node["name"]?.GetValue<string>() ?? string.Empty,
                    Unit = node["yAxis"]?.GetValue<string>() ?? string.Empty,
                    ChartType = node["type"]?.GetValue<string>() ?? string.Empty,
                    Points = node["data"] is JsonArray data ? data.OfType<JsonArray>().Select(Point).ToList() : [],
                });
            }
        }

        return chart;
    }

    private static SolarWebPoint Point(JsonArray point)
    {
        if (point.Count < 2 || point[0] is not JsonValue time)
        {
            throw new InvalidDataException($"Solar.web sent a point without a time: {point.ToJsonString()}");
        }

        return new SolarWebPoint
        {
            TimeUtc = DateTimeOffset.FromUnixTimeMilliseconds(time.GetValue<long>()).UtcDateTime,
            Value = point[1] is JsonValue value && value.GetValueKind() == JsonValueKind.Number ? value.GetValue<double>() : null,
            Text = point.Count > 2 && point[2] is JsonValue text && text.GetValueKind() == JsonValueKind.String ? text.GetValue<string>() : null,
        };
    }
}
