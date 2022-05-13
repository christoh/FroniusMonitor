namespace De.Hochstaetter.Fronius.Models;

[Flags]
public enum FritzBoxFeatures : uint
{
    None = 0,
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
public class FritzBoxDevice : BindableBase, IPowerConsumer1P
{
    private bool wasSwitched;

    [XmlIgnore]
    public IWebClientService? WebClientService { private get; set; }

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
        set => Set(ref functionMask, value, () => NotifyOfPropertyChange(nameof(Features)));
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

    public bool CanSwitch => (Features & FritzBoxFeatures.TurnOnOff) != FritzBoxFeatures.None;

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

    private FritzBoxLevel? levelControl;
    [XmlElement("levelcontrol")]
    public FritzBoxLevel? LevelControl
    {
        get => levelControl;
        set => Set(ref levelControl, value);
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

    private FritzBoxColorControl? color;
    [XmlElement("colorcontrol")]
    public FritzBoxColorControl? Color
    {
        get => color;
        set => Set(ref color, value);
    }

    private bool IsPresentAndUnlocked => IsPresent && (Switch is not { IsUiLocked: { } } || !Switch.IsUiLocked.Value);
    double? ITemperatureSensor.TemperatureCelsius => TemperatureSensor?.Temperature;
    double? IPowerMeter1P.Frequency => null;
    double? IPowerMeter1P.EnergyKiloWattHours => PowerMeter?.EnergyKiloWattHours;
    double? IPowerMeter1P.Voltage => PowerMeter?.Voltage;
    double? IPowerMeter1P.PowerWatts => PowerMeter?.PowerWatts;
    bool IPowerMeter1P.CanMeasurePower => (Features & FritzBoxFeatures.PowerMeter) != FritzBoxFeatures.None;
    bool? ISwitchable.IsTurnedOn => SimpleSwitch?.IsTurnedOn ?? Switch?.IsTurnedOn;
    string? IPowerConsumer1P.Model => string.IsNullOrWhiteSpace(Manufacturer) ? Model : $"{Manufacturer} {Model}".Trim();
    bool ISwitchable.IsSwitchingEnabled => !wasSwitched && IsPresentAndUnlocked && ((ISwitchable)this).CanSwitch;
    bool IDimmable.IsDimmingEnabled => !wasSwitched && IsPresentAndUnlocked && ((IDimmable)this).CanDim;
    bool IDimmable.CanDim => (Features & FritzBoxFeatures.HasLevels) != FritzBoxFeatures.None;
    double? IDimmable.Level => LevelControl?.Level;

    bool IHsvColorControl.HasHsvColorControl => (Features & FritzBoxFeatures.ColoredLight) != FritzBoxFeatures.None
                                                && Color is { SupportedModes: { } }
                                                && (Color.SupportedModes.Value & FritzBoxColorMode.Hsv) != FritzBoxColorMode.None;

    bool IHsvColorControl.IsHsvEnabled => !wasSwitched && IsPresentAndUnlocked && ((IHsvColorControl)this).HasHsvColorControl;
    double? IHsvColorControl.HueDegrees => Color?.HueDegrees;
    double? IHsvColorControl.Saturation => Color?.SaturationAbsolute / 255;
    double? IHsvColorControl.Value => LevelControl?.Level;

    bool IColorTemperatureControl.HasColorTemperatureControl => (Features & FritzBoxFeatures.ColoredLight) != FritzBoxFeatures.None
                                                                && Color is { SupportedModes: { } }
                                                                && (Color.SupportedModes.Value & FritzBoxColorMode.Temperature) != FritzBoxColorMode.None;

    bool IColorTemperatureControl.IsColorTemperatureEnabled => !wasSwitched && IsPresentAndUnlocked && ((IColorTemperatureControl)this).HasColorTemperatureControl;
    double? IColorTemperatureControl.ColorTemperatureKelvin => Color?.TemperatureKelvin;
    double IColorTemperatureControl.MinTemperatureKelvin => 2700;
    double IColorTemperatureControl.MaxTemperatureKelvin => 6500;
    bool IHsvColorControl.IsHsvActive => Color is { CurrentMode: { } } && (Color.CurrentMode.Value & FritzBoxColorMode.Hsv) != FritzBoxColorMode.None;
    bool IColorTemperatureControl.IsColorTemperatureActive => Color is { CurrentMode: { } } && (Color.CurrentMode.Value & FritzBoxColorMode.Temperature) != FritzBoxColorMode.None;

    public static bool? GetBoolState(string? state) => state switch { "1" => true, "0" => false, _ => null };
    public static string GetStringState(bool? state) => state switch { true => "1", false => "0", null => string.Empty };
    public static string GetStringValue(double? value, double factor = 1000d) => !value.HasValue ? string.Empty : ((int)Math.Round(value.Value * factor, MidpointRounding.AwayFromZero)).ToString(CultureInfo.InvariantCulture);
    public static double? GetDoubleValue(string? value, double factor = 1000d) => string.IsNullOrWhiteSpace(value) ? null : int.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture) / factor;
    public static uint? GetUintValue(string? value) => string.IsNullOrWhiteSpace(value) ? null : uint.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);

    public override string ToString() => $"{Manufacturer} {Model}: {DisplayName}";

    public async Task TurnOnOff(bool turnOn)
    {
        InitiateSwitching();

        if (turnOn)
        {
            await WebClientService!.TurnOnFritzBoxDevice(Ain).ConfigureAwait(false);
        }
        else
        {
            await WebClientService!.TurnOffFritzBoxDevice(Ain).ConfigureAwait(false);
        }
    }

    public async Task SetLevel(double level)
    {
        InitiateSwitching();
        await WebClientService!.SetFritzBoxLevel(Ain, level).ConfigureAwait(false);
    }

    public async Task SetHsv(double hueDegrees, double saturation, double value)
    {
        InitiateSwitching();
        await WebClientService!.SetFritzBoxColor(Ain, hueDegrees, saturation).ConfigureAwait(false);
        await WebClientService!.SetFritzBoxLevel(Ain, value).ConfigureAwait(false);
    }

    public async Task SetColorTemperature(double colorTemperatureKelvin)
    {
        InitiateSwitching();
        await WebClientService!.SetFritzBoxColorTemperature(Ain, colorTemperatureKelvin).ConfigureAwait(false);
    }

    private void InitiateSwitching()
    {
        if (WebClientService == null)
        {
            throw new InvalidOperationException("No WebClientService");
        }

        wasSwitched = true;
        NotifyOfPropertyChange(nameof(ISwitchable.IsSwitchingEnabled));
        NotifyOfPropertyChange(nameof(IDimmable.IsDimmingEnabled));
        NotifyOfPropertyChange(nameof(IHsvColorControl.IsHsvEnabled));
        NotifyOfPropertyChange(nameof(IColorTemperatureControl.IsColorTemperatureEnabled));
    }
}
