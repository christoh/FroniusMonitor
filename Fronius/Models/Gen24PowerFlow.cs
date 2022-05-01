using System.Diagnostics.CodeAnalysis;
using De.Hochstaetter.Fronius.Attributes;

namespace De.Hochstaetter.Fronius.Models;

[SuppressMessage("ReSharper", "StringLiteralTypo")]
public class Gen24PowerFlow : Gen24DeviceBase
{
    private SiteType? siteType;

    [FroniusProprietaryImport("state", FroniusDataType.Attribute)]
    public SiteType? SiteType
    {
        get => siteType;
        set => Set(ref siteType, value);
    }

    private string? backupMode;

    [FroniusProprietaryImport("BackupMode", FroniusDataType.Attribute)]
    public string? BackupMode
    {
        get => backupMode;
        set => Set(ref backupMode, value);
    }

    private double? inverterLifeTimeEnergyProduced;

    [FroniusProprietaryImport("ACBRIDGE_ENERGYACTIVE_PRODUCED_SUM_U64", Unit.Joule)]
    public double? InverterLifeTimeEnergyProduced
    {
        get => inverterLifeTimeEnergyProduced;
        set => Set(ref inverterLifeTimeEnergyProduced, value);
    }

    private double? inverterPowerNominal;

    [FroniusProprietaryImport("ACBRIDGE_POWERACTIVE_AC_NOMINAL_F64")]
    public double? InverterPowerNominal
    {
        get => inverterPowerNominal;
        set => Set(ref inverterPowerNominal, value);
    }

    private double? storagePowerConfigured;

    [FroniusProprietaryImport("BAT_POWERACTIVE_DC_CONFIGURED_F64")]
    public double? StoragePowerConfigured
    {
        get => storagePowerConfigured;
        set => Set(ref storagePowerConfigured, value);
    }

    private double? storagePower;

    [FroniusProprietaryImport("BAT_POWERACTIVE_MEAN_SUM_F64")]
    public double? StoragePower
    {
        get => storagePower;
        set => Set(ref storagePower, value);
    }

    private double? loadPower;

    [FroniusProprietaryImport("GRID_POWERACTIVE_LOAD_MEAN_SUM_F64")]
    public double? LoadPower
    {
        get => loadPower;
        set => Set(ref loadPower, value);
    }

    private double? inverterAcPower;

    [FroniusProprietaryImport("GRID_POWERACTIVE_GENERATED_MEAN_SUM_F64")]
    public double? InverterAcPower
    {
        get => inverterAcPower;
        set => Set(ref inverterAcPower, value);
    }

    private double? gridPower;

    [FroniusProprietaryImport("GRID_POWERACTIVE_MEAN_SUM_F64")]
    public double? GridPower
    {
        get => gridPower;
        set => Set(ref gridPower, value);
    }

    private double? pvPower;

    [FroniusProprietaryImport("PV_POWERACTIVE_MEAN_SUM_F64")]
    public double? PvPower
    {
        get => pvPower;
        set => Set(ref pvPower, value);
    }
}
