namespace De.Hochstaetter.Fronius.Models;

[XmlRoot("devicelist")]
[SuppressMessage("ReSharper", "StringLiteralTypo")]
public partial class FritzBoxDeviceList : BindableBase
{
    [ObservableProperty]
    [XmlAttribute("version")]
    public partial uint Version { get; set; }

    [ObservableProperty]
    [XmlAttribute("fwversion")]
    public partial string? FirmwareVersionString { get; set; }

    [XmlElement("device")] public List<FritzBoxDevice> Devices { get; } = [];

    [XmlIgnore] public List<IPowerMeter1P> DevicesWithPowerMeter => Devices.Where(d => d.PowerMeter != null).Cast<IPowerMeter1P>().ToList();
    [XmlIgnore] public IEnumerable<ISwitchable> PowerConsumers => Devices.Where(d => d.CanSwitch).Cast<ISwitchable>().ToList();
}
