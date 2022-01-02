using System.Xml.Serialization;

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
    private string? isTurnedOnString;

    [XmlElement("state")]
    public string? IsTurnedOnString
    {
        get => isTurnedOnString;
        set => Set(ref isTurnedOnString, value, () => NotifyOfPropertyChange(nameof(IsTurnedOn)));
    }

    [XmlIgnore]
    public bool? IsTurnedOn
    {
        get => FritzBoxDevice.GetBoolState(IsTurnedOnString);
        set => IsTurnedOnString = FritzBoxDevice.GetStringState(value);
    }

    private FritzBoxSwitching? switching;

    [XmlElement("mode")]
    public FritzBoxSwitching? Switching
    {
        get => switching;
        set => Set(ref switching, value);
    }

    private string? isUiLockedString;

    [XmlElement("lock")]
    public string? IsUiLockedString
    {
        get => isUiLockedString;
        set => Set(ref isUiLockedString, value, () => NotifyOfPropertyChange(nameof(IsUiLocked)));
    }

    [XmlIgnore]
    public bool? IsUiLocked
    {
        get => FritzBoxDevice.GetBoolState(IsUiLockedString);
        set => IsUiLockedString = FritzBoxDevice.GetStringState(value);
    }

    private string? isDeviceLockedString;

    [XmlElement("devicelock")]
    public string? IsDeviceLockedString
    {
        get => isDeviceLockedString;
        set => Set(ref isDeviceLockedString, value, () => NotifyOfPropertyChange(nameof(IsDeviceLocked)));
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
    private string? isTurnedOnString;

    [XmlElement("state")]
    public string? IsTurnedOnString
    {
        get => isTurnedOnString;
        set => Set(ref isTurnedOnString, value, () => NotifyOfPropertyChange(nameof(IsTurnedOn)));
    }

    [XmlIgnore]
    public bool? IsTurnedOn
    {
        get => FritzBoxDevice.GetBoolState(IsTurnedOnString);
        set => IsTurnedOnString = FritzBoxDevice.GetStringState(value);
    }
}
