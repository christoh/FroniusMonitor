using System.Globalization;
using System.Xml.Serialization;
using De.Hochstaetter.Fronius.Contracts;

namespace De.Hochstaetter.Fronius.Models;

[Flags]
public enum FritzBoxFeatures : uint
{
    HanFunDevice = 1 << 0,
    Light = 1 << 2,
    AlarmSensor = 1 << 4,
    AvmButton = 1 << 5,
    TemperatureRegulation = 1 << 6,
    PowerMeter = 1 << 7,
    TemperatureSensor = 1 << 8,
    AcOutlet = 1 << 9,
    DectRepeater = 1 << 10,
    Microphone = 1 << 11,
    HanFunUnit = 1 << 13,
    TurnOnOff = 1 << 15,
    HasLevels = 1 << 16,
    ColoredLight = 1 << 17,
    Blind = 1 << 18,
}

[XmlType("device")]
public class FritzBoxDevice : BindableBase, IHaveDisplayName
{
    private uint id;

    [XmlAttribute("id")]
    public uint Id
    {
        get => id;
        set => Set(ref id, value);
    }

    private uint functionMask;

    // ReSharper disable once StringLiteralTypo
    [XmlAttribute("functionbitmask")]
    public uint FunctionMask
    {
        get => functionMask;
        set => Set(ref functionMask, value,()=>NotifyOfPropertyChange(nameof(Features)));
    }

    [XmlIgnore]
    public FritzBoxFeatures Features
    {
        get => (FritzBoxFeatures)FunctionMask;
        set => FunctionMask = (uint)value;
    }

    private string ain = string.Empty;

    [XmlAttribute("identifier")]
    public string Ain
    {
        get => ain;
        set => Set(ref ain, value);
    }

    private string? firmwareVersionString;

    // ReSharper disable once StringLiteralTypo
    [XmlAttribute("fwversion")]
    public string? FirmwareVersionString
    {
        get => firmwareVersionString;
        set => Set(ref firmwareVersionString, value);
    }

    private string? manufacturer;

    [XmlAttribute("manufacturer")]
    public string? Manufacturer
    {
        get => manufacturer;
        set => Set(ref manufacturer, value);
    }

    private string? model;

    // ReSharper disable once StringLiteralTypo
    [XmlAttribute("productname")]
    public string? Model
    {
        get => model;
        set => Set(ref model, value);
    }

    private string displayName = string.Empty;

    [XmlElement("name")]
    public string DisplayName
    {
        get => displayName;
        set => Set(ref displayName, value);
    }

    private bool isBusy;

    [XmlElement("txbusy")]
    public bool IsBusy
    {
        get => isBusy;
        set => Set(ref isBusy, value);
    }

    private bool isPresent;

    [XmlElement("present")]
    public bool IsPresent
    {
        get => isPresent;
        set => Set(ref isPresent, value);
    }

    private FritzBoxSwitch? @switch;

    [XmlElement("switch")]
    public FritzBoxSwitch? Switch
    {
        get => @switch;
        set => Set(ref @switch, value);
    }

    private FritzBoxSimpleSwitch? simpleSwitch;

    [XmlElement("simpleonoff")]
    public FritzBoxSimpleSwitch? SimpleSwitch
    {
        get => simpleSwitch;
        set => Set(ref simpleSwitch, value);
    }

    private FritzBoxPowerMeter? powerMeter;

    [XmlElement("powermeter")]
    public FritzBoxPowerMeter? PowerMeter
    {
        get => powerMeter;
        set => Set(ref powerMeter, value);
    }

    private FritzBoxTemperatureSensor? temperatureSensor;

    [XmlElement("temperature")]
    public FritzBoxTemperatureSensor? TemperatureSensor
    {
        get => temperatureSensor;
        set => Set(ref temperatureSensor, value);
    }

    public static bool? GetBoolState(string? state) => state switch { "1" => true, "0" => false, _ => null };
    public static string GetStringState(bool? state) => state switch { true => "1", false => "0", null => string.Empty };
    public static string GetStringValue(double? value, double factor = 1000d) => !value.HasValue ? string.Empty : ((int)Math.Round(value.Value * factor, MidpointRounding.AwayFromZero)).ToString(CultureInfo.InvariantCulture);
    public static double? GetDoubleValue(string? value, double factor = 1000d) => string.IsNullOrWhiteSpace(value) ? null : int.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture) / factor;

    public override string ToString() => $"{Manufacturer} {Model}: {DisplayName}";
}
