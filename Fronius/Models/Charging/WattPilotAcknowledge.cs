namespace De.Hochstaetter.Fronius.Models.Charging;

public record WattPilotAcknowledge(uint RequestId, PropertyInfo PropertyInfo, object? Value)
{
    public ManualResetEventSlim Event { get; } = new();

    public override string ToString() => $"{PropertyInfo.Name} = '{Value}'";
}