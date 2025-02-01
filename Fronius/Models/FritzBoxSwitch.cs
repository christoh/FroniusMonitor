namespace De.Hochstaetter.Fronius.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum FritzBoxSwitching
{
    [JsonStringEnumMemberName("auto")][XmlEnum("auto")] Automatic,
    [JsonStringEnumMemberName("manual")][XmlEnum("manuell")] Manual,
    [JsonStringEnumMemberName("unknown")][XmlEnum("")] Unknown,
}

[XmlType("switch")]
public class FritzBoxSwitch : BindableBase
{
    [XmlElement("state")]
    [JsonIgnore]
    public string? IsTurnedOnString
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(IsTurnedOn)));
    }

    [XmlIgnore]
    public bool? IsTurnedOn
    {
        get => FritzBoxDevice.GetBoolState(IsTurnedOnString);
        set => IsTurnedOnString = FritzBoxDevice.GetStringState(value);
    }

    [XmlElement("mode")]
    public FritzBoxSwitching? Switching
    {
        get;
        set => Set(ref field, value);
    }

    [XmlElement("lock")]
    [JsonIgnore]
    public string? IsUiLockedString
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(IsUiLocked)));
    }

    [XmlIgnore]
    public bool? IsUiLocked
    {
        get => FritzBoxDevice.GetBoolState(IsUiLockedString);
        set => IsUiLockedString = FritzBoxDevice.GetStringState(value);
    }

    [XmlElement("devicelock")]
    [JsonIgnore]
    public string? IsDeviceLockedString
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(IsDeviceLocked)));
    }

    [XmlIgnore]
    public bool? IsDeviceLocked
    {
        get => FritzBoxDevice.GetBoolState(IsDeviceLockedString);
        set => IsDeviceLockedString = FritzBoxDevice.GetStringState(value);
    }
}

// ReSharper disable once StringLiteralTypo
[XmlType("simpleonoff")]
public class FritzBoxSimpleSwitch : BindableBase
{
    [XmlElement("state")]
    [JsonIgnore]
    public string? IsTurnedOnString
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(IsTurnedOn)));
    }

    [XmlIgnore]
    public bool? IsTurnedOn
    {
        get => FritzBoxDevice.GetBoolState(IsTurnedOnString);
        set => IsTurnedOnString = FritzBoxDevice.GetStringState(value);
    }
}
