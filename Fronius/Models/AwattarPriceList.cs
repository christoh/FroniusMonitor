namespace De.Hochstaetter.Fronius.Models;

public class AwattarPriceList : BindableBase
{
    private List<AwattarElectricityPrice> prices = [];

    [JsonPropertyName("data")]
    public List<AwattarElectricityPrice> Prices
    {
        get => prices;
        set => Set(ref prices, value);
    }
}
