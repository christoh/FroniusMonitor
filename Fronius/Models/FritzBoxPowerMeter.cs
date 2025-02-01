namespace De.Hochstaetter.Fronius.Models;

[XmlType("powermeter")]
public class FritzBoxPowerMeter : BindableBase
{
    [XmlElement("voltage")]
    [JsonIgnore]
    public string? VoltageString
    {
        get => FritzBoxDevice.GetStringValue(Voltage);
        set => Voltage = FritzBoxDevice.GetDoubleValue(value);
    }

    [XmlElement("power")]
    [JsonIgnore]
    public string? PowerString
    {
        get => FritzBoxDevice.GetStringValue(PowerWatts);
        set => PowerWatts = FritzBoxDevice.GetDoubleValue(value);
    }

    [XmlElement("energy")]
    [JsonIgnore]
    public string? EnergyString
    {
        get => FritzBoxDevice.GetStringValue(EnergyConsumed, 1);
        set => EnergyConsumed = FritzBoxDevice.GetDoubleValue(value, 1);
    }

    [XmlIgnore]
    public double? EnergyConsumed
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(EnergyString)));
    }

    [XmlIgnore]
    public double? Voltage
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(VoltageString)));
    }

    public double? PowerWatts
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(PowerString)));
    }
}