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
public class Gen24PowerMeter3P : Gen24DeviceBase, IPowerMeter3P
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

    [FroniusProprietaryImport("SMARTMETER_CURRENT_01_F64")]
    public double? CurrentL1
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("SMARTMETER_CURRENT_02_F64")]
    public double? CurrentL2
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("SMARTMETER_CURRENT_03_F64")]
    public double? CurrentL3
    {
        get;
        set => Set(ref field, value);
    }

    public double? OutOfBalanceCurrentL12 => Math.Abs((CurrentL1 - CurrentL2) ?? double.NaN);
    public double? OutOfBalanceCurrentL23 => Math.Abs((CurrentL3 - CurrentL2) ?? double.NaN);
    public double? OutOfBalanceCurrentL31 => Math.Abs((CurrentL1 - CurrentL3) ?? double.NaN);
    public double? OutOfBalanceCurrentMax => new[] { OutOfBalanceCurrentL12, OutOfBalanceCurrentL23, OutOfBalanceCurrentL31 }.Max();

    [FroniusProprietaryImport("SMARTMETER_CURRENT_AC_SUM_NOW_F64")]
    public double? TotalCurrent
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("SMARTMETER_ENERGYACTIVE_ABSOLUT_MINUS_F64")]
    public double? EnergyRealAbsoluteMinus
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("SMARTMETER_ENERGYACTIVE_ABSOLUT_PLUS_F64")]
    public double? EnergyRealAbsolutePlus
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("SMARTMETER_ENERGYACTIVE_CONSUMED_SUM_F64")]
    public double? EnergyActiveConsumed
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("SMARTMETER_ENERGYACTIVE_PRODUCED_SUM_F64")]
    public double? EnergyActiveProduced
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("SMARTMETER_ENERGYREACTIVE_CONSUMED_SUM_F64")]
    public double? EnergyReactiveConsumed
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("SMARTMETER_ENERGYREACTIVE_PRODUCED_SUM_F64")]
    public double? EnergyReactiveProduced
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("SMARTMETER_FACTOR_POWER_01_F64")]
    public double? PowerFactorL1
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("SMARTMETER_FACTOR_POWER_02_F64")]
    public double? PowerFactorL2
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("SMARTMETER_FACTOR_POWER_03_F64")]
    public double? PowerFactorL3
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("SMARTMETER_FACTOR_POWER_SUM_F64")]
    public double? PowerFactorTotal
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("SMARTMETER_FREQUENCY_MEAN_F64")]
    public double? Frequency
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("SMARTMETER_POWERACTIVE_01_F64")]
    public double? ActivePowerL1
    {
        get;
        set => Set(ref field, value, () =>
        {
            NotifyOfPropertyChange(nameof(OutOfBalancePowerL12));
            NotifyOfPropertyChange(nameof(OutOfBalancePowerL31));
            NotifyOfPropertyChange(nameof(OutOfBalancePowerMax));
        });
    }

    [FroniusProprietaryImport("SMARTMETER_POWERACTIVE_02_F64")]
    public double? ActivePowerL2
    {
        get;
        set => Set(ref field, value, () =>
        {
            NotifyOfPropertyChange(nameof(OutOfBalancePowerL12));
            NotifyOfPropertyChange(nameof(OutOfBalancePowerL23));
            NotifyOfPropertyChange(nameof(OutOfBalancePowerMax));
        });
    }

    [FroniusProprietaryImport("SMARTMETER_POWERACTIVE_03_F64")]
    public double? ActivePowerL3
    {
        get;
        set => Set(ref field, value, () =>
        {
            NotifyOfPropertyChange(nameof(OutOfBalancePowerL23));
            NotifyOfPropertyChange(nameof(OutOfBalancePowerL31));
            NotifyOfPropertyChange(nameof(OutOfBalancePowerMax));
        });
    }

    public double? OutOfBalancePowerL12 => Math.Abs((ActivePowerL1 - ActivePowerL2) ?? double.NaN);
    public double? OutOfBalancePowerL23 => Math.Abs((ActivePowerL3 - ActivePowerL2) ?? double.NaN);
    public double? OutOfBalancePowerL31 => Math.Abs((ActivePowerL1 - ActivePowerL3) ?? double.NaN);
    public double? OutOfBalancePowerMax => new[] {OutOfBalancePowerL12, OutOfBalancePowerL23, OutOfBalancePowerL31}.Max();

    [FroniusProprietaryImport("SMARTMETER_POWERACTIVE_MEAN_01_F64")]
    public double? ActivePowerL1Mean
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("SMARTMETER_POWERACTIVE_MEAN_02_F64")]
    public double? ActivePowerL2Mean
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(ActivePowerSumMean)));
    }

    [FroniusProprietaryImport("SMARTMETER_POWERACTIVE_MEAN_03_F64")]
    public double? ActivePowerL3Mean
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(ActivePowerSumMean)));
    }

    [FroniusProprietaryImport("SMARTMETER_POWERACTIVE_MEAN_SUM_F64")]
    public double? ActivePowerSum
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(ActivePowerSumMean)));
    }

    [FroniusProprietaryImport("SMARTMETER_POWERAPPARENT_01_F64")]
    public double? ApparentPowerL1
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("SMARTMETER_POWERAPPARENT_02_F64")]
    public double? ApparentPowerL2
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("SMARTMETER_POWERAPPARENT_03_F64")]
    public double? ApparentPowerL3
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("SMARTMETER_POWERAPPARENT_MEAN_SUM_F64")]
    public double? ApparentPowerSum
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("SMARTMETER_POWERAPPARENT_MEAN_01_F64")]
    public double? ApparentPowerL1Mean
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(ApparentPowerSumMean)));
    }

    [FroniusProprietaryImport("SMARTMETER_POWERAPPARENT_MEAN_02_F64")]
    public double? ApparentPowerL2Mean
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(ApparentPowerSumMean)));
    }

    [FroniusProprietaryImport("SMARTMETER_POWERAPPARENT_MEAN_03_F64")]
    public double? ApparentPowerL3Mean
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(ApparentPowerSumMean)));
    }

    [FroniusProprietaryImport("SMARTMETER_POWERREACTIVE_01_F64")]
    public double? ReactivePowerL1
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("SMARTMETER_POWERREACTIVE_02_F64")]
    public double? ReactivePowerL2
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("SMARTMETER_POWERREACTIVE_03_F64")]
    public double? ReactivePowerL3
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("SMARTMETER_POWERREACTIVE_MEAN_SUM_F64")]
    public double? ReactivePowerSum
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("SMARTMETER_VALUE_LOCATION_U16")]
    public ushort? MeterLocationCurrent
    {
        get;
        set => Set(ref field, value, () =>
        {
            NotifyOfPropertyChange(nameof(Location));
            NotifyOfPropertyChange(nameof(Usage));
        });
    }

    [FroniusProprietaryImport("SMARTMETER_VOLTAGE_01_F64")]
    public double? PhaseVoltageL1
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(PhaseVoltageAverage)));
    }

    [FroniusProprietaryImport("SMARTMETER_VOLTAGE_02_F64")]
    public double? PhaseVoltageL2
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(PhaseVoltageAverage)));
    }

    [FroniusProprietaryImport("SMARTMETER_VOLTAGE_03_F64")]
    public double? PhaseVoltageL3
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(PhaseVoltageAverage)));
    }

    [FroniusProprietaryImport("SMARTMETER_VOLTAGE_MEAN_01_F64")]
    public double? PhaseVoltageL1Mean
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(PhaseVoltageAverageMean)));
    }

    [FroniusProprietaryImport("SMARTMETER_VOLTAGE_MEAN_02_F64")]
    public double? PhaseVoltageL2Mean
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(PhaseVoltageAverageMean)));
    }

    [FroniusProprietaryImport("SMARTMETER_VOLTAGE_MEAN_03_F64")]
    public double? PhaseVoltageL3Mean
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(PhaseVoltageAverageMean)));
    }

    [FroniusProprietaryImport("SMARTMETER_VOLTAGE_MEAN_12_F64")]
    public double? LineVoltageL12
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(LineVoltageAverage)));
    }

    [FroniusProprietaryImport("SMARTMETER_VOLTAGE_MEAN_23_F64")]
    public double? LineVoltageL23
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(LineVoltageAverage)));
    }

    [FroniusProprietaryImport("SMARTMETER_VOLTAGE_MEAN_31_F64")]
    public double? LineVoltageL31
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(LineVoltageAverage)));
    }

    [FroniusProprietaryImport("Manufacturer Serial Number", FroniusDataType.Attribute)]
    public string? OemSerialNumber
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("Production Year", FroniusDataType.Attribute)]
    public ushort? ProductionYear
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("category", FroniusDataType.Attribute)]
    public string? Category
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("label", FroniusDataType.Attribute)]
    public string? Label
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("phaseCnt", FroniusDataType.Attribute)]
    public byte PhaseCount
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("this.rev-sw", FroniusDataType.Attribute)]
    public Version? SoftwareVersion
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("this.rev-hw", FroniusDataType.Attribute)]
    public Version? HardwareVersion
    {
        get;
        set => Set(ref field, value);
    }

    public MeterLocation Location => MeterLocationCurrent == 0 ? MeterLocation.Grid : MeterLocation.Load;
    public MeterUsage Usage => MeterLocationCurrent < 2 ? MeterUsage.Inverter : MeterLocationCurrent > 255 ? MeterUsage.UniqueConsumer : MeterUsage.MultipleConsumers;

    public double? ActivePowerSumMean => ActivePowerL1Mean + ActivePowerL2Mean + ActivePowerL3Mean;
    public double? ApparentPowerSumMean => ApparentPowerL1Mean + ApparentPowerL2Mean + ApparentPowerL3Mean;

    public double? PhaseVoltageAverage => (PhaseVoltageL1 + PhaseVoltageL2 + PhaseVoltageL3) / 3;
    public double? PhaseVoltageAverageMean => (PhaseVoltageL1Mean + PhaseVoltageL2Mean + PhaseVoltageL3Mean) / 3;
    public double? LineVoltageAverage => (LineVoltageL12 + LineVoltageL23 + LineVoltageL31) / 3;
}