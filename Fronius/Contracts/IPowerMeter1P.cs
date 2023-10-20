namespace De.Hochstaetter.Fronius.Contracts;

public interface IPowerMeter1P : IHaveDisplayName, IHaveUniqueId
{
    double? ActivePower => Current * Voltage;
    double? Voltage => ActivePower / Current;
    double? Current => ActivePower / Voltage;
    double? Frequency { get; }
    double? EnergyConsumed { get; }
    bool CanMeasurePower { get; }
    string? DeviceVersion { get; }
}