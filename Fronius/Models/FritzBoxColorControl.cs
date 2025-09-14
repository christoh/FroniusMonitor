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
public partial class FritzBoxColorControl : BindableBase
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

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SupportedModesString))]
    [XmlIgnore]
    public partial FritzBoxColorMode? SupportedModes { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CurrentModeString))]
    [XmlIgnore]
    public partial FritzBoxColorMode? CurrentMode { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HueDegreesString))]
    [XmlIgnore]
    public partial double? HueDegrees { get; set; }

    [XmlElement("hue"), JsonIgnore]
    public string? HueDegreesString
    {
        get => FritzBoxDevice.GetStringValue(HueDegrees, 1);
        set => HueDegrees = FritzBoxDevice.GetDoubleValue(value, 1);
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SaturationString))]
    [XmlIgnore]
    public partial double? SaturationAbsolute { get; set; }

    [XmlElement("saturation"), JsonIgnore]
    public string? SaturationString
    {
        get => FritzBoxDevice.GetStringValue(SaturationAbsolute, 1);
        set => SaturationAbsolute = FritzBoxDevice.GetDoubleValue(value, 1);
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(UnmappedHueDegreesString))]
    [XmlIgnore]
    public partial double? UnmappedHueDegrees { get; set; }

    [XmlElement("unmapped_hue"), JsonIgnore]
    public string? UnmappedHueDegreesString
    {
        get => FritzBoxDevice.GetStringValue(UnmappedHueDegrees, 1);
        set => UnmappedHueDegrees = FritzBoxDevice.GetDoubleValue(value, 1);
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(UnmappedSaturationString))]
    [XmlIgnore]
    public partial double? UnmappedSaturation { get; set; }

    [XmlElement("unmapped_saturation"), JsonIgnore]
    public string? UnmappedSaturationString
    {
        get => FritzBoxDevice.GetStringValue(UnmappedSaturation, 1);
        set => UnmappedSaturation = FritzBoxDevice.GetDoubleValue(value, 1);
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TemperatureKelvinString))]
    [XmlIgnore]
    public partial double? TemperatureKelvin { get; set; }

    [XmlElement("temperature"), JsonIgnore]
    public string? TemperatureKelvinString
    {
        get => FritzBoxDevice.GetStringValue(TemperatureKelvin, 1);
        set => TemperatureKelvin = FritzBoxDevice.GetDoubleValue(value, 1);
    }
}
