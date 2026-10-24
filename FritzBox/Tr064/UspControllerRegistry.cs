namespace De.Hochstaetter.FritzBox.Tr064;

/// <summary>What a USP controller may do on the box. The names are the box's own.</summary>
[Flags]
public enum UspAccessRights
{
    None = 0,
    Smarthome = 1 << 0,
    Mesh = 1 << 1,
    Internet = 1 << 2,
    System = 1 << 3,
    Controller = 1 << 4,
    WiFi = 1 << 5,
    VoIP = 1 << 6,
}

/// <summary>A USP controller the box knows, as <c>X_AVM-DE_USPController</c> describes it.</summary>
public sealed record UspControllerEntry
{
    /// <summary>The position on the box. Only meaningful for an entry read from the box.</summary>
    public int Index { get; init; } = -1;

    public bool Enable { get; init; } = true;

    public required string EndpointId { get; init; }

    /// <summary><c>mqtt</c> or <c>websocket</c>.</summary>
    public string Mtp { get; init; } = "mqtt";

    /// <summary>The MQTT broker, or the controller's WebSocket server.</summary>
    public required string Hostname { get; init; }

    /// <summary>The WebSocket path. Not used with MQTT.</summary>
    public string Path { get; init; } = string.Empty;

    public int Port { get; init; } = 1883;

    public bool UseTls { get; init; }

    public string MqttControllerTopic { get; init; } = string.Empty;

    public string MqttResponseTopic { get; init; } = string.Empty;

    public string MqttClientId { get; init; } = string.Empty;

    /// <summary>The user the box logs in to the broker with.</summary>
    public string Username { get; init; } = string.Empty;

    public UspAccessRights AccessRights { get; init; }
}

/// <summary>
/// Lists, adds and removes the box's USP controllers through the TR-064 service <c>X_AVM-DE_USPController</c>.
/// </summary>
public sealed class UspControllerRegistry(Tr064Client client)
{
    private const string Service = "X_AVM-DE_USPController";
    private const string AddAction = "AddUSPController";

    private static readonly IReadOnlyList<(UspAccessRights Right, string Name)> rightNames =
    [
        (UspAccessRights.Smarthome, "Smarthome"),
        (UspAccessRights.Mesh, "Mesh"),
        (UspAccessRights.Internet, "Internet"),
        (UspAccessRights.System, "System"),
        (UspAccessRights.Controller, "Controller"),
        (UspAccessRights.WiFi, "WiFi"),
        (UspAccessRights.VoIP, "VoIP"),
    ];

    public async Task<IReadOnlyList<UspControllerEntry>> GetAllAsync(CancellationToken token = default)
    {
        var countResult = await client.InvokeAsync(Service, "GetUSPControllerNumberOfEntries", [], token).ConfigureAwait(false);
        var count = int.Parse(countResult["NewUSPControllerNumberOfEntries"], CultureInfo.InvariantCulture);
        var result = new List<UspControllerEntry>(count);

        for (var index = 0; index < count; index++)
        {
            var values = await client.InvokeAsync(Service, "GetUSPControllerByIndex", [new("NewIndex", index.ToString(CultureInfo.InvariantCulture))], token).ConfigureAwait(false);

            result.Add(new UspControllerEntry
            {
                Index = index,
                Enable = Tr064Soap.ParseBoolean(values.GetValueOrDefault("NewEnable", "0")),
                EndpointId = values.GetValueOrDefault("NewEndpointID", string.Empty),
                Mtp = values.GetValueOrDefault("NewMTP", string.Empty),
                Hostname = values.GetValueOrDefault("NewHostname", string.Empty),
                Path = values.GetValueOrDefault("NewPath", string.Empty),
                Port = int.TryParse(values.GetValueOrDefault("NewPort"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var port) ? port : 0,
                UseTls = Tr064Soap.ParseBoolean(values.GetValueOrDefault("NewUseTLS", "0")),
                MqttControllerTopic = values.GetValueOrDefault("NewMQTTControllerTopic", string.Empty),
                MqttResponseTopic = values.GetValueOrDefault("NewMQTTResponseTopic", string.Empty),
                MqttClientId = values.GetValueOrDefault("NewMQTTClientID", string.Empty),
                Username = values.GetValueOrDefault("NewUsername", string.Empty),
                AccessRights = rightNames
                    .Where(r => Tr064Soap.ParseBoolean(values.GetValueOrDefault("NewAccessRight" + r.Name, "0")))
                    .Aggregate(UspAccessRights.None, (rights, r) => rights | r.Right),
            });
        }

        return result;
    }

    public async Task<UspControllerEntry?> FindAsync(string endpointId, CancellationToken token = default) =>
        (await GetAllAsync(token).ConfigureAwait(false)).FirstOrDefault(c => c.EndpointId == endpointId);

    /// <summary>
    /// Adds <paramref name="entry"/> and returns its index. The box usually wants a second factor for this, see
    /// <see cref="Tr064Client.InvokeWithSecondFactorAsync"/>.
    /// </summary>
    /// <param name="password">The password the box logs in to the broker with.</param>
    /// <exception cref="NotSupportedException">The firmware cannot do MQTT for USP (FRITZ!OS before 8.01).</exception>
    public async Task<int> AddAsync
    (
        UspControllerEntry entry,
        string password,
        Func<string, CancellationToken, Task> onConfirmationRequired,
        TimeSpan confirmationTimeout,
        CancellationToken token = default
    )
    {
        var arguments = new Dictionary<string, string>
        {
            ["NewEnable"] = Tr064Soap.FormatBoolean(entry.Enable),
            ["NewEndpointID"] = entry.EndpointId,
            ["NewMTP"] = entry.Mtp,
            ["NewHostname"] = entry.Hostname,
            ["NewPath"] = entry.Path,
            ["NewPort"] = entry.Port.ToString(CultureInfo.InvariantCulture),
            ["NewUseTLS"] = Tr064Soap.FormatBoolean(entry.UseTls),
            ["NewMQTTControllerTopic"] = entry.MqttControllerTopic,
            ["NewMQTTResponseTopic"] = entry.MqttResponseTopic,
            ["NewMQTTClientID"] = entry.MqttClientId,
            ["NewUsername"] = entry.Username,
            ["NewPassword"] = password,
        };

        foreach (var (right, name) in rightNames)
        {
            arguments["NewAccessRight" + name] = Tr064Soap.FormatBoolean(entry.AccessRights.HasFlag(right));
        }

        // The box answers 402 to an argument it does not know, and older firmware knows fewer. Send what its SCPD lists.
        var actions = await client.GetActionsAsync(Service, token).ConfigureAwait(false);

        if (actions.TryGetValue(AddAction, out var declared))
        {
            var inputs = declared.Where(a => !a.IsOutput).Select(a => a.Name).ToHashSet();

            if (entry.Mtp == "mqtt" && !inputs.Contains("NewMQTTControllerTopic"))
            {
                throw new NotSupportedException("The firmware of this box cannot use MQTT for USP. It needs FRITZ!OS 8.01 or later.");
            }

            foreach (var name in arguments.Keys.Where(name => !inputs.Contains(name)).ToList())
            {
                arguments.Remove(name);
            }
        }

        Validate(arguments, await client.InvokeAsync(Service, "GetInfo", [], token).ConfigureAwait(false));
        var result = await client.InvokeWithSecondFactorAsync(Service, AddAction, arguments, onConfirmationRequired, confirmationTimeout, token).ConfigureAwait(false);
        return int.Parse(result["NewIndex"], CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Checks the arguments of <c>AddUSPController</c> against the lengths and characters the box allows, as its
    /// <c>GetInfo</c> names them (<c>NewMinCharsEndpointID</c>, <c>NewAllowedCharsEndpointID</c> and so on). The box
    /// itself only answers 600 "Argument Value Invalid" without saying which argument.
    /// </summary>
    /// <exception cref="ArgumentException">An argument breaks one of the limits. The message names it.</exception>
    internal static void Validate(IReadOnlyDictionary<string, string> arguments, IReadOnlyDictionary<string, string> limits)
    {
        foreach (var (name, value) in arguments)
        {
            var field = name["New".Length..];

            if (limits.TryGetValue("NewMinChars" + field, out var text) && int.TryParse(text, CultureInfo.InvariantCulture, out var minimum) && value.Length < minimum)
            {
                throw new ArgumentException($"The box wants at least {minimum} characters for {name}, it got {value.Length}");
            }

            if (limits.TryGetValue("NewMaxChars" + field, out text) && int.TryParse(text, CultureInfo.InvariantCulture, out var maximum) && value.Length > maximum)
            {
                throw new ArgumentException($"The box allows at most {maximum} characters for {name}, it got {value.Length}");
            }

            if (limits.TryGetValue("NewAllowedChars" + field, out var allowed) && allowed.Length > 0 && value.FirstOrDefault(c => !allowed.Contains(c)) is var forbidden and not '\0')
            {
                throw new ArgumentException($"The box does not allow '{forbidden}' in {name}. Allowed: {allowed}");
            }
        }
    }

    public Task DeleteAsync(int index, CancellationToken token = default) =>
        client.InvokeAsync(Service, "DeleteUSPControllerByIndex", [new("NewIndex", index.ToString(CultureInfo.InvariantCulture))], token);

    public Task SetEnabledAsync(int index, bool enable, CancellationToken token = default) =>
        client.InvokeAsync
        (
            Service,
            "SetUSPControllerEnableByIndex",
            [new("NewIndex", index.ToString(CultureInfo.InvariantCulture)), new("NewEnable", Tr064Soap.FormatBoolean(enable))],
            token
        );
}
