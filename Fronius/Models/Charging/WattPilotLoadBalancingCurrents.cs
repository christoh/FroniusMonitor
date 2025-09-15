using Newtonsoft.Json;

namespace De.Hochstaetter.Fronius.Models.Charging;

public partial class WattPilotLoadBalancingCurrents : BindableBase, ICloneable
{
    [ObservableProperty]
    [JsonProperty("amp")]
    [WattPilot("amp", false)]
    public partial int LocalMaximumCurrent { get; set; }

    [ObservableProperty]
    [JsonProperty("dyn")]
    [WattPilot("dyn", false)]
    public partial int DynamicMaximumCurrent { get; set; }

    [ObservableProperty]
    [JsonProperty("sta")]
    [WattPilot("sta", false)]
    public partial int MaximumCurrentDnoLine { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TimeStamp))]
    [JsonProperty("ts")]
    [WattPilot("ts", false)]
    public partial long TimeStampEpoch { get; set; }

    [Newtonsoft.Json.JsonIgnore]
    public DateTime TimeStamp
    {
        get => DateTime.UnixEpoch.AddSeconds(TimeStampEpoch);
        set => TimeStampEpoch = (long)Math.Round((value - DateTime.UnixEpoch).TotalSeconds, MidpointRounding.AwayFromZero);
    }

    public override bool Equals(object? obj)
    {
        return
            ReferenceEquals(this, obj) ||
            obj is WattPilotLoadBalancingCurrents other &&
            LocalMaximumCurrent.Equals(other.LocalMaximumCurrent) &&
            DynamicMaximumCurrent.Equals(other.DynamicMaximumCurrent) &&
            TimeStampEpoch.Equals(other.TimeStampEpoch) &&
            MaximumCurrentDnoLine.Equals(other.MaximumCurrentDnoLine);
    }

    public override int GetHashCode() => LocalMaximumCurrent ^ DynamicMaximumCurrent ^ MaximumCurrentDnoLine ^ TimeStampEpoch.GetHashCode();

    public object Clone()
    {
        return new WattPilotLoadBalancingCurrents
        {
            LocalMaximumCurrent = LocalMaximumCurrent,
            DynamicMaximumCurrent = DynamicMaximumCurrent,
            TimeStampEpoch = TimeStampEpoch,
            MaximumCurrentDnoLine = MaximumCurrentDnoLine,
        };
    }

    public WattPilotLoadBalancingCurrents Copy() => (WattPilotLoadBalancingCurrents)Clone();

    public static bool operator==(WattPilotLoadBalancingCurrents? left, WattPilotLoadBalancingCurrents? right) => left?.Equals(right) ?? right is null;
    
    public static bool operator!=(WattPilotLoadBalancingCurrents? left, WattPilotLoadBalancingCurrents? right) => !(left == right);
}
