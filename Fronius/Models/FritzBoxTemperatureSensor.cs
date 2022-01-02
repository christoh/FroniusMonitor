using System.Xml.Serialization;

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
        get => FritzBoxDevice.GetStringValue(CorrectionOffset, 10);
        set => CorrectionOffset = FritzBoxDevice.GetDoubleValue(value, 10);
    }

    private double? temperature;

    [XmlIgnore]
    public double? Temperature
    {
        get => temperature;
        set => Set(ref temperature, value);
    }

    private double? correctionOffset;

    [XmlIgnore]
    public double? CorrectionOffset
    {
        get => correctionOffset;
        set => Set(ref correctionOffset, value);
    }

    [XmlIgnore] public double? RawTemperature => Temperature - CorrectionOffset;
}