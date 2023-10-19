namespace De.Hochstaetter.Fronius.Contracts;

public interface IPowerMeter1P : IHaveDisplayName
{
    double? ActivePower => Current * Voltage;
    double? Voltage => ActivePower / Current;
    double? Current => ActivePower / Voltage;
    double? Frequency { get; }
    double? EnergyConsumed { get; }
    bool CanMeasurePower { get; }
    string? Manufacturer { get; }
    string? Model { get; }
    string? SerialNumber { get; }
    string? DeviceVersion { get; }
}