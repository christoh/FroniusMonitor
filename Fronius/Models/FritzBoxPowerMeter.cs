namespace De.Hochstaetter.Fronius.Models;

[XmlType("powermeter")]
public class FritzBoxPowerMeter : BindableBase
{
    [XmlElement("voltage")]
    public string? VoltageString
    {
        get => FritzBoxDevice.GetStringValue(Voltage);
        set => Voltage = FritzBoxDevice.GetDoubleValue(value);
    }

    [XmlElement("power")]
    public string? PowerString
    {
        get => FritzBoxDevice.GetStringValue(PowerWatts);
        set => PowerWatts = FritzBoxDevice.GetDoubleValue(value);
    }

    [XmlElement("energy")]
    public string? EnergyString
    {
        get => FritzBoxDevice.GetStringValue(EnergyConsumed, 1);
        set => EnergyConsumed = FritzBoxDevice.GetDoubleValue(value, 1);
    }

    private double? energyConsumed;

    [XmlIgnore]
    public double? EnergyConsumed
    {
        get => energyConsumed;
        set => Set(ref energyConsumed, value, () => NotifyOfPropertyChange(nameof(EnergyString)));
    }

    private double? voltage;
    [XmlIgnore]
    public double? Voltage
    {
        get => voltage;
        set => Set(ref voltage, value,()=>NotifyOfPropertyChange(nameof(VoltageString)));
    }

    private double? powerWatts;

    public double? PowerWatts
    {
        get => powerWatts;
        set => Set(ref powerWatts, value,()=>NotifyOfPropertyChange(nameof(PowerString)));
    }
}