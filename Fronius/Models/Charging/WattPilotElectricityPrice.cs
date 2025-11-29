using Newtonsoft.Json;

namespace De.Hochstaetter.Fronius.Models.Charging;

[SuppressMessage("ReSharper", "StringLiteralTypo")]
public partial class WattPilotElectricityPrice : BindableBase, IElectricityPrice
{
    [ObservableProperty]
    [JsonProperty("marketprice")]
    [WattPilot("marketprice")]
    public partial decimal CentsPerKiloWattHour { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StartTime))]
    [JsonProperty("start")]
    [WattPilot("start")]
    public partial long StartSeconds { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public DateTime StartTime
    {
        get => DateTime.UnixEpoch.AddSeconds(StartSeconds);
        set => StartSeconds = (long)Math.Round((value - DateTime.UnixEpoch).TotalSeconds, MidpointRounding.AwayFromZero);
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EndTime))]
    [JsonProperty("end")]
    [WattPilot("end")]
    public partial long EndSeconds { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public DateTime EndTime
    {
        get => DateTime.UnixEpoch.AddSeconds(EndSeconds);
        set => EndSeconds = (long)Math.Round((value - DateTime.UnixEpoch).TotalSeconds, MidpointRounding.AwayFromZero);
    }

    public object Clone() => MemberwiseClone();
        
    #if DEBUG
    public override string ToString() => $"{StartTime.ToLocalTime():g}: {CentsPerKiloWattHour:N2} ct/kWh";
    #endif
}