using De.Hochstaetter.Fronius.JsonConverters;

namespace De.Hochstaetter.Fronius.Models;

public class AwattarPriceInfo : BindableBase
{
    private string? @object;

    public string? Object
    {
        get => @object;
        set => Set(ref @object, value);
    }

    private AwattarData data = new();

    public AwattarData Data
    {
        get => data;
        set => Set(ref data, value);
    }
}

public class AwattarData : BindableBase
{
    private List<AwattarPriceComponent> prices = [];

    public List<AwattarPriceComponent> Prices
    {
        get => prices;
        set => Set(ref prices, value);
    }
}

public class AwattarPriceComponent : BindableBase
{
    private decimal price;
    [JsonConverter(typeof(AwattarPriceInfoConverter))]
    public decimal Price
    {
        get => price;
        set => Set(ref price, value);
    }

    private string? description;

    public string? Description
    {
        get => description;
        set => Set(ref description, value);
    }

    private decimal taxRate;

    public decimal TaxRate
    {
        get => taxRate;
        set => Set(ref taxRate, value);
    }

    private string? name;

    public string? Name
    {
        get => name;
        set => Set(ref name, value);
    }

    private string? priceUnit;

    public string? PriceUnit
    {
        get => priceUnit;
        set => Set(ref priceUnit, value);
    }
}