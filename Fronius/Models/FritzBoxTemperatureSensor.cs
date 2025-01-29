namespace De.Hochstaetter.Fronius.Models;

[XmlType("rawTemperature")]
public class FritzBoxTemperatureSensor : BindableBase
{
    [XmlElement("celsius")]
    public string? TemperatureString
    {
        get => FritzBoxDevice.GetStringValue(Temperature, 10);
        set => Temperature = FritzBoxDevice.GetDoubleValue(value, 10);
    }

    [XmlElement("offset")]
    public string? OffsetString
    {
        get => FritzBoxDevice.GetStringValue(Offset, 10);
        set => Offset = FritzBoxDevice.GetDoubleValue(value, 10);
    }

    [XmlIgnore]
    public double? Temperature
    {
        get;
        set => Set(ref field, value, () =>
        {
            NotifyOfPropertyChange(nameof(TemperatureString));
            NotifyOfPropertyChange(nameof(RawTemperature));
        });
    }

    [XmlIgnore]
    public double? Offset
    {
        get;
        set => Set(ref field, value, () =>
        {
            NotifyOfPropertyChange(nameof(OffsetString));
            NotifyOfPropertyChange(nameof(RawTemperature));
        });
    }

    [XmlIgnore] public double? RawTemperature => Temperature - Offset;
}