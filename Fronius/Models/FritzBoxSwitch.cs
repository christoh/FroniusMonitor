namespace De.Hochstaetter.Fronius.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum FritzBoxSwitching
{
    [JsonStringEnumMemberName("auto")][XmlEnum("auto")] Automatic,
    [JsonStringEnumMemberName("manual")][XmlEnum("manuell")] Manual,
    [JsonStringEnumMemberName("unknown")][XmlEnum("")] Unknown,
}

[XmlType("switch")]
public partial class FritzBoxSwitch : BindableBase
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsTurnedOn))]
    [XmlElement("state")]
    [JsonIgnore]
    public partial string? IsTurnedOnString { get; set; }

    [XmlIgnore]
    public bool? IsTurnedOn
    {
        get => FritzBoxDevice.GetBoolState(IsTurnedOnString);
        set => IsTurnedOnString = FritzBoxDevice.GetStringState(value);
    }

    [ObservableProperty]
    [XmlElement("mode")]
    public partial FritzBoxSwitching? Switching { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsUiLocked))]
    [XmlElement("lock")]
    [JsonIgnore]
    public partial string? IsUiLockedString { get; set; }

    [XmlIgnore]
    public bool? IsUiLocked
    {
        get => FritzBoxDevice.GetBoolState(IsUiLockedString);
        set => IsUiLockedString = FritzBoxDevice.GetStringState(value);
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDeviceLocked))]
    [XmlElement("devicelock")]
    [JsonIgnore]
    public partial string? IsDeviceLockedString { get; set; }

    [XmlIgnore]
    public bool? IsDeviceLocked
    {
        get => FritzBoxDevice.GetBoolState(IsDeviceLockedString);
        set => IsDeviceLockedString = FritzBoxDevice.GetStringState(value);
    }
}

// ReSharper disable once StringLiteralTypo
[XmlType("simpleonoff")]
public partial class FritzBoxSimpleSwitch : BindableBase
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsTurnedOn))]
    [XmlElement("state")]
    [JsonIgnore]
    public partial string? IsTurnedOnString { get; set; }

    [XmlIgnore]
    public bool? IsTurnedOn
    {
        get => FritzBoxDevice.GetBoolState(IsTurnedOnString);
        set => IsTurnedOnString = FritzBoxDevice.GetStringState(value);
    }
}
