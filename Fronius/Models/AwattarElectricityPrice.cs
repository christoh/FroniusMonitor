namespace De.Hochstaetter.Fronius.Models;

[SuppressMessage("ReSharper", "StringLiteralTypo")]
public class AwattarElectricityPrice : BindableBase, IElectricityPrice
{
    private long startMilliseconds;

    [JsonPropertyName("start_timestamp")]
    public long StartMilliseconds
    {
        get => startMilliseconds;
        set => Set(ref startMilliseconds, value, () => NotifyOfPropertyChange(nameof(StartTime)));
    }

    private long endMilliseconds;

    [JsonPropertyName("end_timestamp")]
    public long EndMilliseconds
    {
        get => endMilliseconds;
        set => Set(ref endMilliseconds, value, () => NotifyOfPropertyChange(nameof(EndTime)));
    }

    [JsonIgnore]
    public DateTime StartTime
    {
        get => DateTime.UnixEpoch.AddMilliseconds(StartMilliseconds);
        set => StartMilliseconds = (long)Math.Round((value.ToUniversalTime() - DateTime.UnixEpoch).TotalMilliseconds, MidpointRounding.AwayFromZero);
    }

    [JsonIgnore]
    public DateTime EndTime
    {
        get => DateTime.UnixEpoch.AddMilliseconds(EndMilliseconds);
        set => EndMilliseconds = (long)Math.Round((value.ToUniversalTime() - DateTime.UnixEpoch).TotalMilliseconds, MidpointRounding.AwayFromZero);
    }

    private decimal euroPerMegaWattHour;

    [JsonPropertyName("marketprice")]
    public decimal EuroPerMegaWattHour
    {
        get => euroPerMegaWattHour;
        set => Set(ref euroPerMegaWattHour, value, () => NotifyOfPropertyChange(nameof(CentsPerKiloWattHour)));
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

    #if DEBUG
    public override string ToString() => $"{StartTime.ToLocalTime():g}: {CentsPerKiloWattHour:N2} ct/kWh";
    #endif
}
