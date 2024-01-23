namespace De.Hochstaetter.Fronius.Models.Charging;

public record WattPilotLoadBalancingCurrents : BindableRecordBase
{
    private int? localMaximumCurrent;

    [WattPilot("amp", false)]
    public int? LocalMaximumCurrent
    {
        get => localMaximumCurrent;
        set => Set(ref localMaximumCurrent, value);
    }

    private int? dynamicMaximumCurrent;

    [WattPilot("dyn", false)]
    public int? DynamicMaximumCurrent
    {
        get => dynamicMaximumCurrent;
        set => Set(ref dynamicMaximumCurrent, value);
    }

    private int? maximumCurrentDnoLine;

    [WattPilot("sta", false)]
    public int? MaximumCurrentDnoLine
    {
        get => maximumCurrentDnoLine;
        set => Set(ref maximumCurrentDnoLine, value);
    }

    private long? timeStampEpoch;

    [WattPilot("ts", false)]
    public long? TimeStampEpoch
    {
        get => timeStampEpoch;
        set => Set(ref timeStampEpoch, value, () => NotifyOfPropertyChange(nameof(TimeStamp)));
    }

    public DateTime? TimeStamp
    {
        get => TimeStampEpoch.HasValue ? DateTime.UnixEpoch.AddSeconds(TimeStampEpoch.Value) : null;
        set => TimeStampEpoch = value.HasValue ? (long)Math.Round((value.Value - DateTime.UnixEpoch).TotalSeconds, MidpointRounding.AwayFromZero) : null;
    }
}
