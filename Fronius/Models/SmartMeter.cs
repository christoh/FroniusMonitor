namespace De.Hochstaetter.Fronius.Models;

public enum MeterUsage : sbyte
{
    MultipleConsumers,
    UniqueConsumer,
    Inverter,
}

public enum MeterLocation : sbyte
{
    Grid,
    Load,
    Unknown,
}

public class SmartMeterData
{
    public double L1Current { get; init; }
    public double L2Current { get; init; }
    public double L3Current { get; init; }
    public double TotalCurrent { get; init; }
    public bool IsEnabled { get; init; }
    public double ReactiveEnergyConsumedWattHours { get; init; }
    public double ReactiveEnergyProducedWattHours { get; init; }
    public double ReactiveEnergyConsumedKiloWattHours => ReactiveEnergyConsumedWattHours / 1000;
    public double ReactiveEnergyProducedKiloWattHours => ReactiveEnergyProducedWattHours / 1000;
    public double RealEnergyConsumedWattHours { get; init; }
    public double RealEnergyProducedWattHours { get; init; }
    public double RealEnergyConsumedKiloWattHours => RealEnergyConsumedWattHours / 1000;
    public double RealEnergyProducedKiloWattHours => RealEnergyProducedWattHours / 1000;
    public double RealEnergyAbsolutePlusWattHours { get; init; }
    public double RealEnergyAbsoluteMinusWattHours { get; init; }
    public double RealEnergyAbsolutePlusKiloWattHours => RealEnergyAbsolutePlusWattHours / 1000;
    public double RealEnergyAbsoluteMinusKiloWattHours => RealEnergyAbsoluteMinusWattHours / 1000;
    public double Frequency { get; init; }
    public double L1ApparentPower { get; init; }
    public double L2ApparentPower { get; init; }
    public double L3ApparentPower { get; init; }
    public double TotalApparentPower { get; init; }
    public double L1PowerFactor { get; init; }
    public double L2PowerFactor { get; init; }
    public double L3PowerFactor { get; init; }
    public double TotalPowerFactor { get; init; }
    public double L1ReactivePower { get; init; }
    public double L2ReactivePower { get; init; }
    public double L3ReactivePower { get; init; }
    public double TotalReactivePower { get; init; }
    public double L1RealPower { get; init; }
    public double L2RealPower { get; init; }
    public double L3RealPower { get; init; }
    public double TotalRealPower { get; init; }
    public DateTime MeterTimestamp { get; init; }
    public bool IsVisible { get; init; }
    public double L1L2Voltage { get; init; }
    public double L2L3Voltage { get; init; }
    public double L3L1Voltage { get; init; }
    public double L1Voltage { get; init; }
    public double L2Voltage { get; init; }
    public double L3Voltage { get; init; }
    public double AverageTwoPhasesVoltage => new[] { L1L2Voltage, L2L3Voltage, L3L1Voltage }.Average();
    public double AverageVoltage => new[] { L1Voltage, L2Voltage, L3Voltage }.Average();
    public double L1L2OutOfBalancePower => Math.Abs(L1RealPower - L2RealPower);
    public double L2L3OutOfBalancePower => Math.Abs(L2RealPower - L3RealPower);
    public double L3L1OutOfBalancePower => Math.Abs(L3RealPower - L1RealPower);
    public double MaxOutOfBalancePower => new[] { L1L2OutOfBalancePower, L2L3OutOfBalancePower, L3L1OutOfBalancePower }.Max();
    public double L1L2OutOfBalanceCurrent => Math.Abs(L1Current - L2Current);
    public double L2L3OutOfBalanceCurrent => Math.Abs(L2Current - L3Current);
    public double L3L1OutOfBalanceCurrent => Math.Abs(L3Current - L1Current);
    public double MaxOutOfBalanceCurrent => new[] { L1L2OutOfBalanceCurrent, L2L3OutOfBalanceCurrent, L3L1OutOfBalanceCurrent }.Max();
}

public class SmartMeter : DeviceInfo
{
    public SmartMeter()
    {
        DeviceClass = DeviceClass.Meter;
    }

    private string manufacturer = string.Empty;

    public string Manufacturer
    {
        get => manufacturer;
        set => Set(ref manufacturer, value, () => NotifyOfPropertyChange(nameof(DisplayName)));
    }

    private string model = string.Empty;

    public override string Model
    {
        get => model;
        set => Set(ref model, value, () => NotifyOfPropertyChange(nameof(DisplayName)));
    }

    private int meterLocationCurrent;

    public int MeterLocationCurrent
    {
        get => meterLocationCurrent;
        set => Set(ref meterLocationCurrent, value, () =>
        {
            NotifyOfPropertyChange(nameof(Location));
            NotifyOfPropertyChange(nameof(Usage));
        });
    }

    private SmartMeterData? data;

    public SmartMeterData? Data
    {
        get => data;
        set => Set(ref data, value);
    }

    public MeterLocation Location => MeterLocationCurrent == 0 ? MeterLocation.Grid : MeterLocation.Load;
    public MeterUsage Usage => MeterLocationCurrent < 2 ? MeterUsage.Inverter : MeterLocationCurrent > 255 ? MeterUsage.UniqueConsumer : MeterUsage.MultipleConsumers;
    public override string DisplayName => $"{Manufacturer} {Model} #{Id}";

    public void NotifySettingsChanged()
    {
        NotifyOfPropertyChange(nameof(Data));
    }
}