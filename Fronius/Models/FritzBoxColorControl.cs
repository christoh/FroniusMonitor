namespace De.Hochstaetter.Fronius.Models;

[Flags]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum FritzBoxColorMode : uint
{
    [JsonStringEnumMemberName("none")]
    None = 0,
    [JsonStringEnumMemberName("hsv")]
    Hsv = 1,
    [JsonStringEnumMemberName("temperature")]
    Temperature = 4,
}

[XmlType("colorcontrol")]
[SuppressMessage("ReSharper", "StringLiteralTypo")]
public class FritzBoxColorControl : BindableBase
{
    [XmlAttribute("supported_modes")]
    [JsonIgnore]
    public string? SupportedModesString
    {
        get => FritzBoxDevice.GetStringValue((double?)SupportedModes,1);
        set => SupportedModes = (FritzBoxColorMode?)FritzBoxDevice.GetUintValue(value);
    }

    [XmlAttribute("current_mode")]
    [JsonIgnore]
    public string? CurrentModeString
    {
        get => FritzBoxDevice.GetStringValue((double?)SupportedModes,1);
        set => CurrentMode = (FritzBoxColorMode?)FritzBoxDevice.GetUintValue(value);
    }

    [XmlIgnore]
    public FritzBoxColorMode? SupportedModes
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(SupportedModesString)));
    }

    [XmlIgnore]
    public FritzBoxColorMode? CurrentMode
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(CurrentModeString)));
    }

    [XmlIgnore]
    public double? HueDegrees
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(HueDegreesString)));
    }

    [XmlElement("hue")]
    public string? HueDegreesString
    {
        get => FritzBoxDevice.GetStringValue(HueDegrees, 1);
        set => HueDegrees = FritzBoxDevice.GetDoubleValue(value, 1);
    }

    [XmlIgnore]
    public double? SaturationAbsolute
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(SaturationString)));
    }

    [XmlElement("saturation")]
    public string? SaturationString
    {
        get => FritzBoxDevice.GetStringValue(SaturationAbsolute, 1);
        set => SaturationAbsolute = FritzBoxDevice.GetDoubleValue(value, 1);
    }

    [XmlIgnore]
    public double? UnmappedHueDegrees
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(UnmappedHueDegreesString)));
    }

    [XmlElement("unmapped_hue")]
    public string? UnmappedHueDegreesString
    {
        get => FritzBoxDevice.GetStringValue(UnmappedHueDegrees, 1);
        set => UnmappedHueDegrees = FritzBoxDevice.GetDoubleValue(value, 1);
    }

    [XmlIgnore]
    public double? UnmappedSaturation
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(UnmappedSaturationString)));
    }

    [XmlElement("unmapped_saturation")]
    public string? UnmappedSaturationString
    {
        get => FritzBoxDevice.GetStringValue(UnmappedSaturation, 1);
        set => UnmappedSaturation = FritzBoxDevice.GetDoubleValue(value, 1);
    }

    [XmlIgnore]
    public double? TemperatureKelvin
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(TemperatureKelvinString)));
    }

    [XmlElement("temperature")]
    public string? TemperatureKelvinString
    {
        get => FritzBoxDevice.GetStringValue(TemperatureKelvin, 1);
        set => TemperatureKelvin = FritzBoxDevice.GetDoubleValue(value, 1);
    }
}
