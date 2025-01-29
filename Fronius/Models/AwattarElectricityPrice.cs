namespace De.Hochstaetter.Fronius.Models;

[SuppressMessage("ReSharper", "StringLiteralTypo")]
public class AwattarElectricityPrice : AwattarBase, IElectricityPrice
{
    [JsonPropertyName("marketprice")]
    public decimal EuroPerMegaWattHour
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(CentsPerKiloWattHour)));
    }

    [JsonIgnore]
    public decimal CentsPerKiloWattHour
    {
        get => EuroPerMegaWattHour / 10;
        set => EuroPerMegaWattHour = 10 * value;
    }

    //private string? unitName;
    //[JsonPropertyName("unit")]
    //public string? UnitName
    //{
    //    get => unitName;
    //    set => Set(ref unitName, value);
    //}

    public object Clone() => MemberwiseClone();
    
    #if DEBUG
    public override string ToString() => $"{StartTime.ToLocalTime():g}: {CentsPerKiloWattHour:N2} ct/kWh";
    #endif
}
