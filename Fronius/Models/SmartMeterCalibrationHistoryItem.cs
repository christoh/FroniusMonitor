namespace De.Hochstaetter.Fronius.Models;

public partial class SmartMeterCalibrationHistoryItem : BindableBase
{
    [ObservableProperty]
    [XmlAttribute("Crl")]
    public partial double EnergyRealConsumed { get; set; }

    [ObservableProperty]
    [XmlAttribute("Prl")]
    public partial double EnergyRealProduced { get; set; }

    [ObservableProperty]
    [XmlAttribute("Date")]
    public partial DateTime CalibrationDate { get; set; }

    [ObservableProperty]
    [XmlAttribute("CrlOffset")]
    public partial double ConsumedOffset { get; set; }

    [ObservableProperty]
    [XmlAttribute("PrlOffset")]
    public partial double ProducedOffset { get; set; }
}
