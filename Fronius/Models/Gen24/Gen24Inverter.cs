namespace De.Hochstaetter.Fronius.Models.Gen24;

[SuppressMessage("ReSharper", "StringLiteralTypo")]
public partial class Gen24Inverter : Gen24DeviceBase
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PowerFactorTotal))]
    [FroniusProprietaryImport("ACBRIDGE_POWERACTIVE_SUM_MEAN_F32")]
    public partial double? PowerActiveSum { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("ACBRIDGE_POWERREACTIVE_SUM_MEAN_F32")]
    public partial double? PowerReactiveSum { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PowerFactorTotal))]
    [FroniusProprietaryImport("ACBRIDGE_POWERAPPARENT_SUM_MEAN_F32")]
    public partial double? PowerApparentSum { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("ACBRIDGE_POWERREACTIVE_MEAN_01_F32")]
    public partial double? ReactivePowerL1 { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("ACBRIDGE_POWERREACTIVE_MEAN_02_F32")]
    public partial double? ReactivePowerL2 { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("ACBRIDGE_POWERREACTIVE_MEAN_03_F32")]
    public partial double? ReactivePowerL3 { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("ACBRIDGE_TIME_BACKUPMODE_UPTIME_SUM_F32")]
    public partial TimeSpan? BackupModeUpTime { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AcPhaseVoltageAverage))]
    [FroniusProprietaryImport("ACBRIDGE_VOLTAGE_MEAN_01_F32")]
    public partial double? AcVoltageL1 { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AcPhaseVoltageAverage))]
    [FroniusProprietaryImport("ACBRIDGE_VOLTAGE_MEAN_02_F32")]
    public partial double? AcVoltageL2 { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AcPhaseVoltageAverage))]
    [FroniusProprietaryImport("ACBRIDGE_VOLTAGE_MEAN_03_F32")]
    public partial double? AcVoltageL3 { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AcLineVoltageAverage))]
    [FroniusProprietaryImport("ACBRIDGE_VOLTAGE_MEAN_12_F32")]
    public partial double? AcVoltageL12 { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AcLineVoltageAverage))]
    [FroniusProprietaryImport("ACBRIDGE_VOLTAGE_MEAN_23_F32")]
    public partial double? AcVoltageL23 { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AcLineVoltageAverage))]
    [FroniusProprietaryImport("ACBRIDGE_VOLTAGE_MEAN_31_F32")]
    public partial double? AcVoltageL31 { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("BAT_CURRENT_MEAN_F32")]
    public partial double? StorageCurrent { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("BAT_POWERACTIVE_MEAN_F32")]
    public partial double? StoragePower { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("BAT_VOLTAGE_OUTER_MEAN_01_F32")]
    public partial double? StorageVoltage { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SolarCurrentSum))]
    [FroniusProprietaryImport("PV_CURRENT_MEAN_01_F32")]
    public partial double? Solar1Current { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SolarCurrentSum))]
    [FroniusProprietaryImport("PV_CURRENT_MEAN_02_F32")]
    public partial double? Solar2Current { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SolarPowerSum))]
    [FroniusProprietaryImport("PV_POWERACTIVE_MEAN_01_F32")]
    public partial double? Solar1Power { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SolarPowerSum))]
    [FroniusProprietaryImport("PV_POWERACTIVE_MEAN_02_F32")]
    public partial double? Solar2Power { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("PV_VOLTAGE_MEAN_01_F32")]
    public partial double? Solar1Voltage { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("PV_VOLTAGE_MEAN_02_F32")]
    public partial double? Solar2Voltage { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("DEVICE_TIME_UPTIME_SUM_F32")]
    public partial TimeSpan? DeviceUpTime { get; set; }

    public double? AcPhaseVoltageAverage => (AcVoltageL1 + AcVoltageL2 + AcVoltageL3) / 3d;
    public double? AcLineVoltageAverage => (AcVoltageL12 + AcVoltageL23 + AcVoltageL31) / 3d;
    public double? PowerFactorTotal => PowerActiveSum / PowerApparentSum;

    public double? SolarPowerSum => Solar1Power + Solar2Power;
    public double? SolarCurrentSum => Solar1Current + Solar2Current;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CurrentSum), nameof(OutOfBalanceCurrentL12), nameof(OutOfBalanceCurrentL31))]
    [FroniusProprietaryImport("ACBRIDGE_CURRENT_ACTIVE_MEAN_01_F32")]
    public partial double? CurrentL1 { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CurrentSum), nameof(OutOfBalanceCurrentL23), nameof(OutOfBalanceCurrentL12))]
    [FroniusProprietaryImport("ACBRIDGE_CURRENT_ACTIVE_MEAN_02_F32")]
    public partial double? CurrentL2 { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CurrentSum), nameof(OutOfBalanceCurrentL23), nameof(OutOfBalanceCurrentL31))]
    [FroniusProprietaryImport("ACBRIDGE_CURRENT_ACTIVE_MEAN_03_F32")]
    public partial double? CurrentL3 { get; set; }

    public double? OutOfBalanceCurrentL12 => !CurrentL1.HasValue || !CurrentL2.HasValue ? null : Math.Abs(CurrentL1.Value - CurrentL2.Value);
    public double? OutOfBalanceCurrentL23 => !CurrentL2.HasValue || !CurrentL3.HasValue ? null : Math.Abs(CurrentL2.Value - CurrentL3.Value);
    public double? OutOfBalanceCurrentL31 => !CurrentL3.HasValue || !CurrentL1.HasValue ? null : Math.Abs(CurrentL3.Value - CurrentL1.Value);

    public double? CurrentSum => CurrentL1 + CurrentL2 + CurrentL3;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EnergyConsumedSum))]
    [FroniusProprietaryImport("ACBRIDGE_ENERGYACTIVE_ACTIVECONSUMED_SUM_01_U64", Unit.Joule)]
    public partial double? EnergyConsumedL1 { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EnergyConsumedSum))]
    [FroniusProprietaryImport("ACBRIDGE_ENERGYACTIVE_ACTIVECONSUMED_SUM_02_U64", Unit.Joule)]
    public partial double? EnergyConsumedL2 { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EnergyConsumedSum))]
    [FroniusProprietaryImport("ACBRIDGE_ENERGYACTIVE_ACTIVECONSUMED_SUM_03_U64", Unit.Joule)]
    public partial double? EnergyConsumedL3 { get; set; }

    public double? EnergyConsumedSum => EnergyConsumedL1 + EnergyConsumedL2 + EnergyConsumedL3;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EnergyProducedSum), nameof(Efficiency))]
    [FroniusProprietaryImport("ACBRIDGE_ENERGYACTIVE_PRODUCED_SUM_01_U64", Unit.Joule)]
    public partial double? EnergyProducedL1 { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EnergyProducedSum), nameof(Efficiency))]
    [FroniusProprietaryImport("ACBRIDGE_ENERGYACTIVE_PRODUCED_SUM_02_U64", Unit.Joule)]
    public partial double? EnergyProducedL2 { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EnergyProducedSum), nameof(Efficiency))]
    [FroniusProprietaryImport("ACBRIDGE_ENERGYACTIVE_PRODUCED_SUM_03_U64", Unit.Joule)]
    public partial double? EnergyProducedL3 { get; set; }

    public double? Efficiency => (EnergyProducedSum - EnergyConsumedSum) / SolarEnergyLifeTimeSum;

    public double? EnergyProducedSum => EnergyProducedL1 + EnergyProducedL2 + EnergyProducedL3;

    [ObservableProperty]
    [FroniusProprietaryImport("ACBRIDGE_FREQUENCY_MEAN_F32")]
    public partial double? InverterFrequency { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PowerFactorL1))]
    [FroniusProprietaryImport("ACBRIDGE_POWERACTIVE_MEAN_01_F32")]
    public partial double? ActivePowerL1 { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PowerFactorL2))]
    [FroniusProprietaryImport("ACBRIDGE_POWERACTIVE_MEAN_02_F32")]
    public partial double? ActivePowerL2 { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PowerFactorL3))]
    [FroniusProprietaryImport("ACBRIDGE_POWERACTIVE_MEAN_03_F32")]
    public partial double? ActivePowerL3 { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PowerFactorL1))]
    [FroniusProprietaryImport("ACBRIDGE_POWERAPPARENT_MEAN_01_F32")]
    public partial double? ApparentPowerL1 { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PowerFactorL2))]
    [FroniusProprietaryImport("ACBRIDGE_POWERAPPARENT_MEAN_02_F32")]
    public partial double? ApparentPowerL2 { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PowerFactorL3))]
    [FroniusProprietaryImport("ACBRIDGE_POWERAPPARENT_MEAN_03_F32")]
    public partial double? ApparentPowerL3 { get; set; }

    public double? PowerFactorL1 => ApparentPowerL1 is 0.0 || !ActivePowerL1.HasValue || !ApparentPowerL1.HasValue ? null : Math.Min(Math.Max(-1, ActivePowerL1.Value / ApparentPowerL1.Value), 1);
    public double? PowerFactorL2 => ApparentPowerL2 is 0.0 || !ActivePowerL2.HasValue || !ApparentPowerL2.HasValue ? null : Math.Min(Math.Max(-1, ActivePowerL2.Value / ApparentPowerL2.Value), 1);
    public double? PowerFactorL3 => ApparentPowerL3 is 0.0 || !ActivePowerL3.HasValue || !ApparentPowerL3.HasValue ? null : Math.Min(Math.Max(-1, ActivePowerL3.Value / ApparentPowerL3.Value), 1);
    public double? PowerFactorAverage => PowerApparentSum is 0.0 || !PowerActiveSum.HasValue || !PowerApparentSum.HasValue ? null : Math.Min(Math.Max(-1, PowerActiveSum.Value / PowerApparentSum.Value), 1);

    public double? RelativeBackupModeUpTime => BackupModeUpTime / InverterUpTime;

    [ObservableProperty]
    [FroniusProprietaryImport("inverter.nodetype", FroniusDataType.Attribute)]
    public partial double? InverterModbusAddress { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StorageEfficiency))]
    [FroniusProprietaryImport("BAT_ENERGYACTIVE_ACTIVECHARGE_SUM_01_U64", Unit.Joule)]
    public partial double? StorageLifeTimeEnergyCharged { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StorageEfficiency))]
    [FroniusProprietaryImport("BAT_ENERGYACTIVE_ACTIVEDISCHARGE_SUM_01_U64", Unit.Joule)]
    public partial double? StorageLifeTimeEnergyDischarged { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("BAT_VOLTAGE_OUTER_MEAN_01_F32")]
    public partial double? StorageVoltageOuter { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("DCLINK_VOLTAGE_MEAN_F32")]
    public partial double? DcLinkVoltage { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("FEEDINPOINT_MODE_GRID_VALIDITY_U8")]
    public partial byte? GridValidity { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("DEVICE_MODE_OPERATING_REFERRAL_U16")]
    public partial ushort? InverterState { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RelativeBackupModeUpTime))]
    [FroniusProprietaryImport("DEVICE_TIME_UPTIME_SUM_F32")]
    public partial TimeSpan? InverterUpTime { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("DEVICE_VOLTAGE_SELV_F32")]
    public partial double? InverterSafetyExtraLowVoltage { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("FANCONTROL_PERCENT_01_F32", Unit.Percent)]
    public partial double? Fan1RelativeRpm { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("FANCONTROL_PERCENT_02_F32", Unit.Percent)]
    public partial double? Fan2RelativeRpm { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("FEEDINPOINT_FREQUENCY_MEAN_F32")]
    public partial double? FeedInPointFrequency { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FeedInPointPhaseVoltageAverage))]
    [FroniusProprietaryImport("FEEDINPOINT_VOLTAGE_MEAN_01_F32")]
    public partial double? FeedInPointPhaseVoltageL1 { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FeedInPointPhaseVoltageAverage))]
    [FroniusProprietaryImport("FEEDINPOINT_VOLTAGE_MEAN_02_F32")]
    public partial double? FeedInPointPhaseVoltageL2 { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FeedInPointPhaseVoltageAverage))]
    [FroniusProprietaryImport("FEEDINPOINT_VOLTAGE_MEAN_03_F32")]
    public partial double? FeedInPointPhaseVoltageL3 { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FeedInPointLineVoltageAverage))]
    [FroniusProprietaryImport("FEEDINPOINT_VOLTAGE_MEAN_12_F32")]
    public partial double? FeedInPointLineVoltageL12 { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FeedInPointLineVoltageAverage))]
    [FroniusProprietaryImport("FEEDINPOINT_VOLTAGE_MEAN_23_F32")]
    public partial double? FeedInPointLineVoltageL23 { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FeedInPointLineVoltageAverage))]
    [FroniusProprietaryImport("FEEDINPOINT_VOLTAGE_MEAN_31_F32")]
    public partial double? FeedInPointLineVoltageL31 { get; set; }

    public double? FeedInPointLineVoltageAverage => (FeedInPointLineVoltageL12 + FeedInPointLineVoltageL23 + FeedInPointLineVoltageL31) / 3;
    public double? FeedInPointPhaseVoltageAverage => (FeedInPointPhaseVoltageL1 + FeedInPointPhaseVoltageL2 + FeedInPointPhaseVoltageL3) / 3;

    [FroniusProprietaryImport("INVERTER_VALUE_SYNCHRONISATION_BITMAP_U16")]
    [ObservableProperty]
    public partial ushort? InverterSynchronizationBitmap { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("RELAY_MODE_ACTIVATE_BACKUP_INTERLOCK_U16")]
    public partial ushort? BackupInterlock1 { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("RELAY_MODE_ACTIVATE_BACKUP_INTERLOCK_OPT_U16")]
    public partial ushort? BackupInterlock2 { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("ISO_RESISTANCE_MEAN_F32")]
    public partial double? IsolatorResistance { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("DEVICE_TEMPERATURE_AMBIENTMEAN_01_F32")]
    public partial double? AmbientTemperature { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("MODULE_TEMPERATURE_MEAN_01_F32")]
    public partial double? TemperatureInverterAcModule { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("MODULE_TEMPERATURE_MEAN_03_F32")]
    public partial double? TemperatureInverterDcModule { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("MODULE_TEMPERATURE_MEAN_04_F32")]
    public partial double? TemperatureInverterBatteryModule { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SolarEnergyLifeTimeSum), nameof(Efficiency))]
    [FroniusProprietaryImport("PV_ENERGYACTIVE_ACTIVE_SUM_01_U64", Unit.Joule)]
    public partial double? Solar1EnergyLifeTime { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SolarEnergyLifeTimeSum), nameof(Efficiency))]
    [FroniusProprietaryImport("PV_ENERGYACTIVE_ACTIVE_SUM_02_U64", Unit.Joule)]
    public partial double? Solar2EnergyLifeTime { get; set; }

    public double? SolarEnergyLifeTimeSum => Solar1EnergyLifeTime + Solar2EnergyLifeTime;

    public double? StorageEfficiency => StorageLifeTimeEnergyDischarged / StorageLifeTimeEnergyCharged;
}