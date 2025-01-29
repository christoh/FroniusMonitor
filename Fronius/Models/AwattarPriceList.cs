namespace De.Hochstaetter.Fronius.Models;

public class AwattarPriceList : BindableBase
{
    [JsonPropertyName("data")]
    public List<AwattarElectricityPrice> Prices
    {
        get;
        set => Set(ref field, value);
    } = [];
}
