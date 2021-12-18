using De.Hochstaetter.Fronius.Localization;

namespace De.Hochstaetter.Fronius.Models;

public class Storage : DeviceInfo
{
    public override string DisplayName => $"{StorageModel ?? Resources.Unknown} #{Id}";
    public string? Manufacturer { get; init; }
    public string? StorageModel { get; init; }
    public double MaximumCapacityWattHours { get; init; } = double.NaN;
    public double Current { get; init; } = double.NaN;
    public double DesignedCapacityWattHours { get; init; }
    public double DesignedCapacityKiloWattHours => DesignedCapacityWattHours / 1000;
    public bool IsEnabled { get; init; }
    public double StateOfCharge { get; init; } = double.NaN;
    public int StatusBatteryCell { get; init; }
    public double TemperatureCelsius { get; init; }
    public DateTime StorageTimestamp { get; init; }
    public double Voltage { get; init; }
    public double Power => Voltage * Current;
    public double RemainingCapacityWattHours => StateOfCharge * MaximumCapacityWattHours;
    public double RemainingCapacityKiloWattHours => RemainingCapacityWattHours / 1000;

    public string StatusString
    {
        get
        {
            return Manufacturer switch
            {
                "BYD" => StatusBatteryCell switch
                {
                    0 => Resources.Standby,
                    1 => Resources.Inactive,
                    2 => Resources.Starting,
                    3 => Resources.Active,
                    4 => Resources.Error,
                    5 => Resources.Updating,
                    _ => Resources.Unknown
                },
                "LG-Chem" => StatusBatteryCell switch
                {
                    1 => Resources.Standby,
                    3 => Resources.Active,
                    5 => Resources.Error,
                    10 => Resources.Sleeping,
                    _ => Resources.Unknown
                },
                _ => Resources.Unknown
            };
        }
    }
}
