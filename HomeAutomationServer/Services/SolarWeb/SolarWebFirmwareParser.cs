using System.Text.Json;
using System.Text.Json.Nodes;

namespace De.Hochstaetter.HomeAutomationServer.Services.SolarWeb;

/// <summary>
///     Reads the answer of <c>/Firmware/GetComponentUpdateInfos</c>: <c>data.UpdateInfos[]</c>, each with the
///     component's ids, its <c>AvailableUpdate</c> (versions, changelog, flags) and an <c>InfoDescription</c>.
///     Pascal case names, unlike the chart answer.
/// </summary>
public static class SolarWebFirmwareParser
{
    public static SolarWebFirmwareStatus Parse(string json, string pvSystemId, DateTime fetchedUtc)
    {
        var root = JsonNode.Parse(json) as JsonObject ?? throw new InvalidDataException("Solar.web answered no JSON object");
        var status = new SolarWebFirmwareStatus { PvSystemId = pvSystemId, Timestamp = fetchedUtc };

        if (root["data"]?["UpdateInfos"] is JsonArray infos)
        {
            foreach (var info in infos.OfType<JsonObject>())
            {
                var update = info["AvailableUpdate"] as JsonObject;
                var description = info["InfoDescription"] as JsonObject;

                status.Components.Add(new SolarWebFirmwareComponent
                {
                    DataSourceId = String(info, "DataSourceId") ?? string.Empty,
                    ComponentId = info["ComponentId"] is JsonValue id && id.GetValueKind() == JsonValueKind.Number ? id.GetValue<long>() : 0,
                    UpdateFamily = String(info, "UpdateFamily") ?? string.Empty,
                    IsOnline = Bool(info, "IsOnline"),
                    InstalledVersion = SolarWebVersion.Parse(String(update, "InstalledVersion")),
                    UpdateVersion = SolarWebVersion.Parse(String(update, "UpdateVersion")),
                    UpdateVersionPrefix = String(update, "UpdateVersionPrefix") ?? string.Empty,
                    ChangelogUrl = String(update, "ChangelogUrl"),
                    NewerVersionAvailable = Bool(update, "NewerVersionAvailable"),
                    IsUpdateAllowed = Bool(update, "IsUpdateAllowed"),
                    LastUpdate = String(update, "LastUpdate") is { } last && DateTime.TryParse(last, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var lastUpdate) ? lastUpdate : null,
                    UpdateRecommendationInfo = String(info, "UpdateRecommendationInfo") ?? string.Empty,
                    UpdateStatus = String(info, "UpdateStatus") ?? string.Empty,
                    InfoText = String(description, "Text") ?? string.Empty,
                    InfoState = String(description, "State") ?? string.Empty,
                    InfoUrl = String(description, "Url"),
                });
            }
        }

        return status;
    }

    private static string? String(JsonObject? node, string name) => node?[name] is JsonValue value && value.GetValueKind() == JsonValueKind.String ? value.GetValue<string>() : null;

    private static bool Bool(JsonObject? node, string name) => node?[name] is JsonValue value && value.GetValueKind() is JsonValueKind.True or JsonValueKind.False && value.GetValue<bool>();
}
