namespace De.Hochstaetter.Fronius.Models;

public class AwattarEnergy : AwattarBase
{
    [JsonPropertyName("solar")]
    public double SolarProductionMegaWatt
    {
        get;
        set => Set(ref field, value);
    }

    [JsonPropertyName("wind")]
    public double WindProductionMegaWatt
    {
        get;
        set => Set(ref field, value);
    }
}
