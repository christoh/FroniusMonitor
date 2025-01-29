using De.Hochstaetter.Fronius.JsonConverters;

namespace De.Hochstaetter.Fronius.Models;

public class AwattarPriceInfo : BindableBase
{
    public string? Object
    {
        get;
        set => Set(ref field, value);
    }

    public AwattarData Data
    {
        get;
        set => Set(ref field, value);
    } = new();
}

public class AwattarData : BindableBase
{
    public List<AwattarPriceComponent> Prices
    {
        get;
        set => Set(ref field, value);
    } = [];
}

public class AwattarPriceComponent : BindableBase
{
    [JsonConverter(typeof(AwattarPriceInfoConverter))]
    public decimal Price
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(GrossPrice)));
    }

    public decimal GrossPrice => Price + Price * TaxRate;

    public string? Description
    {
        get;
        set => Set(ref field, value);
    }

    public decimal TaxRate
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(GrossPrice)));
    }

    public string? Name
    {
        get;
        set => Set(ref field, value);
    }

    public string? PriceUnit
    {
        get;
        set => Set(ref field, value);
    }
}