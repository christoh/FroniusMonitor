namespace De.Hochstaetter.Fronius.Models.Settings;

public partial class ElectricityPriceSettings : BindableBase
{
    public event EventHandler<ElectricityPriceService>? ElectricityPriceServiceChanged;

    [XmlAttribute]
    public ElectricityPriceService Service
    {
        get;
        set => Set(ref field, value, () => Task.Run(() => ElectricityPriceServiceChanged?.Invoke(this, value)));
    } = ElectricityPriceService.Awattar;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PriceSurchargeBuyPercent))]
    [XmlIgnore]
    // As of 2025 virtually no provider is using this, so default to 1
    public partial decimal PriceFactorBuy { get; set; } = 1;

    [ObservableProperty]
    //[DefaultValue(0m)]
    [XmlAttribute]
    public partial decimal PriceOffsetBuy { get; set; }

    [XmlAttribute("PriceSurchargeBuy")]
    public decimal PriceSurchargeBuyPercent
    {
        get => (PriceFactorBuy - 1) * 100;
        set => PriceFactorBuy = 1 + value / 100;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(VatRatePercent),nameof(VatRate))]
    [XmlIgnore]
    public partial decimal VatFactor { get; set; } = 1.19m;

    [XmlAttribute(nameof(VatRate))]
    public decimal VatRatePercent
    {
        get => (VatFactor - 1) * 100;
        set => VatFactor = 1 + value / 100;
    }

    [XmlIgnore]
    public decimal VatRate
    {
        get => VatFactor - 1;
        set => VatFactor = 1 + value;
    }

    [XmlAttribute]
    [ObservableProperty]
    public partial AwattarCountry PriceRegion { get; set; } = AwattarCountry.GermanyLuxembourg;

    public void CopyFrom(ElectricityPriceSettings other)
    {
        Service = other.Service;
        PriceFactorBuy = other.PriceFactorBuy;
        VatFactor = other.VatFactor;
        PriceRegion = other.PriceRegion;
        PriceOffsetBuy = other.PriceOffsetBuy;
    }
}
