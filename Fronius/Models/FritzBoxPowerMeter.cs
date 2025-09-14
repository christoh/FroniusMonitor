namespace De.Hochstaetter.Fronius.Models;

[XmlType("powermeter")]
[SuppressMessage("ReSharper", "StringLiteralTypo")]
public partial class FritzBoxPowerMeter : BindableBase
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

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EnergyString))]
    [XmlIgnore]
    public partial double? EnergyConsumed { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(VoltageString))]
    [XmlIgnore]
    public partial double? Voltage { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PowerString))]
    public partial double? PowerWatts { get; set; }
}