namespace De.Hochstaetter.Fronius.Models.Charging;

public class WattPilotAcknowledge
{
    public ManualResetEventSlim Event { get; } = new();
    public uint RequestId { get; init; }
    public PropertyInfo PropertyInfo { get; init; } = null!;
    public object? Value { get; init; }

    public override string ToString() => $"{PropertyInfo.Name} = '{Value}'";
}