using Newtonsoft.Json;

namespace De.Hochstaetter.Fronius.Models.Charging;

public class WattPilotLoadBalancingCurrents : BindableBase, ICloneable
{
    private int localMaximumCurrent;

    [JsonProperty("amp")]
    [WattPilot("amp", false)]
    public int LocalMaximumCurrent
    {
        get => localMaximumCurrent;
        set => Set(ref localMaximumCurrent, value);
    }

    private int dynamicMaximumCurrent;

    [JsonProperty("dyn")]
    [WattPilot("dyn", false)]
    public int DynamicMaximumCurrent
    {
        get => dynamicMaximumCurrent;
        set => Set(ref dynamicMaximumCurrent, value);
    }

    private int maximumCurrentDnoLine;

    [JsonProperty("sta")]
    [WattPilot("sta", false)]
    public int MaximumCurrentDnoLine
    {
        get => maximumCurrentDnoLine;
        set => Set(ref maximumCurrentDnoLine, value);
    }

    private long timeStampEpoch;

    [JsonProperty("ts")]
    [WattPilot("ts", false)]
    public long TimeStampEpoch
    {
        get => timeStampEpoch;
        set => Set(ref timeStampEpoch, value, () => NotifyOfPropertyChange(nameof(TimeStamp)));
    }

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
