namespace De.Hochstaetter.Fronius.Models;

public class AwattarEnergy : AwattarBase
{
    private double solarProduction;
    [JsonPropertyName("solar")]
    public double SolarProduction
    {
        get => solarProduction;
        set => Set(ref solarProduction, value);
    }

    private double windProduction;
    [JsonPropertyName("wind")]
    public double WindProduction
    {
        get => windProduction;
        set => Set(ref windProduction, value);
    }
}
