namespace De.Hochstaetter.Fronius.Models.Gen24;

[SuppressMessage("ReSharper", "StringLiteralTypo")]
public class Gen24Inverter : Gen24DeviceBase
{

    [FroniusProprietaryImport("ACBRIDGE_POWERACTIVE_SUM_MEAN_F32")]
    public double? PowerActiveSum
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(PowerFactorTotal)));
    }

    [FroniusProprietaryImport("ACBRIDGE_POWERREACTIVE_SUM_MEAN_F32")]
    public double? PowerReactiveSum
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("ACBRIDGE_POWERAPPARENT_SUM_MEAN_F32")]
    public double? PowerApparentSum
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(PowerFactorTotal)));
    }

    [FroniusProprietaryImport("ACBRIDGE_POWERREACTIVE_MEAN_01_F32")]
    public double? ReactivePowerL1
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("ACBRIDGE_POWERREACTIVE_MEAN_02_F32")]
    public double? ReactivePowerL2
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("ACBRIDGE_POWERREACTIVE_MEAN_03_F32")]
    public double? ReactivePowerL3
    {
        get;
        set => Set(ref field, value);
    }
    [FroniusProprietaryImport("ACBRIDGE_TIME_BACKUPMODE_UPTIME_SUM_F32")]
    public TimeSpan? BackupModeUpTime
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("ACBRIDGE_VOLTAGE_MEAN_01_F32")]
    public double? AcVoltageL1
    {
        get;
        set => Set(ref field, value, () =>
        {
            NotifyOfPropertyChange(nameof(AcPhaseVoltageAverage));
        });
    }

    [FroniusProprietaryImport("ACBRIDGE_VOLTAGE_MEAN_02_F32")]
    public double? AcVoltageL2
    {
        get;
        set => Set(ref field, value, () =>
        {
            NotifyOfPropertyChange(nameof(AcPhaseVoltageAverage));
        });
    }

    [FroniusProprietaryImport("ACBRIDGE_VOLTAGE_MEAN_03_F32")]
    public double? AcVoltageL3
    {
        get;
        set => Set(ref field, value, () =>
        {
            NotifyOfPropertyChange(nameof(AcPhaseVoltageAverage));
        });
    }

    [FroniusProprietaryImport("ACBRIDGE_VOLTAGE_MEAN_12_F32")]
    public double? AcVoltageL12
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(AcLineVoltageAverage)));
    }

    [FroniusProprietaryImport("ACBRIDGE_VOLTAGE_MEAN_23_F32")]
    public double? AcVoltageL23
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(AcLineVoltageAverage)));
    }

    [FroniusProprietaryImport("ACBRIDGE_VOLTAGE_MEAN_31_F32")]
    public double? AcVoltageL31
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(AcLineVoltageAverage)));
    }

    [FroniusProprietaryImport("BAT_CURRENT_MEAN_F32")]
    public double? StorageCurrent
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("BAT_POWERACTIVE_MEAN_F32")]
    public double? StoragePower
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("BAT_VOLTAGE_OUTER_MEAN_01_F32")]
    public double? StorageVoltage
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

    [FroniusProprietaryImport("DEVICE_TIME_UPTIME_SUM_F32")]
    public TimeSpan? DeviceUpTime
    {
        get;
        set => Set(ref field, value);
    }

    public double? AcPhaseVoltageAverage => (AcVoltageL1 + AcVoltageL2 + AcVoltageL3) / 3d;
    public double? AcLineVoltageAverage => (AcVoltageL12 + AcVoltageL23 + AcVoltageL31) / 3d;
    public double? PowerFactorTotal => PowerActiveSum / PowerApparentSum;

    public double? SolarPowerSum => Solar1Power + Solar2Power;
    public double? SolarCurrentSum => Solar1Current + Solar2Current;

    
    [FroniusProprietaryImport("ACBRIDGE_CURRENT_ACTIVE_MEAN_01_F32")]
    public double? CurrentL1
    {
        get;
        set => Set(ref field, value, () =>
        {
            NotifyOfPropertyChange(nameof(CurrentSum));
            NotifyOfPropertyChange(nameof(OutOfBalanceCurrentL12));
            NotifyOfPropertyChange(nameof(OutOfBalanceCurrentL31));
        });
    }

    [FroniusProprietaryImport("ACBRIDGE_CURRENT_ACTIVE_MEAN_02_F32")]
    public double? CurrentL2
    {
        get;
        set => Set(ref field, value, () =>
        {
            NotifyOfPropertyChange(nameof(CurrentSum));
            NotifyOfPropertyChange(nameof(OutOfBalanceCurrentL23));
            NotifyOfPropertyChange(nameof(OutOfBalanceCurrentL12));
        });
    }

    [FroniusProprietaryImport("ACBRIDGE_CURRENT_ACTIVE_MEAN_03_F32")]
    public double? CurrentL3
    {
        get;
        set => Set(ref field, value, () =>
        {
            NotifyOfPropertyChange(nameof(CurrentSum));
            NotifyOfPropertyChange(nameof(OutOfBalanceCurrentL23));
            NotifyOfPropertyChange(nameof(OutOfBalanceCurrentL31));
        });
    }

    public double? OutOfBalanceCurrentL12 => !CurrentL1.HasValue || !CurrentL2.HasValue ? null : Math.Abs(CurrentL1.Value - CurrentL2.Value);
    public double? OutOfBalanceCurrentL23 => !CurrentL2.HasValue || !CurrentL3.HasValue ? null : Math.Abs(CurrentL2.Value - CurrentL3.Value);
    public double? OutOfBalanceCurrentL31 => !CurrentL3.HasValue || !CurrentL1.HasValue ? null : Math.Abs(CurrentL3.Value - CurrentL1.Value);

    public double? CurrentSum => CurrentL1 + CurrentL2 + CurrentL3;

    [FroniusProprietaryImport("ACBRIDGE_ENERGYACTIVE_ACTIVECONSUMED_SUM_01_U64", Unit.Joule)]
    public double? EnergyConsumedL1
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(EnergyConsumedSum)));
    }

    [FroniusProprietaryImport("ACBRIDGE_ENERGYACTIVE_ACTIVECONSUMED_SUM_02_U64", Unit.Joule)]
    public double? EnergyConsumedL2
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(EnergyConsumedSum)));
    }

    [FroniusProprietaryImport("ACBRIDGE_ENERGYACTIVE_ACTIVECONSUMED_SUM_03_U64", Unit.Joule)]
    public double? EnergyConsumedL3
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(EnergyConsumedSum)));
    }

    public double? EnergyConsumedSum => EnergyConsumedL1 + EnergyConsumedL2 + EnergyConsumedL3;

    [FroniusProprietaryImport("ACBRIDGE_ENERGYACTIVE_PRODUCED_SUM_01_U64", Unit.Joule)]
    public double? EnergyProducedL1
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(EnergyProducedSum)));
    }

    [FroniusProprietaryImport("ACBRIDGE_ENERGYACTIVE_PRODUCED_SUM_02_U64", Unit.Joule)]
    public double? EnergyProducedL2
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(EnergyProducedSum)));
    }

    [FroniusProprietaryImport("ACBRIDGE_ENERGYACTIVE_PRODUCED_SUM_03_U64", Unit.Joule)]
    public double? EnergyProducedL3
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(EnergyProducedSum)));
    }

    public double? Efficiency => (EnergyProducedSum - EnergyConsumedSum) / SolarEnergyLifeTimeSum;

    public double? EnergyProducedSum => EnergyProducedL1 + EnergyProducedL2 + EnergyProducedL3;

    [FroniusProprietaryImport("ACBRIDGE_FREQUENCY_MEAN_F32")]
    public double? InverterFrequency
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("ACBRIDGE_POWERACTIVE_MEAN_01_F32")]
    public double? ActivePowerL1
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(PowerFactorL1)));
    }

    [FroniusProprietaryImport("ACBRIDGE_POWERACTIVE_MEAN_02_F32")]
    public double? ActivePowerL2
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(PowerFactorL2)));
    }

    [FroniusProprietaryImport("ACBRIDGE_POWERACTIVE_MEAN_03_F32")]
    public double? ActivePowerL3
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(PowerFactorL3)));
    }

    [FroniusProprietaryImport("ACBRIDGE_POWERAPPARENT_MEAN_01_F32")]
    public double? ApparentPowerL1
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(PowerFactorL1)));
    }

    [FroniusProprietaryImport("ACBRIDGE_POWERAPPARENT_MEAN_02_F32")]
    public double? ApparentPowerL2
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(PowerFactorL2)));
    }

    [FroniusProprietaryImport("ACBRIDGE_POWERAPPARENT_MEAN_03_F32")]
    public double? ApparentPowerL3
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(PowerFactorL3)));
    }

    public double? PowerFactorL1 => ApparentPowerL1 is 0.0 || !ActivePowerL1.HasValue|| !ApparentPowerL1.HasValue ? null : Math.Min(Math.Max(-1,ActivePowerL1.Value / ApparentPowerL1.Value),1);
    public double? PowerFactorL2 => ApparentPowerL2 is 0.0 || !ActivePowerL2.HasValue|| !ApparentPowerL2.HasValue ? null : Math.Min(Math.Max(-1,ActivePowerL2.Value / ApparentPowerL2.Value),1);
    public double? PowerFactorL3 => ApparentPowerL3 is 0.0 || !ActivePowerL3.HasValue|| !ApparentPowerL3.HasValue ? null : Math.Min(Math.Max(-1,ActivePowerL3.Value / ApparentPowerL3.Value),1);
    public double? PowerFactorAverage => PowerApparentSum is 0.0 || !PowerActiveSum.HasValue|| !PowerApparentSum.HasValue ? null : Math.Min(Math.Max(-1,PowerActiveSum.Value / PowerApparentSum.Value),1);

    public double? RelativeBackupModeUpTime => BackupModeUpTime / InverterUpTime;

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

    [FroniusProprietaryImport("FEEDINPOINT_MODE_GRID_VALIDITY_U8")]
    public byte? GridValidity
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
    public double? AmbientTemperature
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

    [FroniusProprietaryImport("RELAY_MODE_ACTIVATE_BACKUP_INTERLOCK_U16")]
    public ushort? BackupInterlock1
    {
        get;
        set => Set(ref field, value);
    }

    [FroniusProprietaryImport("RELAY_MODE_ACTIVATE_BACKUP_INTERLOCK_OPT_U16")]
    public ushort? BackupInterlock2
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

    [FroniusProprietaryImport("PV_ENERGYACTIVE_ACTIVE_SUM_01_U64", Unit.Joule)]
    public double? Solar1EnergyLifeTime
    {
        get;
        set => Set(ref field, value, ()=>NotifyOfPropertyChange(nameof(SolarEnergyLifeTimeSum)));
    }

    [FroniusProprietaryImport("PV_ENERGYACTIVE_ACTIVE_SUM_02_U64", Unit.Joule)]
    public double? Solar2EnergyLifeTime
    {
        get;
        set => Set(ref field, value, ()=>NotifyOfPropertyChange(nameof(SolarEnergyLifeTimeSum)));
    }

    public double? SolarEnergyLifeTimeSum => Solar1EnergyLifeTime + Solar2EnergyLifeTime;

    public double? StorageEfficiency => StorageLifeTimeEnergyDischarged / StorageLifeTimeEnergyCharged;
}