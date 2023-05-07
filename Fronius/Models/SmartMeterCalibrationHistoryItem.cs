namespace De.Hochstaetter.Fronius.Models;

public class SmartMeterCalibrationHistoryItem : BindableBase
{
    private double energyRealConsumed;

    [XmlAttribute("Crl")]
    public double EnergyRealConsumed
    {
        get => energyRealConsumed;
        set => Set(ref energyRealConsumed, value);
    }

    private double energyRealProduced;

    [XmlAttribute("Prl")]
    public double EnergyRealProduced
    {
        get => energyRealProduced;
        set => Set(ref energyRealProduced, value);
    }

    private DateTime calibrationDate;

    [XmlAttribute("Date")]
    public DateTime CalibrationDate
    {
        get => calibrationDate;
        set => Set(ref calibrationDate, value);
    }

    private double consumedOffset;

    [XmlAttribute("CrlOffset")]
    public double ConsumedOffset
    {
        get => consumedOffset;
        set => Set(ref consumedOffset, value);
    }

    private double producedOffset;

    [XmlAttribute("PrlOffset")]
    public double ProducedOffset
    {
        get => producedOffset;
        set => Set(ref producedOffset, value);
    }
}
