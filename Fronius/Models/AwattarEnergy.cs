namespace De.Hochstaetter.Fronius.Models;

public class AwattarEnergy : AwattarBase
{
    private double solarProductionMegaWatt;
    [JsonPropertyName("solar")]
    public double SolarProductionMegaWatt
    {
        get => solarProductionMegaWatt;
        set => Set(ref solarProductionMegaWatt, value);
    }

    private double windProductionMegaWatt;
    [JsonPropertyName("wind")]
    public double WindProductionMegaWatt
    {
        get => windProductionMegaWatt;
        set => Set(ref windProductionMegaWatt, value);
    }
}
