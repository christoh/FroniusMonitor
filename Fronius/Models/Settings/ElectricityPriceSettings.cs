namespace De.Hochstaetter.Fronius.Models.Settings;

public class ElectricityPriceSettings : BindableBase
{
    public event EventHandler<ElectricityPriceService>? ElectricityPriceServiceChanged;

    [XmlAttribute]
    public ElectricityPriceService Service
    {
        get;
        set => Set(ref field, value, () => Task.Run(() => ElectricityPriceServiceChanged?.Invoke(this, value)));
    } = ElectricityPriceService.Awattar;

    [XmlIgnore]
    public decimal PriceFactorBuy
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(PriceSurchargeBuyPercent)));
    } = 1;

    //[DefaultValue(0m)]
    [XmlAttribute]
    public decimal PriceOffsetBuy
    {
        get;
        set => Set(ref field, value);
    }

    [XmlAttribute("PriceSurchargeBuy")]
    public decimal PriceSurchargeBuyPercent
    {
        get => (PriceFactorBuy - 1) * 100;
        set => PriceFactorBuy = 1 + value / 100;
    }

    [XmlIgnore]
    public decimal VatFactor
    {
        get;
        set => Set(ref field, value, () =>
        {
            NotifyOfPropertyChange(nameof(VatRatePercent));
            NotifyOfPropertyChange(nameof(VatRate));
        });
    } = 1.19m;

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
    public AwattarCountry PriceRegion
    {
        get;
        set => Set(ref field, value);
    } = AwattarCountry.GermanyLuxembourg;

    public void CopyFrom(ElectricityPriceSettings other)
    {
        Service = other.Service;
        PriceFactorBuy = other.PriceFactorBuy;
        VatFactor = other.VatFactor;
        PriceRegion = other.PriceRegion;
        PriceOffsetBuy = other.PriceOffsetBuy;
    }
}
