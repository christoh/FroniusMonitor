namespace De.Hochstaetter.Fronius.Contracts;

public interface IPowerMeter1P
{
    double? PowerWatts => Current * Voltage;
    double? Voltage => PowerWatts / Current;
    double? Current => PowerWatts / Voltage;
    double? Frequency { get; }
    double? EnergyKiloWattHours { get; }
    bool CanMeasurePower { get; }
    string? Manufacturer { get; }
    string? Model { get; }
    string? SerialNumber { get; }
}