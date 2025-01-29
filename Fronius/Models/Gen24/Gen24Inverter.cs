namespace De.Hochstaetter.Fronius.Models.Gen24;

[SuppressMessage("ReSharper", "StringLiteralTypo")]
public class Gen24Inverter : Gen24DeviceBase
{
    [FroniusProprietaryImport("ACBRIDGE_CURRENT_ACTIVE_MEAN_01_F32")]
    public double? AcCurrentL1
    {
        get;
        set => Set(ref field, value, () =>
        {
            NotifyOfPropertyChange(nameof(AcCurrentSum));
            NotifyOfPropertyChange(nameof(AcPowerL1));
        });
    }

    [FroniusProprietaryImport("ACBRIDGE_CURRENT_ACTIVE_MEAN_02_F32")]
    public double? AcCurrentL2
    {
        get;
        set => Set(ref field, value, () =>
        {
            NotifyOfPropertyChange(nameof(AcCurrentSum));
            NotifyOfPropertyChange(nameof(AcPowerL2));
        });
    }

    [FroniusProprietaryImport("ACBRIDGE_CURRENT_ACTIVE_MEAN_03_F32")]
    public double? AcCurrentL3
    {
        get;
        set => Set(ref field, value, () =>
        {
            NotifyOfPropertyChange(nameof(AcCurrentSum));
            NotifyOfPropertyChange(nameof(AcPowerL3));
        });
    }

    [FroniusProprietaryImport("ACBRIDGE_POWERAPPARENT_SUM_MEAN_F32")]
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
            NotifyOfPropertyChange(nameof(AcPowerL1));
        });
    }

    [FroniusProprietaryImport("ACBRIDGE_VOLTAGE_MEAN_02_F32")]
    public double? AcVoltageL2
    {
        get;
        set => Set(ref field, value, () =>
        {
            NotifyOfPropertyChange(nameof(AcPhaseVoltageAverage));
            NotifyOfPropertyChange(nameof(AcPowerL2));
        });
    }

    [FroniusProprietaryImport("ACBRIDGE_VOLTAGE_MEAN_03_F32")]
    public double? AcVoltageL3
    {
        get;
        set => Set(ref field, value, () =>
        {
            NotifyOfPropertyChange(nameof(AcPhaseVoltageAverage));
            NotifyOfPropertyChange(nameof(AcPowerL3));
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
    public double? AcCurrentSum => AcCurrentL1 + AcCurrentL2 + AcCurrentL3;
    public double? PowerFactorTotal => PowerActiveSum / PowerApparentSum;

    public double? AcPowerL1 => AcVoltageL1 * AcCurrentL1;
    public double? AcPowerL2 => AcVoltageL2 * AcCurrentL2;
    public double? AcPowerL3 => AcVoltageL3 * AcCurrentL3;

    public double? SolarPowerSum => Solar1Power + Solar2Power;
    public double? SolarCurrentSum => Solar1Current + Solar2Current;

}