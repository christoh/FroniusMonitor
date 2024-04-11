namespace De.Hochstaetter.Fronius.Models.Gen24;

/*
 * mandatory field
 * Mode: Contains:
 * "produce-only",                      inverter only
 * "meter", "vague-meter",              inverter and meter
 * "bidirectional" or "ac-coupled"      inverter , meter and battery
 */

public enum SiteType : sbyte
{
    [EnumParse(ParseAs = "produce-only")] ProduceOnly,
    [EnumParse(ParseAs = "meter")] Meter,
    [EnumParse(ParseAs = "vague-meter")] VagueMeter,
    [EnumParse(ParseAs = "bidirectional")] BiDirectional,
    [EnumParse(ParseAs = "ac-coupled")] AcCoupled,
    [EnumParse(IsDefault = true)] Unknown,
}

[SuppressMessage("ReSharper", "StringLiteralTypo")]
public class Gen24PowerFlow : Gen24DeviceBase
{
    private SiteType? siteType;
    private static readonly IList<SmartMeterCalibrationHistoryItem> history = IoC.TryGet<IDataCollectionService>()?.SmartMeterHistory!;
    private static int oldSmartMeterHistoryCountProduced;
    private static int oldSmartMeterHistoryCountConsumed;

    [FroniusProprietaryImport("state", FroniusDataType.Attribute)]
    public SiteType? SiteType
    {
        get => siteType;
        set => Set(ref siteType, value);
    }

    public string? SiteTypeDisplayName => SiteType?.ToDisplayName();

    private string? backupModeDisplayName;

    [FroniusProprietaryImport("BackupMode", FroniusDataType.Attribute)]
    public string? BackupModeDisplayName
    {
        get => backupModeDisplayName;
        set => Set(ref backupModeDisplayName, value);
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
        set => Set(ref loadPower, value, () => NotifyOfPropertyChange(nameof(LoadPowerCorrected)));
    }

    private static double consumedFactor = 1;

    private static double ConsumedFactor
    {
        get
        {
            if (oldSmartMeterHistoryCountConsumed != history.Count)
            {
                var consumed = (IReadOnlyList<SmartMeterCalibrationHistoryItem>)history.Where(item => double.IsFinite(item.ConsumedOffset)).ToList();
                consumedFactor = CalculateSmartMeterFactor(consumed, false);
                oldSmartMeterHistoryCountConsumed = history.Count;
            }

            return consumedFactor;
        }
    }

    private static double producedFactor = 1;

    private static double ProducedFactor
    {
        get
        {
            if (oldSmartMeterHistoryCountProduced != history.Count)
            {
                var produced = (IReadOnlyList<SmartMeterCalibrationHistoryItem>)history.Where(item => double.IsFinite(item.ProducedOffset)).ToList();
                producedFactor = CalculateSmartMeterFactor(produced, true);
                oldSmartMeterHistoryCountProduced = history.Count;
            }

            return producedFactor;
        }
    }

    public double? LoadPowerCorrected => LoadPower + GridPower - GridPowerCorrected;

    public double? GridPowerCorrected => GridPower * (GridPower < 0 ? ProducedFactor : ConsumedFactor);

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
        set => Set(ref gridPower, value, () =>
        {
            NotifyOfPropertyChange(nameof(GridPowerCorrected));
            NotifyOfPropertyChange(nameof(LoadPowerCorrected));
        });
    }

    private double? solarPower;

    [FroniusProprietaryImport("PV_POWERACTIVE_MEAN_SUM_F64")]
    public double? SolarPower
    {
        get => solarPower;
        set => Set(ref solarPower, value);
    }

    private string? mainInverterId;

    [FroniusProprietaryImport("main", FroniusDataType.Attribute)]
    public string? MainInverterId
    {
        get => mainInverterId;
        set => Set(ref mainInverterId, value);
    }

    public IEnumerable<double> AllPowers => new[] { StoragePower, GridPower, SolarPower, LoadPower ?? -InverterAcPower }.Where(ps => ps.HasValue).Select(ps => ps!.Value);
    public double DcInputPower => new[] { StoragePower ?? 0, SolarPower ?? 0 }.Where(ps => ps > 0).Sum();
    public double PowerLoss => (StoragePower ?? 0) + (SolarPower ?? 0) - (InverterAcPower ?? 0);
    public double? Efficiency => 1 - PowerLoss / DcInputPower;

    private static double CalculateSmartMeterFactor(IReadOnlyList<SmartMeterCalibrationHistoryItem> list, bool isProduced)
    {
        if (list.Count < 2)
        {
            return 1.0;
        }

        var first = list[0];
        var last = list[^1];
        var rawEnergy = (isProduced ? last.EnergyRealProduced : last.EnergyRealConsumed) - (isProduced ? first.EnergyRealProduced : first.EnergyRealConsumed);
        var offsetEnergy = (isProduced ? last.ProducedOffset : last.ConsumedOffset) - (isProduced ? first.ProducedOffset : first.ConsumedOffset);
        return (rawEnergy + offsetEnergy) / rawEnergy;
    }
}
