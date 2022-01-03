using System.Diagnostics.CodeAnalysis;
using System.Xml.Serialization;
using De.Hochstaetter.Fronius.Contracts;

namespace De.Hochstaetter.Fronius.Models;

[XmlRoot("devicelist")]
[SuppressMessage("ReSharper", "StringLiteralTypo")]
public class FritzBoxDeviceList : BindableBase
{
    private uint version;

    [XmlAttribute("version")]
    public uint Version
    {
        get => version;
        set => Set(ref version, value);
    }

    private string? firmwareVersionString;

    [XmlAttribute("fwversion")]
    public string? FirmwareVersionString
    {
        get => firmwareVersionString;
        set => Set(ref firmwareVersionString, value);
    }

    [XmlElement("device")] public List<FritzBoxDevice> Devices { get; } = new();

    [XmlIgnore] public IEnumerable<IPowerMeter1P> DevicesWithPowerMeter => Devices.Where(d => d.PowerMeter != null);
}
