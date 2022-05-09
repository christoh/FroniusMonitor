using System.Diagnostics.CodeAnalysis;
using De.Hochstaetter.Fronius.Attributes;

namespace De.Hochstaetter.Fronius.Models.Gen24;

[SuppressMessage("ReSharper", "StringLiteralTypo")]
public class Gen24PowerMeter : Gen24DeviceBase
{
    private double? currentL1;

    [FroniusProprietaryImport("SMARTMETER_CURRENT_01_F64")]
    public double? CurrentL1
    {
        get => currentL1;
        set => Set(ref currentL1, value);
    }

    private double? currentL2;

    [FroniusProprietaryImport("SMARTMETER_CURRENT_02_F64")]
    public double? CurrentL2
    {
        get => currentL2;
        set => Set(ref currentL2, value);
    }

    private double? currentL3;

    [FroniusProprietaryImport("SMARTMETER_CURRENT_03_F64")]
    public double? CurrentL3
    {
        get => currentL3;
        set => Set(ref currentL3, value);
    }

    public double? OutOfBalanceCurrentL12 => Math.Abs((CurrentL1 - CurrentL2) ?? double.NaN);
    public double? OutOfBalanceCurrentL23 => Math.Abs((CurrentL3 - CurrentL2) ?? double.NaN);
    public double? OutOfBalanceCurrentL31 => Math.Abs((CurrentL1 - CurrentL3) ?? double.NaN);
    public double? OutOfBalanceCurrentMax => new[] { OutOfBalanceCurrentL12, OutOfBalanceCurrentL23, OutOfBalanceCurrentL31 }.Max();

    private double? totalCurrent;

    [FroniusProprietaryImport("SMARTMETER_CURRENT_AC_SUM_NOW_F64")]
    public double? TotalCurrent
    {
        get => totalCurrent;
        set => Set(ref totalCurrent, value);
    }

    private double? energyRealAbsoluteMinus;

    [FroniusProprietaryImport("SMARTMETER_ENERGYACTIVE_ABSOLUT_MINUS_F64")]
    public double? EnergyRealAbsoluteMinus
    {
        get => energyRealAbsoluteMinus;
        set => Set(ref energyRealAbsoluteMinus, value);
    }

    private double? energyRealAbsolutePlus;

    [FroniusProprietaryImport("SMARTMETER_ENERGYACTIVE_ABSOLUT_PLUS_F64")]
    public double? EnergyRealAbsolutePlus
    {
        get => energyRealAbsolutePlus;
        set => Set(ref energyRealAbsolutePlus, value);
    }

    private double? energyRealConsumed;

    [FroniusProprietaryImport("SMARTMETER_ENERGYACTIVE_CONSUMED_SUM_F64")]
    public double? EnergyRealConsumed
    {
        get => energyRealConsumed;
        set => Set(ref energyRealConsumed, value);
    }

    private double? energyRealProduced;

    [FroniusProprietaryImport("SMARTMETER_ENERGYACTIVE_PRODUCED_SUM_F64")]
    public double? EnergyRealProduced
    {
        get => energyRealProduced;
        set => Set(ref energyRealProduced, value);
    }

    private double? energyReactiveConsumed;

    [FroniusProprietaryImport("SMARTMETER_ENERGYREACTIVE_CONSUMED_SUM_F64")]
    public double? EnergyReactiveConsumed
    {
        get => energyReactiveConsumed;
        set => Set(ref energyReactiveConsumed, value);
    }

    private double? energyReactiveProduced;

    [FroniusProprietaryImport("SMARTMETER_ENERGYREACTIVE_PRODUCED_SUM_F64")]
    public double? EnergyReactiveProduced
    {
        get => energyReactiveProduced;
        set => Set(ref energyReactiveProduced, value);
    }

    private double? powerFactorL1;

    [FroniusProprietaryImport("SMARTMETER_FACTOR_POWER_01_F64")]
    public double? PowerFactorL1
    {
        get => powerFactorL1;
        set => Set(ref powerFactorL1, value);
    }

    private double? powerFactorL2;

    [FroniusProprietaryImport("SMARTMETER_FACTOR_POWER_02_F64")]
    public double? PowerFactorL2
    {
        get => powerFactorL2;
        set => Set(ref powerFactorL2, value);
    }

    private double? powerFactorL3;

    [FroniusProprietaryImport("SMARTMETER_FACTOR_POWER_03_F64")]
    public double? PowerFactorL3
    {
        get => powerFactorL3;
        set => Set(ref powerFactorL3, value);
    }

    private double? powerFactorTotal;

    [FroniusProprietaryImport("SMARTMETER_FACTOR_POWER_SUM_F64")]
    public double? PowerFactorTotal
    {
        get => powerFactorTotal;
        set => Set(ref powerFactorTotal, value);
    }

    private double? frequency;

    [FroniusProprietaryImport("SMARTMETER_FREQUENCY_MEAN_F64")]
    public double? Frequency
    {
        get => frequency;
        set => Set(ref frequency, value);
    }

    private double? realPowerL1;

    [FroniusProprietaryImport("SMARTMETER_POWERACTIVE_01_F64")]
    public double? RealPowerL1
    {
        get => realPowerL1;
        set => Set(ref realPowerL1, value, () =>
        {
            NotifyOfPropertyChange(nameof(OutOfBalancePowerL12));
            NotifyOfPropertyChange(nameof(OutOfBalancePowerL31));
            NotifyOfPropertyChange(nameof(OutOfBalancePowerMax));
        });
    }

    private double? realPowerL2;

    [FroniusProprietaryImport("SMARTMETER_POWERACTIVE_02_F64")]
    public double? RealPowerL2
    {
        get => realPowerL2;
        set => Set(ref realPowerL2, value, () =>
        {
            NotifyOfPropertyChange(nameof(OutOfBalancePowerL12));
            NotifyOfPropertyChange(nameof(OutOfBalancePowerL23));
            NotifyOfPropertyChange(nameof(OutOfBalancePowerMax));
        });
    }

    private double? realPowerL3;

    [FroniusProprietaryImport("SMARTMETER_POWERACTIVE_03_F64")]
    public double? RealPowerL3
    {
        get => realPowerL3;
        set => Set(ref realPowerL3, value, () =>
        {
            NotifyOfPropertyChange(nameof(OutOfBalancePowerL23));
            NotifyOfPropertyChange(nameof(OutOfBalancePowerL31));
            NotifyOfPropertyChange(nameof(OutOfBalancePowerMax));
        });
    }

    public double? OutOfBalancePowerL12 => Math.Abs((RealPowerL1 - RealPowerL2) ?? double.NaN);
    public double? OutOfBalancePowerL23 => Math.Abs((RealPowerL3 - RealPowerL2) ?? double.NaN);
    public double? OutOfBalancePowerL31 => Math.Abs((RealPowerL1 - RealPowerL3) ?? double.NaN);
    public double? OutOfBalancePowerMax => new[] {OutOfBalancePowerL12, OutOfBalancePowerL23, OutOfBalancePowerL31}.Max();

    private double? realPowerL1Mean;

    [FroniusProprietaryImport("SMARTMETER_POWERACTIVE_MEAN_01_F64")]
    public double? RealPowerL1Mean
    {
        get => realPowerL1Mean;
        set => Set(ref realPowerL1Mean, value);
    }

    private double? realPowerL2Mean;

    [FroniusProprietaryImport("SMARTMETER_POWERACTIVE_MEAN_02_F64")]
    public double? RealPowerL2Mean
    {
        get => realPowerL2Mean;
        set => Set(ref realPowerL2Mean, value, () => NotifyOfPropertyChange(nameof(RealPowerSumMean)));
    }

    private double? realPowerL3Mean;

    [FroniusProprietaryImport("SMARTMETER_POWERACTIVE_MEAN_03_F64")]
    public double? RealPowerL3Mean
    {
        get => realPowerL3Mean;
        set => Set(ref realPowerL3Mean, value, () => NotifyOfPropertyChange(nameof(RealPowerSumMean)));
    }

    private double? realPowerSum;

    [FroniusProprietaryImport("SMARTMETER_POWERACTIVE_MEAN_SUM_F64")]
    public double? RealPowerSum
    {
        get => realPowerSum;
        set => Set(ref realPowerSum, value, () => NotifyOfPropertyChange(nameof(RealPowerSumMean)));
    }

    private double? apparentPowerL1;

    [FroniusProprietaryImport("SMARTMETER_POWERAPPARENT_01_F64")]
    public double? ApparentPowerL1
    {
        get => apparentPowerL1;
        set => Set(ref apparentPowerL1, value);
    }

    private double? apparentPowerL2;

    [FroniusProprietaryImport("SMARTMETER_POWERAPPARENT_02_F64")]
    public double? ApparentPowerL2
    {
        get => apparentPowerL2;
        set => Set(ref apparentPowerL2, value);
    }

    private double? apparentPowerL3;

    [FroniusProprietaryImport("SMARTMETER_POWERAPPARENT_03_F64")]
    public double? ApparentPowerL3
    {
        get => apparentPowerL3;
        set => Set(ref apparentPowerL3, value);
    }

    private double? apparentPowerSum;

    [FroniusProprietaryImport("SMARTMETER_POWERAPPARENT_MEAN_SUM_F64")]
    public double? ApparentPowerSum
    {
        get => apparentPowerSum;
        set => Set(ref apparentPowerSum, value);
    }

    private double? apparentPowerL1Mean;

    [FroniusProprietaryImport("SMARTMETER_POWERAPPARENT_MEAN_01_F64")]
    public double? ApparentPowerL1Mean
    {
        get => apparentPowerL1Mean;
        set => Set(ref apparentPowerL1Mean, value, () => NotifyOfPropertyChange(nameof(ApparentPowerSumMean)));
    }

    private double? apparentPowerL2Mean;

    [FroniusProprietaryImport("SMARTMETER_POWERAPPARENT_MEAN_02_F64")]
    public double? ApparentPowerL2Mean
    {
        get => apparentPowerL2Mean;
        set => Set(ref apparentPowerL2Mean, value, () => NotifyOfPropertyChange(nameof(ApparentPowerSumMean)));
    }

    private double? apparentPowerL3Mean;

    [FroniusProprietaryImport("SMARTMETER_POWERAPPARENT_MEAN_03_F64")]
    public double? ApparentPowerL3Mean
    {
        get => apparentPowerL3Mean;
        set => Set(ref apparentPowerL3Mean, value, () => NotifyOfPropertyChange(nameof(ApparentPowerSumMean)));
    }

    private double? reactivePowerL1;
    [FroniusProprietaryImport("SMARTMETER_POWERREACTIVE_01_F64")]
    public double? ReactivePowerL1
    {
        get => reactivePowerL1;
        set => Set(ref reactivePowerL1, value);
    }

    private double? reactivePowerL2;
    [FroniusProprietaryImport("SMARTMETER_POWERREACTIVE_02_F64")]
    public double? ReactivePowerL2
    {
        get => reactivePowerL2;
        set => Set(ref reactivePowerL2, value);
    }

    private double? reactivePowerL3;
    [FroniusProprietaryImport("SMARTMETER_POWERREACTIVE_03_F64")]
    public double? ReactivePowerL3
    {
        get => reactivePowerL3;
        set => Set(ref reactivePowerL3, value);
    }

    private double? reactivePowerSum;
    [FroniusProprietaryImport("SMARTMETER_POWERREACTIVE_MEAN_SUM_F64")]
    public double? ReactivePowerSum
    {
        get => reactivePowerSum;
        set => Set(ref reactivePowerSum, value);
    }

    private ushort? meterLocationCurrent;
    [FroniusProprietaryImport("SMARTMETER_VALUE_LOCATION_U16")]
    public ushort? MeterLocationCurrent
    {
        get => meterLocationCurrent;
        set => Set(ref meterLocationCurrent, value, () =>
        {
            NotifyOfPropertyChange(nameof(Location));
            NotifyOfPropertyChange(nameof(Usage));
        });
    }

    private double? phaseVoltageL1;
    [FroniusProprietaryImport("SMARTMETER_VOLTAGE_01_F64")]
    public double? PhaseVoltageL1
    {
        get => phaseVoltageL1;
        set => Set(ref phaseVoltageL1, value, () => NotifyOfPropertyChange(nameof(PhaseVoltageAverage)));
    }

    private double? phaseVoltageL2;
    [FroniusProprietaryImport("SMARTMETER_VOLTAGE_02_F64")]
    public double? PhaseVoltageL2
    {
        get => phaseVoltageL2;
        set => Set(ref phaseVoltageL2, value, () => NotifyOfPropertyChange(nameof(PhaseVoltageAverage)));
    }

    private double? phaseVoltageL3;
    [FroniusProprietaryImport("SMARTMETER_VOLTAGE_03_F64")]
    public double? PhaseVoltageL3
    {
        get => phaseVoltageL3;
        set => Set(ref phaseVoltageL3, value, () => NotifyOfPropertyChange(nameof(PhaseVoltageAverage)));
    }

    private double? phaseVoltageL1Mean;
    [FroniusProprietaryImport("SMARTMETER_VOLTAGE_MEAN_01_F64")]
    public double? PhaseVoltageL1Mean
    {
        get => phaseVoltageL1Mean;
        set => Set(ref phaseVoltageL1Mean, value, () => NotifyOfPropertyChange(nameof(PhaseVoltageAverageMean)));
    }

    private double? phaseVoltageL2Mean;
    [FroniusProprietaryImport("SMARTMETER_VOLTAGE_MEAN_02_F64")]
    public double? PhaseVoltageL2Mean
    {
        get => phaseVoltageL2Mean;
        set => Set(ref phaseVoltageL2Mean, value, () => NotifyOfPropertyChange(nameof(PhaseVoltageAverageMean)));
    }

    private double? phaseVoltageL3Mean;
    [FroniusProprietaryImport("SMARTMETER_VOLTAGE_MEAN_03_F64")]
    public double? PhaseVoltageL3Mean
    {
        get => phaseVoltageL3Mean;
        set => Set(ref phaseVoltageL3Mean, value, () => NotifyOfPropertyChange(nameof(PhaseVoltageAverageMean)));
    }

    private double? lineVoltageL12;
    [FroniusProprietaryImport("SMARTMETER_VOLTAGE_MEAN_12_F64")]
    public double? LineVoltageL12
    {
        get => lineVoltageL12;
        set => Set(ref lineVoltageL12, value, () => NotifyOfPropertyChange(nameof(LineVoltageAverage)));
    }

    private double? lineVoltageL23;
    [FroniusProprietaryImport("SMARTMETER_VOLTAGE_MEAN_23_F64")]
    public double? LineVoltageL23
    {
        get => lineVoltageL23;
        set => Set(ref lineVoltageL23, value, () => NotifyOfPropertyChange(nameof(LineVoltageAverage)));
    }

    private double? lineVoltageL31;
    [FroniusProprietaryImport("SMARTMETER_VOLTAGE_MEAN_31_F64")]
    public double? LineVoltageL31
    {
        get => lineVoltageL31;
        set => Set(ref lineVoltageL31, value, () => NotifyOfPropertyChange(nameof(LineVoltageAverage)));
    }

    private string? oemSerialNumber;
    [FroniusProprietaryImport("Manufacturer Serial Number", FroniusDataType.Attribute)]
    public string? OemSerialNumber
    {
        get => oemSerialNumber;
        set => Set(ref oemSerialNumber, value);
    }

    private ushort? productionYear;
    [FroniusProprietaryImport("Production Year", FroniusDataType.Attribute)]
    public ushort? ProductionYear
    {
        get => productionYear;
        set => Set(ref productionYear, value);
    }

    private string? category;
    [FroniusProprietaryImport("category", FroniusDataType.Attribute)]
    public string? Category
    {
        get => category;
        set => Set(ref category, value);
    }

    private string? label;
    [FroniusProprietaryImport("label", FroniusDataType.Attribute)]
    public string? Label
    {
        get => label;
        set => Set(ref label, value);
    }

    private byte phaseCount;
    [FroniusProprietaryImport("phaseCnt", FroniusDataType.Attribute)]
    public byte PhaseCount
    {
        get => phaseCount;
        set => Set(ref phaseCount, value);
    }

    private Version? softwareVersion;
    [FroniusProprietaryImport("this.rev-sw", FroniusDataType.Attribute)]
    public Version? SoftwareVersion
    {
        get => softwareVersion;
        set => Set(ref softwareVersion, value);
    }

    private Version? hardwareVersion;
    [FroniusProprietaryImport("this.rev-hw", FroniusDataType.Attribute)]
    public Version? HardwareVersion
    {
        get => hardwareVersion;
        set => Set(ref hardwareVersion, value);
    }

    public MeterLocation Location => MeterLocationCurrent == 0 ? MeterLocation.Grid : MeterLocation.Load;
    public MeterUsage Usage => MeterLocationCurrent < 2 ? MeterUsage.Inverter : MeterLocationCurrent > 255 ? MeterUsage.UniqueConsumer : MeterUsage.MultipleConsumers;

    public double? RealPowerSumMean => RealPowerL1Mean + RealPowerL2Mean + RealPowerL3Mean;
    public double? ApparentPowerSumMean => ApparentPowerL1Mean + ApparentPowerL2Mean + ApparentPowerL3Mean;

    public double? PhaseVoltageAverage => (PhaseVoltageL1 + PhaseVoltageL2 + PhaseVoltageL3) / 3;
    public double? PhaseVoltageAverageMean => (PhaseVoltageL1Mean + PhaseVoltageL2Mean + PhaseVoltageL3Mean) / 3;
    public double? LineVoltageAverage => (LineVoltageL12 + LineVoltageL23 + LineVoltageL31) / 3;
}