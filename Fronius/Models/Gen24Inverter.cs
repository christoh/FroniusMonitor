using System.Diagnostics.CodeAnalysis;
using De.Hochstaetter.Fronius.Attributes;

namespace De.Hochstaetter.Fronius.Models;

[SuppressMessage("ReSharper", "StringLiteralTypo")]
public class Gen24Inverter : Gen24DeviceBase
{
    private double? acCurrentL1;

    [FroniusProprietaryImport("ACBRIDGE_CURRENT_ACTIVE_MEAN_01_F32")]
    public double? AcCurrentL1
    {
        get => acCurrentL1;
        set => Set(ref acCurrentL1, value, () =>
        {
            NotifyOfPropertyChange(nameof(AcCurrentSum));
            NotifyOfPropertyChange(nameof(AcPowerL1));
        });
    }

    private double? acCurrentL2;

    [FroniusProprietaryImport("ACBRIDGE_CURRENT_ACTIVE_MEAN_02_F32")]
    public double? AcCurrentL2
    {
        get => acCurrentL2;
        set => Set(ref acCurrentL2, value, () =>
        {
            NotifyOfPropertyChange(nameof(AcCurrentSum));
            NotifyOfPropertyChange(nameof(AcPowerL2));
        });
    }

    private double? acCurrentL3;

    [FroniusProprietaryImport("ACBRIDGE_CURRENT_ACTIVE_MEAN_03_F32")]
    public double? AcCurrentL3
    {
        get => acCurrentL3;
        set => Set(ref acCurrentL3, value, () =>
        {
            NotifyOfPropertyChange(nameof(AcCurrentSum));
            NotifyOfPropertyChange(nameof(AcPowerL3));
        });
    }

    private double? powerRealSum;

    [FroniusProprietaryImport("ACBRIDGE_POWERAPPARENT_SUM_MEAN_F32")]
    public double? PowerRealSum
    {
        get => powerRealSum;
        set => Set(ref powerRealSum, value);
    }

    private double? powerReactiveSum;

    [FroniusProprietaryImport("ACBRIDGE_POWERREACTIVE_SUM_MEAN_F32")]
    public double? PowerReactiveSum
    {
        get => powerReactiveSum;
        set => Set(ref powerReactiveSum, value);
    }

    private double? powerApparentSum;

    [FroniusProprietaryImport("ACBRIDGE_POWERAPPARENT_SUM_MEAN_F32")]
    public double? PowerApparentSum
    {
        get => powerApparentSum;
        set => Set(ref powerApparentSum, value);
    }

    private TimeSpan? backupModeUpTime;

    [FroniusProprietaryImport("ACBRIDGE_TIME_BACKUPMODE_UPTIME_SUM_F32")]
    public TimeSpan? BackupModeUpTime
    {
        get => backupModeUpTime;
        set => Set(ref backupModeUpTime, value);
    }

    private double? acVoltageL1;

    [FroniusProprietaryImport("ACBRIDGE_VOLTAGE_MEAN_01_F32")]
    public double? AcVoltageL1
    {
        get => acVoltageL1;
        set => Set(ref acVoltageL1, value, () =>
        {
            NotifyOfPropertyChange(nameof(AcPhaseVoltageAverage));
            NotifyOfPropertyChange(nameof(AcPowerL1));
        });
    }

    private double? acVoltageL2;

    [FroniusProprietaryImport("ACBRIDGE_VOLTAGE_MEAN_02_F32")]
    public double? AcVoltageL2
    {
        get => acVoltageL2;
        set => Set(ref acVoltageL2, value, () =>
        {
            NotifyOfPropertyChange(nameof(AcPhaseVoltageAverage));
            NotifyOfPropertyChange(nameof(AcPowerL2));
        });
    }

    private double? acVoltageL3;

    [FroniusProprietaryImport("ACBRIDGE_VOLTAGE_MEAN_03_F32")]
    public double? AcVoltageL3
    {
        get => acVoltageL3;
        set => Set(ref acVoltageL3, value, () =>
        {
            NotifyOfPropertyChange(nameof(AcPhaseVoltageAverage));
            NotifyOfPropertyChange(nameof(AcPowerL3));
        });
    }

    private double? acVoltageL12;

    [FroniusProprietaryImport("ACBRIDGE_VOLTAGE_MEAN_12_F32")]
    public double? AcVoltageL12
    {
        get => acVoltageL12;
        set => Set(ref acVoltageL12, value, () => NotifyOfPropertyChange(nameof(AcLineVoltageAverage)));
    }

    private double? acVoltageL23;

    [FroniusProprietaryImport("ACBRIDGE_VOLTAGE_MEAN_23_F32")]
    public double? AcVoltageL23
    {
        get => acVoltageL23;
        set => Set(ref acVoltageL23, value, () => NotifyOfPropertyChange(nameof(AcLineVoltageAverage)));
    }

    private double? acVoltageL31;

    [FroniusProprietaryImport("ACBRIDGE_VOLTAGE_MEAN_31_F32")]
    public double? AcVoltageL31
    {
        get => acVoltageL31;
        set => Set(ref acVoltageL31, value, () => NotifyOfPropertyChange(nameof(AcLineVoltageAverage)));
    }

    private double? storageCurrent;

    [FroniusProprietaryImport("BAT_CURRENT_MEAN_F32")]
    public double? StorageCurrent
    {
        get => storageCurrent;
        set => Set(ref storageCurrent, value);
    }

    private double? storagePower;

    [FroniusProprietaryImport("BAT_POWERACTIVE_MEAN_F32")]
    public double? StoragePower
    {
        get => storagePower;
        set => Set(ref storagePower, value);
    }

    private double? storageVoltage;

    [FroniusProprietaryImport("BAT_VOLTAGE_OUTER_MEAN_01_F32")]
    public double? StorageVoltage
    {
        get => storageVoltage;
        set => Set(ref storageVoltage, value);
    }

    private double? pv1Current;

    [FroniusProprietaryImport("PV_CURRENT_MEAN_01_F32")]
    public double? Pv1Current
    {
        get => pv1Current;
        set => Set(ref pv1Current, value, () => NotifyOfPropertyChange(nameof(PvCurrentSum)));
    }

    private double? pv2Current;

    [FroniusProprietaryImport("PV_CURRENT_MEAN_02_F32")]
    public double? Pv2Current
    {
        get => pv2Current;
        set => Set(ref pv2Current, value, () => NotifyOfPropertyChange(nameof(PvCurrentSum)));
    }

    private double? pv1Power;

    [FroniusProprietaryImport("PV_POWERACTIVE_MEAN_01_F32")]
    public double? Pv1Power
    {
        get => pv1Power;
        set => Set(ref pv1Power, value, () => NotifyOfPropertyChange(nameof(PvPowerSum)));
    }

    private double? pv2Power;

    [FroniusProprietaryImport("PV_POWERACTIVE_MEAN_02_F32")]
    public double? Pv2Power
    {
        get => pv2Power;
        set => Set(ref pv2Power, value, () => NotifyOfPropertyChange(nameof(PvPowerSum)));
    }

    private double? pv1Voltage;

    [FroniusProprietaryImport("PV_VOLTAGE_MEAN_01_F32")]
    public double? Pv1Voltage
    {
        get => pv1Voltage;
        set => Set(ref pv1Voltage, value);
    }

    private double? pv2Voltage;

    [FroniusProprietaryImport("PV_VOLTAGE_MEAN_02_F32")]
    public double? Pv2Voltage
    {
        get => pv2Voltage;
        set => Set(ref pv2Voltage, value);
    }

    private TimeSpan? deviceUpTime;

    [FroniusProprietaryImport("DEVICE_TIME_UPTIME_SUM_F32")]
    public TimeSpan? DeviceUpTime
    {
        get => deviceUpTime;
        set => Set(ref deviceUpTime, value);
    }

    public double? AcPhaseVoltageAverage => (AcVoltageL1 + AcVoltageL2 + AcVoltageL3) / 3d;
    public double? AcLineVoltageAverage => (AcVoltageL12 + AcVoltageL23 + AcVoltageL31) / 3d;
    public double? AcCurrentSum => AcCurrentL1 + AcCurrentL2 + AcCurrentL3;

    public double? AcPowerL1 => AcVoltageL1 * AcCurrentL1;
    public double? AcPowerL2 => AcVoltageL2 * AcCurrentL2;
    public double? AcPowerL3 => AcVoltageL3 * AcCurrentL3;

    public double? PvPowerSum => Pv1Power + Pv2Power;
    public double? PvCurrentSum => Pv1Current + Pv2Current;

}