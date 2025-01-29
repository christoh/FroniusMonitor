namespace De.Hochstaetter.Fronius.Models;

[XmlRoot("devicelist")]
[SuppressMessage("ReSharper", "StringLiteralTypo")]
public class FritzBoxDeviceList : BindableBase
{
    [XmlAttribute("version")]
    public uint Version
    {
        get;
        set => Set(ref field, value);
    }

    [XmlAttribute("fwversion")]
    public string? FirmwareVersionString
    {
        get;
        set => Set(ref field, value);
    }

    [XmlElement("device")] public List<FritzBoxDevice> Devices { get; } = new();

    [XmlIgnore] public List<IPowerMeter1P> DevicesWithPowerMeter => Devices.Where(d => d.PowerMeter != null).Cast<IPowerMeter1P>().ToList();
    [XmlIgnore] public IEnumerable<ISwitchable> PowerConsumers => Devices.Where(d => d.CanSwitch).Cast<ISwitchable>().ToList();
}
