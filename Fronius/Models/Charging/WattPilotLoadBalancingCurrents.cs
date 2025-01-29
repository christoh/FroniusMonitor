using Newtonsoft.Json;

namespace De.Hochstaetter.Fronius.Models.Charging;

public class WattPilotLoadBalancingCurrents : BindableBase, ICloneable
{
    [JsonProperty("amp")]
    [WattPilot("amp", false)]
    public int LocalMaximumCurrent
    {
        get;
        set => Set(ref field, value);
    }

    [JsonProperty("dyn")]
    [WattPilot("dyn", false)]
    public int DynamicMaximumCurrent
    {
        get;
        set => Set(ref field, value);
    }

    [JsonProperty("sta")]
    [WattPilot("sta", false)]
    public int MaximumCurrentDnoLine
    {
        get;
        set => Set(ref field, value);
    }

    [JsonProperty("ts")]
    [WattPilot("ts", false)]
    public long TimeStampEpoch
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(TimeStamp)));
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
