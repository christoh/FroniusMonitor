using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using De.Hochstaetter.Fronius.Attributes;

namespace De.Hochstaetter.Fronius.Models;

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
        set => Set(ref inverterEnergyConsumedL3, value, ()=>NotifyOfPropertyChange(nameof(InverterEnergyConsumedSum)));
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
        set => Set(ref inverterRealPowerL1, value);
    }

    private double? inverterRealPowerL2;
    [FroniusProprietaryImport("ACBRIDGE_POWERACTIVE_MEAN_02_F32")]
    public double? InverterRealPowerL2
    {
        get => inverterRealPowerL2;
        set => Set(ref inverterRealPowerL2, value);
    }

    private double? inverterRealPowerL3;
    [FroniusProprietaryImport("ACBRIDGE_POWERACTIVE_MEAN_03_F32")]
    public double? InverterRealPowerL3
    {
        get => inverterRealPowerL3;
        set => Set(ref inverterRealPowerL3, value);
    }

    private double? inverterRealPowerSum;
    [FroniusProprietaryImport("ACBRIDGE_POWERACTIVE_SUM_MEAN_F32")]
    public double? InverterRealPowerSum
    {
        get => inverterRealPowerSum;
        set => Set(ref inverterRealPowerSum, value);
    }

    private double? inverterApparentPowerL1;
    [FroniusProprietaryImport("ACBRIDGE_POWERAPPARENT_MEAN_01_F32")]
    public double? InverterApparentPowerL1
    {
        get => inverterApparentPowerL1;
        set => Set(ref inverterApparentPowerL1, value);
    }

    private double? inverterApparentPowerL2;
    [FroniusProprietaryImport("ACBRIDGE_POWERAPPARENT_MEAN_02_F32")]
    public double? InverterApparentPowerL2
    {
        get => inverterApparentPowerL2;
        set => Set(ref inverterApparentPowerL2, value);
    }

    private double? inverterApparentPowerL3;
    [FroniusProprietaryImport("ACBRIDGE_POWERAPPARENT_MEAN_03_F32")]
    public double? InverterApparentPowerL3
    {
        get => inverterApparentPowerL3;
        set => Set(ref inverterApparentPowerL3, value);
    }

    private double? inverterApparentPowerSum;
    [FroniusProprietaryImport("ACBRIDGE_POWERAPPARENT_SUM_MEAN_F32")]
    public double? InverterApparentPowerSum
    {
        get => inverterApparentPowerSum;
        set => Set(ref inverterApparentPowerSum, value);
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
        get => inverterPhaseVoltageL1;
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


}