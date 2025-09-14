namespace De.Hochstaetter.Fronius.Models;

[SuppressMessage("ReSharper", "StringLiteralTypo")]
public partial class AwattarElectricityPrice : AwattarBase, IElectricityPrice
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CentsPerKiloWattHour))]
    [JsonPropertyName("marketprice")]
    public partial decimal EuroPerMegaWattHour { get; set; }

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
