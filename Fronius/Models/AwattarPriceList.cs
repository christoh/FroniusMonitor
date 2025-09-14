namespace De.Hochstaetter.Fronius.Models;

public partial class AwattarPriceList : BindableBase
{
    [ObservableProperty]
    [JsonPropertyName("data")]
    public partial List<AwattarElectricityPrice> Prices { get; set; } = [];
}
