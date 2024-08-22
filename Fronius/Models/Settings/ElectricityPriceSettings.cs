namespace De.Hochstaetter.Fronius.Models.Settings;

public class ElectricityPriceSettings : BindableBase
{
    public event EventHandler<ElectricityPriceService>? ElectricityPriceServiceChanged;

    private ElectricityPriceService service = ElectricityPriceService.Awattar;
    [XmlAttribute]
    public ElectricityPriceService Service
    {
        get => service;
        set => Set(ref service, value, () => Task.Run(() => ElectricityPriceServiceChanged?.Invoke(this, value)));
    }

    private decimal priceFactorBuy = 1;
    [XmlIgnore]
    public decimal PriceFactorBuy
    {
        get => priceFactorBuy;
        set => Set(ref priceFactorBuy, value, () => NotifyOfPropertyChange(nameof(PriceSurchargeBuyPercent)));
    }

    private decimal priceOffsetBuy;
    //[DefaultValue(0m)]
    [XmlAttribute]
    public decimal PriceOffsetBuy
    {
        get => priceOffsetBuy;
        set => Set(ref priceOffsetBuy, value);
    }

    [XmlAttribute("PriceSurchargeBuy")]
    public decimal PriceSurchargeBuyPercent
    {
        get => (PriceFactorBuy - 1) * 100;
        set => PriceFactorBuy = 1 + value / 100;
    }

    private decimal vatFactor = 1.19m;
    [XmlIgnore]
    public decimal VatFactor
    {
        get => vatFactor;
        set => Set(ref vatFactor, value, () =>
        {
            NotifyOfPropertyChange(nameof(VatRatePercent));
            NotifyOfPropertyChange(nameof(VatRate));
        });
    }

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

    private AwattarCountry priceRegion = AwattarCountry.GermanyLuxembourg;
    [XmlAttribute]
    public AwattarCountry PriceRegion
    {
        get => priceRegion;
        set => Set(ref priceRegion, value);
    }

    public void CopyFrom(ElectricityPriceSettings other)
    {
        Service = other.Service;
        PriceFactorBuy = other.PriceFactorBuy;
        VatFactor = other.VatFactor;
        PriceRegion = other.PriceRegion;
        PriceOffsetBuy = other.PriceOffsetBuy;
    }
}
