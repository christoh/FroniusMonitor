namespace De.Hochstaetter.Fronius.Models.Gen24;

[SuppressMessage("ReSharper", "StringLiteralTypo")]
public class Gen24Cache : Gen24DeviceBase
{
    [FroniusProprietaryImport("ACBRIDGE_CURRENT_ACTIVE_MEAN_01_F32")]
    public double? InverterCurrentL1
    {
        get;
        set => Set(ref field, value, () =>
        {
            NotifyOfPropertyChange(nameof(InverterCurrentSum));
            NotifyOfPropertyChange(nameof(InverterOutOfBalanceCurrentL12));
            NotifyOfPropertyChange(nameof(InverterOutOfBalanceCurrentL31));
        });
    }

    [FroniusProprietaryImport("ACBRIDGE_CURRENT_ACTIVE_MEAN_02_F32")]
    public double? InverterCurrentL2
    {
        get;
        set => Set(ref field, value, () =>
        {
            NotifyOfPropertyChange(nameof(InverterCurrentSum));
            NotifyOfPropertyChange(nameof(InverterOutOfBalanceCurrentL23));
            NotifyOfPropertyChange(nameof(InverterOutOfBalanceCurrentL12));
        });
    }

    [FroniusProprietaryImport("ACBRIDGE_CURRENT_ACTIVE_MEAN_03_F32")]
    public double? InverterCurrentL3
    {
        get;
        set => Set(ref field, value, () =>
        {
            NotifyOfPropertyChange(nameof(InverterCurrentSum));
            NotifyOfPropertyChange(nameof(InverterOutOfBalanceCurrentL23));
            NotifyOfPropertyChange(nameof(InverterOutOfBalanceCurrentL31));
        });
    }

    public double? InverterOutOfBalanceCurrentL12 => !InverterCurrentL1.HasValue || !InverterCurrentL2.HasValue ? null : Math.Abs(InverterCurrentL1.Value - InverterCurrentL2.Value);
    public double? InverterOutOfBalanceCurrentL23 => !InverterCurrentL2.HasValue || !InverterCurrentL3.HasValue ? null : Math.Abs(InverterCurrentL2.Value - InverterCurrentL3.Value);
    public double? InverterOutOfBalanceCurrentL31 => !InverterCurrentL3.HasValue || !InverterCurrentL1.HasValue ? null : Math.Abs(InverterCurrentL3.Value - InverterCurrentL1.Value);

    public double? InverterCurrentSum => InverterCurrentL1 + InverterCurrentL2 + InverterCurrentL3;

    [FroniusProprietaryImport("ACBRIDGE_ENERGYACTIVE_ACTIVECONSUMED_SUM_01_U64", Unit.Joule)]
    public double? InverterEnergyConsumedL1
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(InverterEnergyConsumedSum)));
    }

    [FroniusProprietaryImport("ACBRIDGE_ENERGYACTIVE_ACTIVECONSUMED_SUM_02_U64", Unit.Joule)]
    public double? InverterEnergyConsumedL2
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(InverterEnergyConsumedSum)));
    }

    [FroniusProprietaryImport("ACBRIDGE_ENERGYACTIVE_ACTIVECONSUMED_SUM_03_U64", Unit.Joule)]
    public double? InverterEnergyConsumedL3
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(InverterEnergyConsumedSum)));
    }

    public double? InverterEnergyConsumedSum => InverterEnergyConsumedL1 + InverterEnergyConsumedL2 + InverterEnergyConsumedL3;

    [FroniusProprietaryImport("ACBRIDGE_ENERGYACTIVE_PRODUCED_SUM_01_U64", Unit.Joule)]
    public double? InverterEnergyProducedL1
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(InverterEnergyProducedSum)));
    }

    [FroniusProprietaryImport("ACBRIDGE_ENERGYACTIVE_PRODUCED_SUM_02_U64", Unit.Joule)]
    public double? InverterEnergyProducedL2
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(InverterEnergyProducedSum)));
    }

    [FroniusProprietaryImport("ACBRIDGE_ENERGYACTIVE_PRODUCED_SUM_03_U64", Unit.Joule)]
    public double? InverterEnergyProducedL3
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(InverterEnergyProducedSum)));
    }

    public double? InverterEfficiency => (InverterEnergyProducedSum - InverterEnergyConsumedSum) / SolarEnergyLifeTimeSum;

    public double? InverterEnergyProducedSum => InverterEnergyProducedL1 + InverterEnergyProducedL2 + InverterEnergyProducedL3;

    [FroniusProprietaryImport("ACBRIDGE_FREQUENCY_MEAN_F32")]
    public double? InverterFrequency
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("ACBRIDGE_POWERACTIVE_MEAN_01_F32")]
    public double? InverterActivePowerL1
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(InverterPowerFactorL1)));
    }

    [FroniusProprietaryImport("ACBRIDGE_POWERACTIVE_MEAN_02_F32")]
    public double? InverterActivePowerL2
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(InverterPowerFactorL2)));
    }

    [FroniusProprietaryImport("ACBRIDGE_POWERACTIVE_MEAN_03_F32")]
    public double? InverterActivePowerL3
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(InverterPowerFactorL3)));
    }

    [FroniusProprietaryImport("ACBRIDGE_POWERACTIVE_SUM_MEAN_F32")]
    public double? InverterActivePowerSum
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(InverterPowerFactorAverage)));
    }

    [FroniusProprietaryImport("ACBRIDGE_POWERAPPARENT_MEAN_01_F32")]
    public double? InverterApparentPowerL1
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(InverterPowerFactorL1)));
    }

    [FroniusProprietaryImport("ACBRIDGE_POWERAPPARENT_MEAN_02_F32")]
    public double? InverterApparentPowerL2
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(InverterPowerFactorL2)));
    }

    [FroniusProprietaryImport("ACBRIDGE_POWERAPPARENT_MEAN_03_F32")]
    public double? InverterApparentPowerL3
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(InverterPowerFactorL3)));
    }

    [FroniusProprietaryImport("ACBRIDGE_POWERAPPARENT_SUM_MEAN_F32")]
    public double? InverterApparentPowerSum
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(InverterPowerFactorAverage)));
    }

    [FroniusProprietaryImport("ACBRIDGE_POWERREACTIVE_MEAN_01_F32")]
    public double? InverterReactivePowerL1
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("ACBRIDGE_POWERREACTIVE_MEAN_02_F32")]
    public double? InverterReactivePowerL2
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("ACBRIDGE_POWERREACTIVE_MEAN_03_F32")]
    public double? InverterReactivePowerL3
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("ACBRIDGE_POWERREACTIVE_SUM_MEAN_F32")]
    public double? InverterReactivePowerSum
    {
        get;
        set => Set(ref field, value);
    }

    public double? InverterPowerFactorL1 => InverterApparentPowerL1 is 0.0 || !InverterActivePowerL1.HasValue|| !InverterApparentPowerL1.HasValue ? null : Math.Min(Math.Max(-1,InverterActivePowerL1.Value / InverterApparentPowerL1.Value),1);
    public double? InverterPowerFactorL2 => InverterApparentPowerL2 is 0.0 || !InverterActivePowerL2.HasValue|| !InverterApparentPowerL2.HasValue ? null : Math.Min(Math.Max(-1,InverterActivePowerL2.Value / InverterApparentPowerL2.Value),1);
    public double? InverterPowerFactorL3 => InverterApparentPowerL3 is 0.0 || !InverterActivePowerL3.HasValue|| !InverterApparentPowerL3.HasValue ? null : Math.Min(Math.Max(-1,InverterActivePowerL3.Value / InverterApparentPowerL3.Value),1);
    public double? InverterPowerFactorAverage => InverterApparentPowerSum is 0.0 || !InverterActivePowerSum.HasValue|| !InverterApparentPowerSum.HasValue ? null : Math.Min(Math.Max(-1,InverterActivePowerSum.Value / InverterApparentPowerSum.Value),1);

    [FroniusProprietaryImport("ACBRIDGE_TIME_BACKUPMODE_UPTIME_SUM_F32")]
    public TimeSpan BackupModeUpTime
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(RelativeBackupModeUpTime)));
    }

    public double? RelativeBackupModeUpTime => BackupModeUpTime / InverterUpTime;

    [FroniusProprietaryImport("ACBRIDGE_VOLTAGE_MEAN_01_F32")]
    public double? InverterPhaseVoltageL1
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(InverterPhaseVoltageAverage)));
    }

    [FroniusProprietaryImport("ACBRIDGE_VOLTAGE_MEAN_02_F32")]
    public double? InverterPhaseVoltageL2
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(InverterPhaseVoltageAverage)));
    }

    [FroniusProprietaryImport("ACBRIDGE_VOLTAGE_MEAN_03_F32")]
    public double? InverterPhaseVoltageL3
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(InverterPhaseVoltageAverage)));
    }

    public double? InverterPhaseVoltageAverage => (InverterPhaseVoltageL1 + InverterPhaseVoltageL2 + InverterPhaseVoltageL3) / 3;

    [FroniusProprietaryImport("ACBRIDGE_VOLTAGE_MEAN_12_F32")]
    public double? InverterLineVoltageL12
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(InverterLineVoltageAverage)));
    }

    [FroniusProprietaryImport("ACBRIDGE_VOLTAGE_MEAN_23_F32")]
    public double? InverterLineVoltageL23
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(InverterLineVoltageAverage)));
    }

    [FroniusProprietaryImport("ACBRIDGE_VOLTAGE_MEAN_31_F32")]
    public double? InverterLineVoltageL31
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(InverterLineVoltageAverage)));
    }

    public double? InverterLineVoltageAverage => (InverterLineVoltageL12 + InverterLineVoltageL23 + InverterLineVoltageL31) / 3;

    [FroniusProprietaryImport("BAT_CURRENT_MEAN_F32")]
    public double? StorageCurrent
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("inverter.nodetype", FroniusDataType.Attribute)]
    public double? InverterModbusAddress
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("BAT_ENERGYACTIVE_ACTIVECHARGE_SUM_01_U64", Unit.Joule)]
    public double? StorageLifeTimeEnergyCharged
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(StorageEfficiency)));
    }

    [FroniusProprietaryImport("BAT_ENERGYACTIVE_ACTIVEDISCHARGE_SUM_01_U64", Unit.Joule)]
    public double? StorageLifeTimeEnergyDischarged
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(StorageEfficiency)));
    }

    [FroniusProprietaryImport("BAT_POWERACTIVE_MEAN_F32")]
    public double? StoragePower
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("BAT_VOLTAGE_OUTER_MEAN_01_F32")]
    public double? StorageVoltageOuter
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("DCLINK_VOLTAGE_MEAN_F32")]
    public double? DcLinkVoltage
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("DEVICE_MODE_OPERATING_REFERRAL_U16")]
    public ushort? InverterState
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("DEVICE_TEMPERATURE_AMBIENTEMEAN_F32")]
    public double? InverterAmbientTemperature
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("DEVICE_TIME_UPTIME_SUM_F32")]
    public TimeSpan? InverterUpTime
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(RelativeBackupModeUpTime)));
    }

    [FroniusProprietaryImport("DEVICE_VOLTAGE_SELV_F32")]
    public double? InverterSafetyExtraLowVoltage
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("FANCONTROL_PERCENT_01_F32", Unit.Percent)]
    public double? Fan1RelativeRpm
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("FANCONTROL_PERCENT_02_F32", Unit.Percent)]
    public double? Fan2RelativeRpm
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("FEEDINPOINT_FREQUENCY_MEAN_F32")]
    public double? FeedInPointFrequency
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("FEEDINPOINT_VOLTAGE_MEAN_01_F32")]
    public double? FeedInPointPhaseVoltageL1
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("FEEDINPOINT_VOLTAGE_MEAN_02_F32")]
    public double? FeedInPointPhaseVoltageL2
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("FEEDINPOINT_VOLTAGE_MEAN_03_F32")]
    public double? FeedInPointPhaseVoltageL3
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("FEEDINPOINT_VOLTAGE_MEAN_12_F32")]
    public double? FeedInPointLineVoltageL12
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("FEEDINPOINT_VOLTAGE_MEAN_23_F32")]
    public double? FeedInPointLineVoltageL23
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("FEEDINPOINT_VOLTAGE_MEAN_31_F32")]
    public double? FeedInPointLineVoltageL31
    {
        get;
        set => Set(ref field, value);
    }

    public double? FeedInPointLineVoltageAverage => (FeedInPointLineVoltageL12 + FeedInPointLineVoltageL23 + FeedInPointLineVoltageL31) / 3;
    public double? FeedInPointPhaseVoltageAverage => (FeedInPointPhaseVoltageL1 + FeedInPointPhaseVoltageL2 + FeedInPointPhaseVoltageL3) / 3;

    [FroniusProprietaryImport("INVERTER_VALUE_SYNCHRONISATION_BITMAP_U16")]
    public ushort? InverterSynchronizationBitmap
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("ISO_RESISTANCE_MEAN_F32")]
    public double? IsolatorResistance
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("MODULE_TEMPERATURE_MEAN_01_F32")]
    public double? TemperatureInverterAcModule
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("MODULE_TEMPERATURE_MEAN_03_F32")]
    public double? TemperatureInverterDcModule
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("MODULE_TEMPERATURE_MEAN_04_F32")]
    public double? TemperatureInverterBatteryModule
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("PV_CURRENT_MEAN_01_F32")]
    public double? Solar1Current
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(SolarCurrentSum)));
    }

    [FroniusProprietaryImport("PV_CURRENT_MEAN_02_F32")]
    public double? Solar2Current
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(SolarCurrentSum)));
    }

    [FroniusProprietaryImport("PV_POWERACTIVE_MEAN_01_F32")]
    public double? Solar1Power
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(SolarPowerSum)));
    }

    [FroniusProprietaryImport("PV_POWERACTIVE_MEAN_02_F32")]
    public double? Solar2Power
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(SolarPowerSum)));
    }

    [FroniusProprietaryImport("PV_VOLTAGE_MEAN_01_F32")]
    public double? Solar1Voltage
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("PV_VOLTAGE_MEAN_02_F32")]
    public double? Solar2Voltage
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("PV_ENERGYACTIVE_ACTIVE_SUM_01_U64", Unit.Joule)]
    public double? Solar1EnergyLifeTime
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("PV_ENERGYACTIVE_ACTIVE_SUM_02_U64", Unit.Joule)]
    public double? Solar2EnergyLifeTime
    {
        get;
        set => Set(ref field, value);
    }

    public double? SolarEnergyLifeTimeSum => Solar1EnergyLifeTime + Solar2EnergyLifeTime;

    public double? StorageEfficiency => StorageLifeTimeEnergyDischarged / StorageLifeTimeEnergyCharged;

    public double? SolarPowerSum => Solar1Power + Solar2Power;
    public double? SolarCurrentSum => Solar1Current + Solar2Current;
}
