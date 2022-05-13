namespace De.Hochstaetter.Fronius.Models;

[Flags]
public enum FritzBoxColorMode : uint
{
    None = 0,
    Hsv = 1,
    Temperature = 4,
}

[XmlType("colorcontrol")]
[SuppressMessage("ReSharper", "StringLiteralTypo")]
public class FritzBoxColorControl : BindableBase
{
    [XmlAttribute("supported_modes")]
    public string? SupportedModesString
    {
        get => FritzBoxDevice.GetStringValue((double?)SupportedModes,1);
        set => SupportedModes = (FritzBoxColorMode?)FritzBoxDevice.GetUintValue(value);
    }

    [XmlAttribute("current_mode")]
    public string? CurrentModeString
    {
        get => FritzBoxDevice.GetStringValue((double?)SupportedModes,1);
        set => CurrentMode = (FritzBoxColorMode?)FritzBoxDevice.GetUintValue(value);
    }

    private FritzBoxColorMode? supportedModes;

    [XmlIgnore]
    public FritzBoxColorMode? SupportedModes
    {
        get => supportedModes;
        set => Set(ref supportedModes, value, () => NotifyOfPropertyChange(nameof(SupportedModesString)));
    }

    private FritzBoxColorMode? currentMode;

    [XmlIgnore]
    public FritzBoxColorMode? CurrentMode
    {
        get => currentMode;
        set => Set(ref currentMode, value, () => NotifyOfPropertyChange(nameof(CurrentModeString)));
    }

    private double? hueDegrees;

    [XmlIgnore]
    public double? HueDegrees
    {
        get => hueDegrees;
        set => Set(ref hueDegrees, value, () => NotifyOfPropertyChange(nameof(HueDegreesString)));
    }

    [XmlElement("hue")]
    public string? HueDegreesString
    {
        get => FritzBoxDevice.GetStringValue(HueDegrees, 1);
        set => HueDegrees = FritzBoxDevice.GetDoubleValue(value, 1);
    }

    private double? saturationAbsolute;

    [XmlIgnore]
    public double? SaturationAbsolute
    {
        get => saturationAbsolute;
        set => Set(ref saturationAbsolute, value, () => NotifyOfPropertyChange(nameof(SaturationString)));
    }

    [XmlElement("saturation")]
    public string? SaturationString
    {
        get => FritzBoxDevice.GetStringValue(SaturationAbsolute, 1);
        set => SaturationAbsolute = FritzBoxDevice.GetDoubleValue(value, 1);
    }

    private double? unmappedHueDegrees;

    [XmlIgnore]
    public double? UnmappedHueDegrees
    {
        get => unmappedHueDegrees;
        set => Set(ref unmappedHueDegrees, value, () => NotifyOfPropertyChange(nameof(UnmappedHueDegreesString)));
    }

    [XmlElement("unmapped_hue")]
    public string? UnmappedHueDegreesString
    {
        get => FritzBoxDevice.GetStringValue(UnmappedHueDegrees, 1);
        set => UnmappedHueDegrees = FritzBoxDevice.GetDoubleValue(value, 1);
    }

    private double? unmappedSaturation;

    [XmlIgnore]
    public double? UnmappedSaturation
    {
        get => unmappedSaturation;
        set => Set(ref unmappedSaturation, value, () => NotifyOfPropertyChange(nameof(UnmappedSaturationString)));
    }

    [XmlElement("unmapped_saturation")]
    public string? UnmappedSaturationString
    {
        get => FritzBoxDevice.GetStringValue(UnmappedSaturation, 1);
        set => UnmappedSaturation = FritzBoxDevice.GetDoubleValue(value, 1);
    }

    private double? temperatureKelvin;

    [XmlIgnore]
    public double? TemperatureKelvin
    {
        get => temperatureKelvin;
        set => Set(ref temperatureKelvin, value, () => NotifyOfPropertyChange(nameof(TemperatureKelvinString)));
    }

    [XmlElement("temperature")]
    public string? TemperatureKelvinString
    {
        get => FritzBoxDevice.GetStringValue(TemperatureKelvin, 1);
        set => TemperatureKelvin = FritzBoxDevice.GetDoubleValue(value, 1);
    }
}
