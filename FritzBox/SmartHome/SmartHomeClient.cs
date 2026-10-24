using De.Hochstaetter.FritzBox.Http;

namespace De.Hochstaetter.FritzBox.SmartHome;

/// <summary>
/// The Smart Home REST API of a box (FRITZ!OS 8.20 and later) at <c>api/v0/smarthome</c>. The box pushes nothing, so
/// reading means polling <see cref="GetOverviewAsync"/>. The statistics endpoints are left out on purpose: AVM warns
/// that each of their requests costs a radio request to the device.
/// </summary>
public sealed class SmartHomeClient(FritzBoxHttpClient http)
{
    internal static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public async Task<SmartHomeOverview> GetOverviewAsync(CancellationToken token = default) =>
        Parse(await http.GetRestAsync("smarthome/overview", token).ConfigureAwait(false));

    /// <summary>
    /// Sets what <paramref name="interfaces"/> holds on the unit <paramref name="unitUid"/>, for example
    /// <c>new SmartHomeInterfaces { OnOffInterface = new() { Active = true } }</c>. Interfaces and values left
    /// <see langword="null"/> stay as they are. Setting a level or a color does not switch a lamp on.
    /// </summary>
    public Task SetUnitAsync(string unitUid, SmartHomeInterfaces interfaces, CancellationToken token = default) =>
        http.SendRestAsync(HttpMethod.Put, $"smarthome/overview/units/{Uri.EscapeDataString(unitUid)}", JsonSerializer.Serialize(new { interfaces }, JsonOptions), token);

    /// <summary>Applies a template or a scenario, as its button in the web interface does.</summary>
    public Task ApplyTemplateAsync(string templateUid, CancellationToken token = default) =>
        http.SendRestAsync(HttpMethod.Post, $"smarthome/overview/templates/{Uri.EscapeDataString(templateUid)}", JsonSerializer.Serialize(new { triggerEvent = true }, JsonOptions), token);

    /// <summary>Switches a routine on or off.</summary>
    public Task SetTriggerEnabledAsync(string triggerUid, bool enabled, CancellationToken token = default) =>
        http.SendRestAsync(HttpMethod.Put, $"smarthome/overview/triggers/{Uri.EscapeDataString(triggerUid)}", JsonSerializer.Serialize(new { enabled }, JsonOptions), token);

    internal static SmartHomeOverview Parse(string json) =>
        JsonSerializer.Deserialize<SmartHomeOverview>(json, JsonOptions) ?? throw new InvalidDataException("smarthome/overview answered with null");
}
