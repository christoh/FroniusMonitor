namespace De.Hochstaetter.Fronius.Models;

[XmlType("rawTemperature")]
public partial class FritzBoxTemperatureSensor : BindableBase
{
    [XmlElement("celsius")]
    [JsonIgnore]
    public string? TemperatureString
    {
        get => FritzBoxDevice.GetStringValue(Temperature, 10);
        set => Temperature = FritzBoxDevice.GetDoubleValue(value, 10);
    }

    [XmlElement("offset")]
    [JsonIgnore]
    public string? OffsetString
    {
        get => FritzBoxDevice.GetStringValue(Offset, 10);
        set => Offset = FritzBoxDevice.GetDoubleValue(value, 10);
    }

    [XmlIgnore]
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TemperatureString),nameof(RawTemperature))]
    public partial double? Temperature { get; set; }

    [XmlIgnore]
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(OffsetString),nameof(RawTemperature))]
    public partial double? Offset { get; set; }

    [XmlIgnore] public double? RawTemperature => Temperature - Offset;
}