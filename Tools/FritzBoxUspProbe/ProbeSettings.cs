namespace De.Hochstaetter.FritzBoxUspProbe;

/// <summary>
/// What probe.json holds. Passwords are never in the file: they come from environment variables or are asked for.
/// </summary>
internal sealed class ProbeSettings
{
    public BoxSettings Box { get; set; } = new();

    public BrokerSettings Broker { get; set; } = new();

    public ControllerSettings Controller { get; set; } = new();

    public string OutputDirectory { get; set; } = "probe-output";

    public static ProbeSettings Load(string path) => File.Exists(path)
        ? JsonSerializer.Deserialize<ProbeSettings>(File.ReadAllText(path), new JsonSerializerOptions { ReadCommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true }) ??
          throw new InvalidDataException($"{path} is empty")
        : throw new FileNotFoundException($"{path} not found. Copy probe.sample.json to probe.json and adjust it.", path);
}

internal sealed class BoxSettings
{
    /// <summary>The box's TR-064 root.</summary>
    public string Url { get; set; } = "http://192.168.44.11:49000";

    /// <summary>A box user with the right "FRITZ!Box Einstellungen". Password: FRITZBOX_PASSWORD.</summary>
    public string UserName { get; set; } = string.Empty;
}

internal sealed class BrokerSettings
{
    /// <summary>The broker as both the probe and the box reach it.</summary>
    public string Host { get; set; } = "192.168.44.50";

    public int Port { get; set; } = 1883;

    public bool UseTls { get; set; }

    /// <summary>The broker user of the probe. Password: MQTT_PASSWORD.</summary>
    public string UserName { get; set; } = "usp-probe";
}

internal sealed class ControllerSettings
{
    public string EndpointId { get; set; } = "proto::usp-probe";

    /// <summary>The box's <c>MQTTControllerTopic</c>. The probe listens there and gives it as its "reply to" topic.</summary>
    public string MqttControllerTopic { get; set; } = "usp/controller/usp-probe";

    /// <summary>The box's <c>MQTTResponseTopic</c>.</summary>
    public string MqttResponseTopic { get; set; } = "usp/agent/fritzbox-neufahrn";

    /// <summary>The broker user of the box. Password: MQTT_BOX_PASSWORD.</summary>
    public string BoxUserName { get; set; } = "fritzbox-neufahrn";

    /// <summary>Comma separated: Smarthome, Mesh, Internet, System, Controller, WiFi, VoIP.</summary>
    public string AccessRights { get; set; } = "Smarthome, Internet";

    /// <summary>
    /// Everything the probe listens to besides its own topic, so that it sees what the box publishes where. AVM's
    /// documentation leaves open which of the two topics is which.
    /// </summary>
    public string ListenFilter { get; set; } = "usp/#";
}
