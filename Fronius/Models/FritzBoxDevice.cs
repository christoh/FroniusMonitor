namespace De.Hochstaetter.Fronius.Models;

[Flags]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum FritzBoxFeatures : uint
{
    [JsonStringEnumMemberName("none")]
    None = 0,
    [JsonStringEnumMemberName("hanFunDevice")]
    HanFunDevice = 1 << 0,
    [JsonStringEnumMemberName("light")]
    Light = 1 << 2,
    [JsonStringEnumMemberName("alarmSensor")]
    AlarmSensor = 1 << 4,
    [JsonStringEnumMemberName("avmButton")]
    AvmButton = 1 << 5,
    [JsonStringEnumMemberName("temperatureRegulation")]
    TemperatureRegulation = 1 << 6,
    [JsonStringEnumMemberName("powerMeter")]
    PowerMeter = 1 << 7,
    [JsonStringEnumMemberName("temperatureSensor")]
    TemperatureSensor = 1 << 8,
    [JsonStringEnumMemberName("acOutlet")]
    AcOutlet = 1 << 9,
    [JsonStringEnumMemberName("dectRepeater")]
    DectRepeater = 1 << 10,
    [JsonStringEnumMemberName("microphone")]
    Microphone = 1 << 11,
    [JsonStringEnumMemberName("hanFunUnit")]
    HanFunUnit = 1 << 13,
    [JsonStringEnumMemberName("turnOnOff")]
    TurnOnOff = 1 << 15,
    [JsonStringEnumMemberName("hasLevels")]
    HasLevels = 1 << 16,
    [JsonStringEnumMemberName("coloredLight")]
    ColoredLight = 1 << 17,
    [JsonStringEnumMemberName("blind")]
    Blind = 1 << 18,
}

[XmlType("device")]
public partial class FritzBoxDevice : BindableBase, IPowerConsumer1P
{
    public void CopyFrom(FritzBoxDevice other)
    {
        Id = other.Id;
        wasSwitched = false;
        FunctionMask = other.FunctionMask;
        Ain = other.Ain;
        FirmwareVersionString = other.FirmwareVersionString;
        Manufacturer = other.Manufacturer;
        Model = other.Model;
        DisplayName = other.DisplayName;
        IsBusy = other.IsBusy;
        IsPresent = other.IsPresent;
        Switch = other.Switch;
        LevelControl = other.LevelControl;
        SimpleSwitch = other.SimpleSwitch;
        PowerMeter = other.PowerMeter;
        TemperatureSensor = other.TemperatureSensor;
        Color = other.Color;
        Refresh();
    }

    private bool wasSwitched;

    [XmlIgnore]
    public IFritzBoxService? FritzBoxService { private get; set; }

    [XmlAttribute("id")]
    public uint Id
    {
        get;
        set => Set(ref field, value);
    }

    // ReSharper disable once StringLiteralTypo
    [XmlAttribute("functionbitmask")]
    public uint FunctionMask
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(Features)));
    }

    [XmlIgnore, JsonIgnore]
    public FritzBoxFeatures Features
    {
        get => (FritzBoxFeatures)FunctionMask;
        set => FunctionMask = (uint)value;
    }

    [XmlAttribute("identifier")]
    public string Ain
    {
        get;
        set => Set(ref field, value);
    } = string.Empty;

    // ReSharper disable once StringLiteralTypo
    [XmlAttribute("fwversion")]
    public string? FirmwareVersionString
    {
        get;
        set => Set(ref field, value);
    }

    [XmlAttribute("manufacturer")]
    public string? Manufacturer
    {
        get;
        set => Set(ref field, value);
    }

    // ReSharper disable once StringLiteralTypo
    [XmlAttribute("productname")]
    public string? Model
    {
        get;
        set => Set(ref field, value);
    }

    public bool CanSwitch => (Features & FritzBoxFeatures.TurnOnOff) != FritzBoxFeatures.None;

    [XmlElement("name")]
    public string DisplayName
    {
        get;
        set => Set(ref field, value);
    } = string.Empty;

    [XmlElement("txbusy")]
    public bool IsBusy
    {
        get;
        set => Set(ref field, value);
    }

    [XmlElement("present")]
    public bool IsPresent
    {
        get;
        set => Set(ref field, value);
    }

    [XmlElement("switch")]
    public FritzBoxSwitch? Switch
    {
        get;
        set => Set(ref field, value);
    }

    [XmlElement("levelcontrol")]
    public FritzBoxLevel? LevelControl
    {
        get;
        set => Set(ref field, value);
    }

    [XmlElement("simpleonoff")]
    public FritzBoxSimpleSwitch? SimpleSwitch
    {
        get;
        set => Set(ref field, value);
    }

    [XmlElement("powermeter")]
    public FritzBoxPowerMeter? PowerMeter
    {
        get;
        set => Set(ref field, value);
    }

    [XmlElement("temperature")]
    public FritzBoxTemperatureSensor? TemperatureSensor
    {
        get;
        set => Set(ref field, value);
    }

    [XmlElement("colorcontrol")]
    public FritzBoxColorControl? Color
    {
        get;
        set => Set(ref field, value);
    }

    private bool IsPresentAndUnlocked => IsPresent && (Switch is not { IsUiLocked: { } } || !Switch.IsUiLocked.Value);
    double? ITemperatureSensor.TemperatureCelsius => TemperatureSensor?.Temperature;
    double? IPowerMeter1P.Frequency => null;
    double? IPowerMeter1P.EnergyConsumed => PowerMeter?.EnergyConsumed;
    double? IPowerMeter1P.Voltage => PowerMeter?.Voltage;
    double? IPowerMeter1P.ActivePower => PowerMeter?.PowerWatts;
    bool IPowerMeter1P.CanMeasurePower => (Features & FritzBoxFeatures.PowerMeter) != FritzBoxFeatures.None;
    bool? ISwitchable.IsTurnedOn => SimpleSwitch?.IsTurnedOn ?? Switch?.IsTurnedOn;
    string? IPowerConsumer1P.Model => string.IsNullOrWhiteSpace(Manufacturer) ? Model : $"{Manufacturer} {Model}".Trim();
    bool ISwitchable.IsSwitchingEnabled => !wasSwitched && IsPresentAndUnlocked && ((ISwitchable)this).CanSwitch;
    bool IDimmable.IsDimmingEnabled => !wasSwitched && IsPresentAndUnlocked && ((IDimmable)this).CanDim;
    bool IDimmable.CanDim => (Features & FritzBoxFeatures.HasLevels) != FritzBoxFeatures.None;
    double? IDimmable.Level => LevelControl?.Level;

    bool IHsvColorControl.HasHsvColorControl => (Features & FritzBoxFeatures.ColoredLight) != FritzBoxFeatures.None
                                                && Color is { SupportedModes: not null }
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
    string IHaveUniqueId.SerialNumber => Ain.Replace(" ", string.Empty);
    string? IPowerMeter1P.DeviceVersion => FirmwareVersionString;

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
            await FritzBoxService!.TurnOnFritzBoxDevice(Ain).ConfigureAwait(false);
        }
        else
        {
            await FritzBoxService!.TurnOffFritzBoxDevice(Ain).ConfigureAwait(false);
        }
    }

    public async Task SetLevel(double level)
    {
        InitiateSwitching();
        await FritzBoxService!.SetFritzBoxLevel(Ain, level).ConfigureAwait(false);
    }

    public async Task SetHsv(double hueDegrees, double saturation, double value)
    {
        InitiateSwitching();
        await FritzBoxService!.SetFritzBoxColor(Ain, hueDegrees, saturation).ConfigureAwait(false);
        await FritzBoxService!.SetFritzBoxLevel(Ain, value).ConfigureAwait(false);
    }

    public async Task SetColorTemperature(double colorTemperatureKelvin)
    {
        InitiateSwitching();
        await FritzBoxService!.SetFritzBoxColorTemperature(Ain, colorTemperatureKelvin).ConfigureAwait(false);
    }

    private void InitiateSwitching()
    {
        if (FritzBoxService == null)
        {
            throw new InvalidOperationException("No FritzBoxService");
        }

        wasSwitched = true;
        NotifyOfPropertyChange(nameof(ISwitchable.IsSwitchingEnabled));
        NotifyOfPropertyChange(nameof(IDimmable.IsDimmingEnabled));
        NotifyOfPropertyChange(nameof(IHsvColorControl.IsHsvEnabled));
        NotifyOfPropertyChange(nameof(IColorTemperatureControl.IsColorTemperatureEnabled));
    }
}
