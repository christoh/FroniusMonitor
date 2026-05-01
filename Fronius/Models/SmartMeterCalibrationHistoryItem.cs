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
    [DefaultValue(double.NaN)]
    [XmlAttribute("CrlOffset")]
    public partial double ConsumedOffset { get; set; } = double.NaN;

    [ObservableProperty]
    [DefaultValue(double.NaN)]
    [XmlAttribute("PrlOffset")]
    public partial double ProducedOffset { get; set; } = double.NaN;

#if DEBUG
    public override string ToString() => $"{CalibrationDate:g} P: {EnergyRealProduced} ({ProducedOffset}) / C: {EnergyRealConsumed} ({ConsumedOffset})";
#endif
}