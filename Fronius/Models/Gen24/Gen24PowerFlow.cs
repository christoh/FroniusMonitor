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
    private static readonly IList<SmartMeterCalibrationHistoryItem> history = IoC.TryGet<ISolarSystemService>()?.SmartMeterHistory!;
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

    private static double consumedFactor = double.NaN;

    public static double ConsumedFactor
    {
        get
        {
            if (oldSmartMeterHistoryCountConsumed != history.Count || !double.IsFinite(consumedFactor))
            {
                var consumed = (IReadOnlyList<SmartMeterCalibrationHistoryItem>)history.Where(item => double.IsFinite(item.ConsumedOffset)).ToList();

                consumedFactor = consumed.Count < 2
                    ? 1
                    : (consumed[^1].EnergyRealConsumed + consumed[^1].ConsumedOffset - consumed[0].EnergyRealConsumed - consumed[0].ConsumedOffset) / (consumed[^1].EnergyRealConsumed - consumed[0].EnergyRealConsumed);

                oldSmartMeterHistoryCountConsumed = history.Count;
            }

            return consumedFactor;
        }
    }

    private static double producedFactor = double.NaN;

    public static double ProducedFactor
    {
        get
        {
            if (oldSmartMeterHistoryCountProduced != history.Count || !double.IsFinite(producedFactor))
            {
                var produced = (IReadOnlyList<SmartMeterCalibrationHistoryItem>)history.Where(item => double.IsFinite(item.ProducedOffset)).ToList();

                producedFactor = produced.Count < 2
                    ? 1
                    : (produced[^1].EnergyRealProduced + produced[^1].ProducedOffset - produced[0].EnergyRealProduced - produced[0].ProducedOffset) / (produced[^1].EnergyRealProduced - produced[0].EnergyRealProduced);

                oldSmartMeterHistoryCountProduced = history.Count;
            }

            return producedFactor;
        }
    }

    public double? LoadPowerCorrected => LoadPower + GridPower - GridPowerCorrected;

    public double? GridPowerCorrected
    {
        get { return GridPower * (GridPower < 0 ? ProducedFactor : ConsumedFactor); }
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

    public IEnumerable<double> AllPowers => new[] { StoragePower, GridPowerCorrected, SolarPower, LoadPowerCorrected ?? -InverterAcPower }.Where(ps => ps.HasValue).Select(ps => ps!.Value);
    public double DcPower => (StoragePower ?? 0) + (SolarPower ?? 0);
    public double AcPower => (LoadPowerCorrected ?? -InverterAcPower ?? 0) + (GridPowerCorrected ?? 0);
    public double PowerLoss => DcPower + AcPower;
    public double? Input => AllPowers.Any() ? AllPowers.Where(ps => ps > 0).Sum() : null;
    public double? Output => AllPowers.Any() ? AllPowers.Where(ps => ps < 0).Sum() : null;
    public double? Efficiency => 1 - PowerLoss / Input;

    //private void NotifyPowers()
    //{
    //    NotifyOfPropertyChange(nameof(AllPowers));
    //    NotifyOfPropertyChange(nameof(DcPower));
    //    NotifyOfPropertyChange(nameof(PowerLoss));
    //    NotifyOfPropertyChange(nameof(PowerLoss));
    //    NotifyOfPropertyChange(nameof(Input));
    //    NotifyOfPropertyChange(nameof(Output));
    //    NotifyOfPropertyChange(nameof(Efficiency));
    //}
}
