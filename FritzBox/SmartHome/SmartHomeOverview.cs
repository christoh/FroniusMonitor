namespace De.Hochstaetter.FritzBox.SmartHome;

// The JSON of the Smart Home REST API (FRITZ!OS 8.20 and later, API version 0.9.5), as far as this library reads or
// writes it. Fields the API names "UID" carry an explicit name; everything else follows the camel case of the API.
// What a unit cannot report (a device out of radio range, a lamp whose level is unknown) is missing from the JSON, so
// every value is nullable. The same interface classes serve for PUT, where only the fields that are set are written.

/// <summary><c>GET smarthome/overview</c>: everything the box knows about its smart home in one answer.</summary>
public sealed class SmartHomeOverview
{
    public List<SmartHomeDevice> Devices { get; init; } = [];
    public List<SmartHomeUnit> Units { get; init; } = [];
    public List<SmartHomeTemplate> Templates { get; init; } = [];
    public List<SmartHomeTrigger> Triggers { get; init; } = [];
}

/// <summary>A physical device. What it can do is in its <see cref="UnitUids">units</see>.</summary>
public sealed class SmartHomeDevice
{
    [JsonPropertyName("UID")] public string Uid { get; init; } = string.Empty;
    public string Ain { get; init; } = string.Empty;
    public string? Name { get; init; }
    public string? ProductName { get; init; }
    public string? Manufacturer { get; init; }
    public string? FirmwareVersion { get; init; }

    /// <summary><c>socket</c>, <c>lamp</c>, <c>thermostat</c>, <c>sensor</c>, <c>blind</c>, <c>control</c> or <c>other</c>.</summary>
    public string? ProductCategory { get; init; }

    public bool IsConnected { get; init; }
    public bool IsZigbeeDevice { get; init; }
    public List<string> UnitUids { get; init; } = [];
}

/// <summary>
/// A function of a device that can be read or controlled. An outlet is a single unit with the UID of its device, a
/// lamp has a unit of its own (<c>&lt;device UID&gt;-1</c>).
/// </summary>
public sealed class SmartHomeUnit
{
    [JsonPropertyName("UID")] public string Uid { get; init; } = string.Empty;
    public string Ain { get; init; } = string.Empty;
    public string? Name { get; init; }
    public string? DeviceUid { get; init; }

    /// <summary><c>avmPlugSocket</c>, <c>dimmableColorBulb</c> and many more; it tells which interfaces to expect.</summary>
    public string? UnitType { get; init; }

    public bool IsConnected { get; init; }
    public bool IsGroupUnit { get; init; }
    public SmartHomeInterfaces Interfaces { get; init; } = new();
}

public sealed class SmartHomeInterfaces
{
    public SmartHomeOnOff? OnOffInterface { get; init; }
    public SmartHomeLevelControl? LevelControlInterface { get; init; }
    public SmartHomeColorControl? ColorControlInterface { get; init; }
    public SmartHomeMultimeter? MultimeterInterface { get; init; }
    public SmartHomeTemperature? TemperatureInterface { get; init; }
}

public abstract class SmartHomeInterface
{
    /// <summary><c>valid</c>, <c>unknown</c> or <c>notConnected</c>. Read only.</summary>
    public string? State { get; init; }

    [JsonIgnore] public bool IsValid => State == "valid";
}

public sealed class SmartHomeOnOff : SmartHomeInterface
{
    public bool? Active { get; init; }

    /// <summary>Switching at the device itself is locked. Outlets only.</summary>
    public bool? IsLockedDeviceLocal { get; init; }

    /// <summary>Switching through the app, the web interface or this API is locked. Outlets only.</summary>
    public bool? IsLockedDeviceApi { get; init; }
}

public sealed class SmartHomeLevelControl : SmartHomeInterface
{
    /// <summary>0..100.</summary>
    public int? Level { get; init; }
}

public sealed class SmartHomeColorControl : SmartHomeInterface
{
    public const string HueSaturation = "hueSaturation";
    public const string Temperature = "temperature";

    /// <summary><see cref="HueSaturation"/>, <see cref="Temperature"/> or <c>unknown</c>.</summary>
    public string? CurrentColorMode { get; init; }

    public List<string>? SupportedColorModes { get; init; }
    public SmartHomeHsColor? HsColor { get; init; }

    /// <summary>Kelvin, 2700..6500 for AVM's lamps.</summary>
    public int? ColorTemperature { get; init; }
}

public sealed class SmartHomeHsColor
{
    /// <summary>0..359 degrees.</summary>
    public int Hue { get; init; }

    /// <summary>0..255.</summary>
    public int Saturation { get; init; }
}

public sealed class SmartHomeMultimeter : SmartHomeInterface
{
    /// <summary>mW, in steps of 10 mW.</summary>
    public long? Power { get; init; }

    /// <summary>mV.</summary>
    public long? Voltage { get; init; }

    /// <summary>mA.</summary>
    public long? Current { get; init; }

    /// <summary>Wh since the statistics were last reset.</summary>
    public long? Energy { get; init; }
}

public sealed class SmartHomeTemperature : SmartHomeInterface
{
    /// <summary>
    /// °C in steps of 0.1, as the device measures it. A configured offset is not in it, and not in the overview
    /// either (only in <c>smarthome/configuration/units/{UID}</c>).
    /// </summary>
    public double? Celsius { get; init; }
}

/// <summary>A template or a scenario (a template of templates) the box can apply.</summary>
public sealed class SmartHomeTemplate
{
    [JsonPropertyName("UID")] public string Uid { get; init; } = string.Empty;
    public string? Name { get; init; }
}

/// <summary>A routine: when a unit reports a value, the box applies a template.</summary>
public sealed class SmartHomeTrigger
{
    [JsonPropertyName("UID")] public string Uid { get; init; } = string.Empty;
    public string? Name { get; init; }
    public bool Enabled { get; init; }
}
