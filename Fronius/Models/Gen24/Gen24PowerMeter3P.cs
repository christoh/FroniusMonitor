namespace De.Hochstaetter.Fronius.Models.Gen24;

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

[SuppressMessage("ReSharper", "StringLiteralTypo")]
public partial class Gen24PowerMeter3P : Gen24DeviceBase, IPowerMeter3P
{
    public double? EnergyActiveProducedL3 => null;
    public double? EnergyActiveConsumedL1 => null;
    public double? EnergyActiveConsumedL2 => null;
    public double? EnergyActiveConsumedL3 => null;
    public double? EnergyApparentProducedL1 => null;
    public double? EnergyApparentProducedL2 => null;
    public double? EnergyApparentProducedL3 => null;
    public double? EnergyApparentConsumedL1 => null;
    public double? EnergyApparentConsumedL2 => null;
    public double? EnergyApparentConsumedL3 => null;
    public double? EnergyActiveProducedL1 => null;
    public double? EnergyActiveProducedL2 => null;
    public double? EnergyReactiveConsumedL1 => null;
    public double? EnergyReactiveConsumedL2 => null;
    public double? EnergyReactiveConsumedL3 => null;
    public double? EnergyReactiveProducedL1 => null;
    public double? EnergyReactiveProducedL2 => null;
    public double? EnergyReactiveProducedL3 => null;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(OutOfBalanceCurrentL12), nameof(OutOfBalanceCurrentL31), nameof(OutOfBalanceCurrentMax))]
    [FroniusProprietaryImport("SMARTMETER_CURRENT_01_F64")]
    public partial double? CurrentL1 { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(OutOfBalanceCurrentL12), nameof(OutOfBalanceCurrentL23), nameof(OutOfBalanceCurrentMax))]
    [FroniusProprietaryImport("SMARTMETER_CURRENT_02_F64")]
    public partial double? CurrentL2 { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(OutOfBalanceCurrentL23), nameof(OutOfBalanceCurrentL31), nameof(OutOfBalanceCurrentMax))]
    [FroniusProprietaryImport("SMARTMETER_CURRENT_03_F64")]
    public partial double? CurrentL3 { get; set; }

    public double? OutOfBalanceCurrentL12 => Math.Abs((CurrentL1 - CurrentL2) ?? double.NaN);
    public double? OutOfBalanceCurrentL23 => Math.Abs((CurrentL3 - CurrentL2) ?? double.NaN);
    public double? OutOfBalanceCurrentL31 => Math.Abs((CurrentL1 - CurrentL3) ?? double.NaN);
    public double? OutOfBalanceCurrentMax => new[] { OutOfBalanceCurrentL12, OutOfBalanceCurrentL23, OutOfBalanceCurrentL31 }.Max();

    [ObservableProperty]
    [FroniusProprietaryImport("SMARTMETER_CURRENT_AC_SUM_NOW_F64")]
    public partial double? TotalCurrent { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("SMARTMETER_ENERGYACTIVE_ABSOLUT_MINUS_F64")]
    public partial double? EnergyRealAbsoluteMinus { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("SMARTMETER_ENERGYACTIVE_ABSOLUT_PLUS_F64")]
    public partial double? EnergyRealAbsolutePlus { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("SMARTMETER_ENERGYACTIVE_CONSUMED_SUM_F64")]
    public partial double? EnergyActiveConsumed { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("SMARTMETER_ENERGYACTIVE_PRODUCED_SUM_F64")]
    public partial double? EnergyActiveProduced { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("SMARTMETER_ENERGYREACTIVE_CONSUMED_SUM_F64")]
    public partial double? EnergyReactiveConsumed { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("SMARTMETER_ENERGYREACTIVE_PRODUCED_SUM_F64")]
    public partial double? EnergyReactiveProduced { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("SMARTMETER_FACTOR_POWER_01_F64")]
    public partial double? PowerFactorL1 { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("SMARTMETER_FACTOR_POWER_02_F64")]
    public partial double? PowerFactorL2 { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("SMARTMETER_FACTOR_POWER_03_F64")]
    public partial double? PowerFactorL3 { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("SMARTMETER_FACTOR_POWER_SUM_F64")]
    public partial double? PowerFactorTotal { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("SMARTMETER_FREQUENCY_MEAN_F64")]
    public partial double? Frequency { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(OutOfBalancePowerL12), nameof(OutOfBalancePowerL31), nameof(OutOfBalancePowerMax))]
    [FroniusProprietaryImport("SMARTMETER_POWERACTIVE_01_F64")]
    public partial double? ActivePowerL1 { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(OutOfBalancePowerL12), nameof(OutOfBalancePowerL23), nameof(OutOfBalancePowerMax))]
    [FroniusProprietaryImport("SMARTMETER_POWERACTIVE_02_F64")]
    public partial double? ActivePowerL2 { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(OutOfBalancePowerL23), nameof(OutOfBalancePowerL31), nameof(OutOfBalancePowerMax))]
    [FroniusProprietaryImport("SMARTMETER_POWERACTIVE_03_F64")]
    public partial double? ActivePowerL3 { get; set; }

    public double? OutOfBalancePowerL12 => Math.Abs((ActivePowerL1 - ActivePowerL2) ?? double.NaN);
    public double? OutOfBalancePowerL23 => Math.Abs((ActivePowerL3 - ActivePowerL2) ?? double.NaN);
    public double? OutOfBalancePowerL31 => Math.Abs((ActivePowerL1 - ActivePowerL3) ?? double.NaN);
    public double? OutOfBalancePowerMax => new[] { OutOfBalancePowerL12, OutOfBalancePowerL23, OutOfBalancePowerL31 }.Max();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ActivePowerSumMean))]
    [FroniusProprietaryImport("SMARTMETER_POWERACTIVE_MEAN_01_F64")]
    public partial double? ActivePowerL1Mean { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ActivePowerSumMean))]
    [FroniusProprietaryImport("SMARTMETER_POWERACTIVE_MEAN_02_F64")]
    public partial double? ActivePowerL2Mean { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ActivePowerSumMean))]
    [FroniusProprietaryImport("SMARTMETER_POWERACTIVE_MEAN_03_F64")]
    public partial double? ActivePowerL3Mean { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("SMARTMETER_POWERACTIVE_MEAN_SUM_F64")]
    public partial double? ActivePowerSum { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("SMARTMETER_POWERAPPARENT_01_F64")]
    public partial double? ApparentPowerL1 { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("SMARTMETER_POWERAPPARENT_02_F64")]
    public partial double? ApparentPowerL2 { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("SMARTMETER_POWERAPPARENT_03_F64")]
    public partial double? ApparentPowerL3 { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("SMARTMETER_POWERAPPARENT_MEAN_SUM_F64")]
    public partial double? ApparentPowerSum { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ApparentPowerSumMean))]
    [FroniusProprietaryImport("SMARTMETER_POWERAPPARENT_MEAN_01_F64")]
    public partial double? ApparentPowerL1Mean { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ApparentPowerSumMean))]
    [FroniusProprietaryImport("SMARTMETER_POWERAPPARENT_MEAN_02_F64")]
    public partial double? ApparentPowerL2Mean { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ApparentPowerSumMean))]
    [FroniusProprietaryImport("SMARTMETER_POWERAPPARENT_MEAN_03_F64")]
    public partial double? ApparentPowerL3Mean { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("SMARTMETER_POWERREACTIVE_01_F64")]
    public partial double? ReactivePowerL1 { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("SMARTMETER_POWERREACTIVE_02_F64")]
    public partial double? ReactivePowerL2 { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("SMARTMETER_POWERREACTIVE_03_F64")]
    public partial double? ReactivePowerL3 { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("SMARTMETER_POWERREACTIVE_MEAN_SUM_F64")]
    public partial double? ReactivePowerSum { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("SMARTMETER_VALUE_LOCATION_U16")]
    [NotifyPropertyChangedFor(nameof(Location),nameof(Usage))]
    public partial ushort? MeterLocationCurrent { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("SMARTMETER_VOLTAGE_01_F64")]
    [NotifyPropertyChangedFor(nameof(PhaseVoltageAverage))]
    public partial double? PhaseVoltageL1 { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PhaseVoltageAverage))]
    [FroniusProprietaryImport("SMARTMETER_VOLTAGE_02_F64")]
    public partial double? PhaseVoltageL2 { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PhaseVoltageAverage))]
    [FroniusProprietaryImport("SMARTMETER_VOLTAGE_03_F64")]
    public partial double? PhaseVoltageL3 { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PhaseVoltageAverageMean))]
    [FroniusProprietaryImport("SMARTMETER_VOLTAGE_MEAN_01_F64")]
    public partial double? PhaseVoltageL1Mean { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PhaseVoltageAverageMean))]
    [FroniusProprietaryImport("SMARTMETER_VOLTAGE_MEAN_02_F64")]
    public partial double? PhaseVoltageL2Mean { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PhaseVoltageAverageMean))]
    [FroniusProprietaryImport("SMARTMETER_VOLTAGE_MEAN_03_F64")]
    public partial double? PhaseVoltageL3Mean { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LineVoltageAverage))]
    [FroniusProprietaryImport("SMARTMETER_VOLTAGE_MEAN_12_F64")]
    public partial double? LineVoltageL12 { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LineVoltageAverage))]
    [FroniusProprietaryImport("SMARTMETER_VOLTAGE_MEAN_23_F64")]
    public partial double? LineVoltageL23 { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LineVoltageAverage))]
    [FroniusProprietaryImport("SMARTMETER_VOLTAGE_MEAN_31_F64")]
    public partial double? LineVoltageL31 { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("Manufacturer Serial Number", FroniusDataType.Attribute)]
    public partial string? OemSerialNumber { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("Production Year", FroniusDataType.Attribute)]
    public partial ushort? ProductionYear { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("category", FroniusDataType.Attribute)]
    public partial string? Category { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("label", FroniusDataType.Attribute)]
    public partial string? Label { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("phaseCnt", FroniusDataType.Attribute)]
    public partial byte PhaseCount { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("this.rev-sw", FroniusDataType.Attribute)]
    public partial Version? SoftwareVersion { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("this.rev-hw", FroniusDataType.Attribute)]
    public partial Version? HardwareVersion { get; set; }

    public MeterLocation Location => MeterLocationCurrent == 0 ? MeterLocation.Grid : MeterLocation.Load;
    public MeterUsage Usage => MeterLocationCurrent < 2 ? MeterUsage.Inverter : MeterLocationCurrent > 255 ? MeterUsage.UniqueConsumer : MeterUsage.MultipleConsumers;

    public double? ActivePowerSumMean => ActivePowerL1Mean + ActivePowerL2Mean + ActivePowerL3Mean;
    public double? ApparentPowerSumMean => ApparentPowerL1Mean + ApparentPowerL2Mean + ApparentPowerL3Mean;

    public double? PhaseVoltageAverage => (PhaseVoltageL1 + PhaseVoltageL2 + PhaseVoltageL3) / 3;
    public double? PhaseVoltageAverageMean => (PhaseVoltageL1Mean + PhaseVoltageL2Mean + PhaseVoltageL3Mean) / 3;
    public double? LineVoltageAverage => (LineVoltageL12 + LineVoltageL23 + LineVoltageL31) / 3;
}