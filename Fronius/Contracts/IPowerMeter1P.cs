namespace De.Hochstaetter.Fronius.Contracts;

public interface IPowerMeter1P : ISwitchable
{
    double? PowerWatts => Current * Voltage;
    double? Voltage => PowerWatts / Current;
    double? Current => PowerWatts / Voltage;
    double? Temperature { get; }
    double? Frequency { get; }
    double? EnergyKiloWattHours { get; }
    bool CanReadPower { get; }
}