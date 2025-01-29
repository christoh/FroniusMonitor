namespace De.Hochstaetter.Fronius.Models;

public enum FritzBoxSwitching
{
    [XmlEnum("auto")] Automatic,
    [XmlEnum("manuell")] Manual,
    [XmlEnum("")] Unknown,
}

[XmlType("switch")]
public class FritzBoxSwitch : BindableBase
{
    [XmlElement("state")]
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
