namespace De.Hochstaetter.Fronius.Models.Gen24;

[SuppressMessage("ReSharper", "StringLiteralTypo")]
public class Gen24Cache : Gen24DeviceBase
{
    private double? inverterCurrentL1;

    [FroniusProprietaryImport("ACBRIDGE_CURRENT_ACTIVE_MEAN_01_F32")]
    public double? InverterCurrentL1
    {
        get => inverterCurrentL1;
        set => Set(ref inverterCurrentL1, value, () => NotifyOfPropertyChange(nameof(InverterCurrentSum)));
    }

    private double? inverterCurrentL2;

    [FroniusProprietaryImport("ACBRIDGE_CURRENT_ACTIVE_MEAN_02_F32")]
    public double? InverterCurrentL2
    {
        get => inverterCurrentL2;
        set => Set(ref inverterCurrentL2, value, () => NotifyOfPropertyChange(nameof(InverterCurrentSum)));
    }

    private double? inverterCurrentL3;

    [FroniusProprietaryImport("ACBRIDGE_CURRENT_ACTIVE_MEAN_03_F32")]
    public double? InverterCurrentL3
    {
        get => inverterCurrentL3;
        set => Set(ref inverterCurrentL3, value, () => NotifyOfPropertyChange(nameof(InverterCurrentSum)));
    }

    public double? InverterCurrentSum => InverterCurrentL1 + InverterCurrentL2 + InverterCurrentL3;

    private double? inverterEnergyConsumedL1;

    [FroniusProprietaryImport("ACBRIDGE_ENERGYACTIVE_ACTIVECONSUMED_SUM_01_U64", Unit.Joule)]
    public double? InverterEnergyConsumedL1
    {
        get => inverterEnergyConsumedL1;
        set => Set(ref inverterEnergyConsumedL1, value, () => NotifyOfPropertyChange(nameof(InverterEnergyConsumedSum)));
    }

    private double? inverterEnergyConsumedL2;

    [FroniusProprietaryImport("ACBRIDGE_ENERGYACTIVE_ACTIVECONSUMED_SUM_02_U64", Unit.Joule)]
    public double? InverterEnergyConsumedL2
    {
        get => inverterEnergyConsumedL1;
        set => Set(ref inverterEnergyConsumedL2, value, () => NotifyOfPropertyChange(nameof(InverterEnergyConsumedSum)));
    }

    private double? inverterEnergyConsumedL3;

    [FroniusProprietaryImport("ACBRIDGE_ENERGYACTIVE_ACTIVECONSUMED_SUM_03_U64", Unit.Joule)]
    public double? InverterEnergyConsumedL3
    {
        get => inverterEnergyConsumedL3;
        set => Set(ref inverterEnergyConsumedL3, value, () => NotifyOfPropertyChange(nameof(InverterEnergyConsumedSum)));
    }

    public double? InverterEnergyConsumedSum => InverterEnergyConsumedL1 + InverterEnergyConsumedL2 + InverterEnergyConsumedL3;

    private double? inverterEnergyProducedL1;

    [FroniusProprietaryImport("ACBRIDGE_ENERGYACTIVE_PRODUCED_SUM_01_U64", Unit.Joule)]
    public double? InverterEnergyProducedL1
    {
        get => inverterEnergyProducedL1;
        set => Set(ref inverterEnergyProducedL1, value, () => NotifyOfPropertyChange(nameof(InverterEnergyProducedSum)));
    }

    private double? inverterEnergyProducedL2;

    [FroniusProprietaryImport("ACBRIDGE_ENERGYACTIVE_PRODUCED_SUM_02_U64", Unit.Joule)]
    public double? InverterEnergyProducedL2
    {
        get => inverterEnergyProducedL2;
        set => Set(ref inverterEnergyProducedL2, value, () => NotifyOfPropertyChange(nameof(InverterEnergyProducedSum)));
    }

    private double? inverterEnergyProducedL3;

    [FroniusProprietaryImport("ACBRIDGE_ENERGYACTIVE_PRODUCED_SUM_03_U64", Unit.Joule)]
    public double? InverterEnergyProducedL3
    {
        get => inverterEnergyProducedL3;
        set => Set(ref inverterEnergyProducedL3, value, () => NotifyOfPropertyChange(nameof(InverterEnergyProducedSum)));
    }

    public double? InverterEnergyProducedSum => InverterEnergyProducedL1 + InverterEnergyProducedL2 + InverterEnergyProducedL3;

    private double? inverterFrequency;

    [FroniusProprietaryImport("ACBRIDGE_FREQUENCY_MEAN_F32")]
    public double? InverterFrequency
    {
        get => inverterFrequency;
        set => Set(ref inverterFrequency, value);
    }

    private double? inverterRealPowerL1;

    [FroniusProprietaryImport("ACBRIDGE_POWERACTIVE_MEAN_01_F32")]
    public double? InverterRealPowerL1
    {
        get => inverterRealPowerL1;
        set => Set(ref inverterRealPowerL1, value, () => NotifyOfPropertyChange(nameof(InverterPowerFactorL1)));
    }

    private double? inverterRealPowerL2;

    [FroniusProprietaryImport("ACBRIDGE_POWERACTIVE_MEAN_02_F32")]
    public double? InverterRealPowerL2
    {
        get => inverterRealPowerL2;
        set => Set(ref inverterRealPowerL2, value, () => NotifyOfPropertyChange(nameof(InverterPowerFactorL2)));
    }

    private double? inverterRealPowerL3;

    [FroniusProprietaryImport("ACBRIDGE_POWERACTIVE_MEAN_03_F32")]
    public double? InverterRealPowerL3
    {
        get => inverterRealPowerL3;
        set => Set(ref inverterRealPowerL3, value, () => NotifyOfPropertyChange(nameof(InverterPowerFactorL3)));
    }

    private double? inverterRealPowerSum;

    [FroniusProprietaryImport("ACBRIDGE_POWERACTIVE_SUM_MEAN_F32")]
    public double? InverterRealPowerSum
    {
        get => inverterRealPowerSum;
        set => Set(ref inverterRealPowerSum, value, () => NotifyOfPropertyChange(nameof(InverterPowerFactorAverage)));
    }

    private double? inverterApparentPowerL1;

    [FroniusProprietaryImport("ACBRIDGE_POWERAPPARENT_MEAN_01_F32")]
    public double? InverterApparentPowerL1
    {
        get => inverterApparentPowerL1;
        set => Set(ref inverterApparentPowerL1, value, () => NotifyOfPropertyChange(nameof(InverterPowerFactorL1)));
    }

    private double? inverterApparentPowerL2;

    [FroniusProprietaryImport("ACBRIDGE_POWERAPPARENT_MEAN_02_F32")]
    public double? InverterApparentPowerL2
    {
        get => inverterApparentPowerL2;
        set => Set(ref inverterApparentPowerL2, value, () => NotifyOfPropertyChange(nameof(InverterPowerFactorL2)));
    }

    private double? inverterApparentPowerL3;

    [FroniusProprietaryImport("ACBRIDGE_POWERAPPARENT_MEAN_03_F32")]
    public double? InverterApparentPowerL3
    {
        get => inverterApparentPowerL3;
        set => Set(ref inverterApparentPowerL3, value, () => NotifyOfPropertyChange(nameof(InverterPowerFactorL3)));
    }

    private double? inverterApparentPowerSum;

    [FroniusProprietaryImport("ACBRIDGE_POWERAPPARENT_SUM_MEAN_F32")]
    public double? InverterApparentPowerSum
    {
        get => inverterApparentPowerSum;
        set => Set(ref inverterApparentPowerSum, value, () => NotifyOfPropertyChange(nameof(InverterPowerFactorAverage)));
    }

    private double? inverterReactivePowerL1;

    [FroniusProprietaryImport("ACBRIDGE_POWERREACTIVE_MEAN_01_F32")]
    public double? InverterReactivePowerL1
    {
        get => inverterReactivePowerL1;
        set => Set(ref inverterReactivePowerL1, value);
    }

    private double? inverterReactivePowerL2;

    [FroniusProprietaryImport("ACBRIDGE_POWERREACTIVE_MEAN_02_F32")]
    public double? InverterReactivePowerL2
    {
        get => inverterReactivePowerL2;
        set => Set(ref inverterReactivePowerL2, value);
    }

    private double? inverterReactivePowerL3;

    [FroniusProprietaryImport("ACBRIDGE_POWERREACTIVE_MEAN_03_F32")]
    public double? InverterReactivePowerL3
    {
        get => inverterReactivePowerL3;
        set => Set(ref inverterReactivePowerL3, value);
    }

    private double? inverterReactivePowerSum;

    [FroniusProprietaryImport("ACBRIDGE_POWERREACTIVE_SUM_MEAN_F32")]
    public double? InverterReactivePowerSum
    {
        get => inverterReactivePowerSum;
        set => Set(ref inverterReactivePowerSum, value);
    }

    public double? InverterPowerFactorL1 => InverterRealPowerL1 / InverterApparentPowerL1;
    public double? InverterPowerFactorL2 => InverterRealPowerL2 / InverterApparentPowerL2;
    public double? InverterPowerFactorL3 => InverterRealPowerL3 / InverterApparentPowerL3;
    public double? InverterPowerFactorAverage => InverterRealPowerSum / InverterApparentPowerSum;

    private TimeSpan backupModeUpTime;

    [FroniusProprietaryImport("ACBRIDGE_TIME_BACKUPMODE_UPTIME_SUM_F32")]
    public TimeSpan BackupModeUpTime
    {
        get => backupModeUpTime;
        set => Set(ref backupModeUpTime, value);
    }

    private double? inverterPhaseVoltageL1;

    [FroniusProprietaryImport("ACBRIDGE_VOLTAGE_MEAN_01_F32")]
    public double? InverterPhaseVoltageL1
    {
        get => inverterPhaseVoltageL1;
        set => Set(ref inverterPhaseVoltageL1, value, () => NotifyOfPropertyChange(nameof(InverterPhaseVoltageAverage)));
    }

    private double? inverterPhaseVoltageL2;

    [FroniusProprietaryImport("ACBRIDGE_VOLTAGE_MEAN_02_F32")]
    public double? InverterPhaseVoltageL2
    {
        get => inverterPhaseVoltageL2;
        set => Set(ref inverterPhaseVoltageL2, value, () => NotifyOfPropertyChange(nameof(InverterPhaseVoltageAverage)));
    }

    private double? inverterPhaseVoltageL3;

    [FroniusProprietaryImport("ACBRIDGE_VOLTAGE_MEAN_03_F32")]
    public double? InverterPhaseVoltageL3
    {
        get => inverterPhaseVoltageL3;
        set => Set(ref inverterPhaseVoltageL3, value, () => NotifyOfPropertyChange(nameof(InverterPhaseVoltageAverage)));
    }

    public double? InverterPhaseVoltageAverage => (InverterPhaseVoltageL1 + InverterPhaseVoltageL2 + InverterPhaseVoltageL3) / 3;

    private double? inverterLineVoltageL12;

    [FroniusProprietaryImport("ACBRIDGE_VOLTAGE_MEAN_12_F32")]
    public double? InverterLineVoltageL12
    {
        get => inverterLineVoltageL12;
        set => Set(ref inverterLineVoltageL12, value, () => NotifyOfPropertyChange(nameof(InverterLineVoltageAverage)));
    }

    private double? inverterLineVoltageL23;

    [FroniusProprietaryImport("ACBRIDGE_VOLTAGE_MEAN_23_F32")]
    public double? InverterLineVoltageL23
    {
        get => inverterLineVoltageL23;
        set => Set(ref inverterLineVoltageL23, value, () => NotifyOfPropertyChange(nameof(InverterLineVoltageAverage)));
    }

    private double? inverterLineVoltageL31;

    [FroniusProprietaryImport("ACBRIDGE_VOLTAGE_MEAN_31_F32")]
    public double? InverterLineVoltageL31
    {
        get => inverterLineVoltageL31;
        set => Set(ref inverterLineVoltageL31, value, () => NotifyOfPropertyChange(nameof(InverterLineVoltageAverage)));
    }

    public double? InverterLineVoltageAverage => (InverterLineVoltageL12 + InverterLineVoltageL23 + InverterLineVoltageL31) / 3;

    private double? storageCurrent;

    [FroniusProprietaryImport("BAT_CURRENT_MEAN_F32")]
    public double? StorageCurrent
    {
        get => storageCurrent;
        set => Set(ref storageCurrent, value);
    }

    private double? inverterModbusAddress;

    [FroniusProprietaryImport("inverter.nodetype", FroniusDataType.Attribute)]
    public double? InverterModbusAddress
    {
        get => inverterModbusAddress;
        set => Set(ref inverterModbusAddress, value);
    }

    private double? storageLifeTimeEnergyCharged;

    [FroniusProprietaryImport("BAT_ENERGYACTIVE_ACTIVECHARGE_SUM_01_U64", Unit.Joule)]
    public double? StorageLifeTimeEnergyCharged
    {
        get => storageLifeTimeEnergyCharged;
        set => Set(ref storageLifeTimeEnergyCharged, value, () => NotifyOfPropertyChange(nameof(StorageEfficiency)));
    }

    private double? storageLifeTimeEnergyDischarged;

    [FroniusProprietaryImport("BAT_ENERGYACTIVE_ACTIVEDISCHARGE_SUM_01_U64", Unit.Joule)]
    public double? StorageLifeTimeEnergyDischarged
    {
        get => storageLifeTimeEnergyDischarged;
        set => Set(ref storageLifeTimeEnergyDischarged, value, () => NotifyOfPropertyChange(nameof(StorageEfficiency)));
    }

    private double? storagePower;

    [FroniusProprietaryImport("BAT_POWERACTIVE_MEAN_F32")]
    public double? StoragePower
    {
        get => storagePower;
        set => Set(ref storagePower, value);
    }

    private double? storageVoltageOuter;

    [FroniusProprietaryImport("BAT_VOLTAGE_OUTER_MEAN_01_F32")]
    public double? StorageVoltageOuter
    {
        get => storageVoltageOuter;
        set => Set(ref storageVoltageOuter, value);
    }

    private double? dcLinkVoltage;

    [FroniusProprietaryImport("DCLINK_VOLTAGE_MEAN_F32")]
    public double? DcLinkVoltage
    {
        get => dcLinkVoltage;
        set => Set(ref dcLinkVoltage, value);
    }

    private ushort? inverterState;

    [FroniusProprietaryImport("DEVICE_MODE_OPERATING_REFERRAL_U16")]
    public ushort? InverterState
    {
        get => inverterState;
        set => Set(ref inverterState, value);
    }

    private double? inverterAmbientTemperature;

    [FroniusProprietaryImport("DEVICE_TEMPERATURE_AMBIENTEMEAN_F32")]
    public double? InverterAmbientTemperature
    {
        get => inverterAmbientTemperature;
        set => Set(ref inverterAmbientTemperature, value);
    }

    private TimeSpan? inverterUpTime;

    [FroniusProprietaryImport("DEVICE_TIME_UPTIME_SUM_F32")]
    public TimeSpan? InverterUpTime
    {
        get => inverterUpTime;
        set => Set(ref inverterUpTime, value);
    }

    private double? inverterSafetyExtraLowVoltage;

    [FroniusProprietaryImport("DEVICE_VOLTAGE_SELV_F32")]
    public double? InverterSafetyExtraLowVoltage
    {
        get => inverterSafetyExtraLowVoltage;
        set => Set(ref inverterSafetyExtraLowVoltage, value);
    }

    private double? fan1RelativeRpm;

    [FroniusProprietaryImport("FANCONTROL_PERCENT_01_F32", Unit.Percent)]
    public double? Fan1RelativeRpm
    {
        get => fan1RelativeRpm;
        set => Set(ref fan1RelativeRpm, value);
    }

    private double? fan2RelativeRpm;

    [FroniusProprietaryImport("FANCONTROL_PERCENT_02_F32", Unit.Percent)]
    public double? Fan2RelativeRpm
    {
        get => fan2RelativeRpm;
        set => Set(ref fan2RelativeRpm, value);
    }

    private double? feedInPointFrequency;

    [FroniusProprietaryImport("FEEDINPOINT_FREQUENCY_MEAN_F32")]
    public double? FeedInPointFrequency
    {
        get => feedInPointFrequency;
        set => Set(ref feedInPointFrequency, value);
    }

    private double? feedInPointPhaseVoltageL1;

    [FroniusProprietaryImport("FEEDINPOINT_VOLTAGE_MEAN_01_F32")]
    public double? FeedInPointPhaseVoltageL1
    {
        get => feedInPointPhaseVoltageL1;
        set => Set(ref feedInPointPhaseVoltageL1, value);
    }

    private double? feedInPointPhaseVoltageL2;

    [FroniusProprietaryImport("FEEDINPOINT_VOLTAGE_MEAN_02_F32")]
    public double? FeedInPointPhaseVoltageL2
    {
        get => feedInPointPhaseVoltageL2;
        set => Set(ref feedInPointPhaseVoltageL2, value);
    }

    private double? feedInPointPhaseVoltageL3;

    [FroniusProprietaryImport("FEEDINPOINT_VOLTAGE_MEAN_03_F32")]
    public double? FeedInPointPhaseVoltageL3
    {
        get => feedInPointPhaseVoltageL3;
        set => Set(ref feedInPointPhaseVoltageL3, value);
    }

    private double? feedInPointLineVoltageL12;

    [FroniusProprietaryImport("FEEDINPOINT_VOLTAGE_MEAN_12_F32")]
    public double? FeedInPointLineVoltageL12
    {
        get => feedInPointLineVoltageL12;
        set => Set(ref feedInPointLineVoltageL12, value);
    }

    private double? feedInPointLineVoltageL23;

    [FroniusProprietaryImport("FEEDINPOINT_VOLTAGE_MEAN_23_F32")]
    public double? FeedInPointLineVoltageL23
    {
        get => feedInPointLineVoltageL23;
        set => Set(ref feedInPointLineVoltageL23, value);
    }

    private double? feedInPointLineVoltageL31;

    [FroniusProprietaryImport("FEEDINPOINT_VOLTAGE_MEAN_31_F32")]
    public double? FeedInPointLineVoltageL31
    {
        get => feedInPointLineVoltageL31;
        set => Set(ref feedInPointLineVoltageL31, value);
    }

    public double? FeedInPointLineVoltageAverage => (FeedInPointLineVoltageL12 + FeedInPointLineVoltageL23 + FeedInPointLineVoltageL31) / 3;
    public double? FeedInPointPhaseVoltageAverage => (FeedInPointPhaseVoltageL1 + FeedInPointPhaseVoltageL2 + FeedInPointPhaseVoltageL3) / 3;

    private ushort? inverterSynchronizationBitmap;

    [FroniusProprietaryImport("INVERTER_VALUE_SYNCHRONISATION_BITMAP_U16")]
    public ushort? InverterSynchronizationBitmap
    {
        get => inverterSynchronizationBitmap;
        set => Set(ref inverterSynchronizationBitmap, value);
    }

    private double? isolatorResistance;

    [FroniusProprietaryImport("ISO_RESISTANCE_MEAN_F32")]
    public double? IsolatorResistance
    {
        get => isolatorResistance;
        set => Set(ref isolatorResistance, value);
    }

    private double? temperatureInverterModule1;

    [FroniusProprietaryImport("MODULE_TEMPERATURE_MEAN_01_F32")]
    public double? TemperatureInverterModule1
    {
        get => temperatureInverterModule1;
        set => Set(ref temperatureInverterModule1, value);
    }

    private double? temperatureInverterModule3;

    [FroniusProprietaryImport("MODULE_TEMPERATURE_MEAN_03_F32")]
    public double? TemperatureInverterModule3
    {
        get => temperatureInverterModule3;
        set => Set(ref temperatureInverterModule3, value);
    }

    private double? temperatureInverterModule4;

    [FroniusProprietaryImport("MODULE_TEMPERATURE_MEAN_04_F32")]
    public double? TemperatureInverterModule4
    {
        get => temperatureInverterModule4;
        set => Set(ref temperatureInverterModule4, value);
    }

    private double? solar1Current;

    [FroniusProprietaryImport("PV_CURRENT_MEAN_01_F32")]
    public double? Solar1Current
    {
        get => solar1Current;
        set => Set(ref solar1Current, value, () => NotifyOfPropertyChange(nameof(SolarCurrentSum)));
    }

    private double? solar2Current;

    [FroniusProprietaryImport("PV_CURRENT_MEAN_02_F32")]
    public double? Solar2Current
    {
        get => solar2Current;
        set => Set(ref solar2Current, value, () => NotifyOfPropertyChange(nameof(SolarCurrentSum)));
    }

    private double? solar1Power;

    [FroniusProprietaryImport("PV_POWERACTIVE_MEAN_01_F32")]
    public double? Solar1Power
    {
        get => solar1Power;
        set => Set(ref solar1Power, value, () => NotifyOfPropertyChange(nameof(SolarPowerSum)));
    }

    private double? solar2Power;

    [FroniusProprietaryImport("PV_POWERACTIVE_MEAN_02_F32")]
    public double? Solar2Power
    {
        get => solar2Power;
        set => Set(ref solar2Power, value, () => NotifyOfPropertyChange(nameof(SolarPowerSum)));
    }

    private double? solar1Voltage;

    [FroniusProprietaryImport("PV_VOLTAGE_MEAN_01_F32")]
    public double? Solar1Voltage
    {
        get => solar1Voltage;
        set => Set(ref solar1Voltage, value);
    }

    private double? solar2Voltage;

    [FroniusProprietaryImport("PV_VOLTAGE_MEAN_02_F32")]
    public double? Solar2Voltage
    {
        get => solar2Voltage;
        set => Set(ref solar2Voltage, value);
    }

    private double? solar1EnergyLifeTime;

    [FroniusProprietaryImport("PV_ENERGYACTIVE_ACTIVE_SUM_01_U64", Unit.Joule)]
    public double? Solar1EnergyLifeTime
    {
        get => solar1EnergyLifeTime;
        set => Set(ref solar1EnergyLifeTime, value);
    }

    private double? solar2EnergyLifeTime;

    [FroniusProprietaryImport("PV_ENERGYACTIVE_ACTIVE_SUM_02_U64", Unit.Joule)]
    public double? Solar2EnergyLifeTime
    {
        get => solar2EnergyLifeTime;
        set => Set(ref solar2EnergyLifeTime, value);
    }

    public double? SolarEnergyLifeTimeSum => Solar1EnergyLifeTime + Solar2EnergyLifeTime;

    public double? StorageEfficiency => StorageLifeTimeEnergyDischarged / StorageLifeTimeEnergyCharged;

    public double? SolarPowerSum => Solar1Power + Solar2Power;
    public double? SolarCurrentSum => Solar1Current + Solar2Current;
}
