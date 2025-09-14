namespace De.Hochstaetter.Fronius.Models;

public partial class AwattarEnergy : AwattarBase
{
    [ObservableProperty]
    [JsonPropertyName("solar")]
    public partial double SolarProductionMegaWatt { get; set; }

    [ObservableProperty]
    [JsonPropertyName("wind")]
    public partial double WindProductionMegaWatt { get; set; }
}
