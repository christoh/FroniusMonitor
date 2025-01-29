namespace De.Hochstaetter.Fronius.Models;

public class SmartMeterCalibrationHistoryItem : BindableBase
{
    [XmlAttribute("Crl")]
    public double EnergyRealConsumed
    {
        get;
        set => Set(ref field, value);
    }

    [XmlAttribute("Prl")]
    public double EnergyRealProduced
    {
        get;
        set => Set(ref field, value);
    }

    [XmlAttribute("Date")]
    public DateTime CalibrationDate
    {
        get;
        set => Set(ref field, value);
    }

    [XmlAttribute("CrlOffset")]
    public double ConsumedOffset
    {
        get;
        set => Set(ref field, value);
    }

    [XmlAttribute("PrlOffset")]
    public double ProducedOffset
    {
        get;
        set => Set(ref field, value);
    }
}
