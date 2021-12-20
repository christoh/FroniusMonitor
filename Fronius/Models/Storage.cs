using De.Hochstaetter.Fronius.Localization;

namespace De.Hochstaetter.Fronius.Models;

public class StorageData
{
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
    public double Degradation => 1 - MaximumCapacityWattHours / DesignedCapacityWattHours;
    public string Manufacturer { get; init; } = string.Empty;

    public TrafficLight TrafficLight => Manufacturer switch
    {
        "BYD" => StatusBatteryCell switch
        {
            3 => TrafficLight.Green,
            4 => TrafficLight.Red,
            _ => TrafficLight.Yellow
        },

        "LG-Chem" => StatusBatteryCell switch
        {
            3 => TrafficLight.Green,
            5 => TrafficLight.Red,
            _ => TrafficLight.Yellow,
        },

        _ => TrafficLight.Yellow
    };

    public string StatusString => Manufacturer switch
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

public class Storage : DeviceInfo
{
    public override string DisplayName => $"{Model ?? Resources.Unknown} #{Id}";

    private string model = string.Empty;

    public override string Model
    {
        get => model;
        set => Set(ref model, value, () => NotifyOfPropertyChange(nameof(DisplayName)));
    }

    private StorageData? data;

    public StorageData? Data
    {
        get => data;
        set => Set(ref data, value);
    }
}
