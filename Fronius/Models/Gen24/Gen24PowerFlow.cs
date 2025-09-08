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
    [EnumParse(ParseAs = "produce-only")]
    ProduceOnly,

    [EnumParse(ParseAs = "meter")]
    Meter,

    [EnumParse(ParseAs = "vague-meter")]
    VagueMeter,

    [EnumParse(ParseAs = "bidirectional")]
    BiDirectional,

    [EnumParse(ParseAs = "ac-coupled")]
    AcCoupled,

    [EnumParse(IsDefault = true)]
    Unknown,
}

[SuppressMessage("ReSharper", "StringLiteralTypo")]
public partial class Gen24PowerFlow : Gen24DeviceBase
{
    private static readonly IList<SmartMeterCalibrationHistoryItem>? history = IoC.TryGet<IDataCollectionService>()?.SmartMeterHistory!;
    private static int oldSmartMeterHistoryCountProduced;
    private static int oldSmartMeterHistoryCountConsumed;

    [JsonIgnore]
    [ObservableProperty]
    public partial double StoragePower { get; set; }

    [JsonIgnore]
    [ObservableProperty]
    public partial double LoadPower { get; set; }

    private static double ConsumedFactor
    {
        get
        {
            if (history != null && oldSmartMeterHistoryCountConsumed != history.Count)
            {
                field = CalculateSmartMeterFactor(false);
                oldSmartMeterHistoryCountConsumed = history.Count;
            }

            return field;
        }
    } = 1;

    private static double ProducedFactor
    {
        get
        {
            if (history != null && oldSmartMeterHistoryCountProduced != history.Count)
            {
                field = CalculateSmartMeterFactor(true);
                oldSmartMeterHistoryCountProduced = history.Count;
            }

            return field;
        }
    } = 1;

    public double LoadPowerCorrected => LoadPower + GridPower - GridPowerCorrected;

    public double GridPowerCorrected => GridPower * (GridPower < 0 ? ProducedFactor : ConsumedFactor);

    [JsonIgnore]
    [ObservableProperty]
    public partial double InverterAcPower { get; set; }

    [JsonIgnore]
    [ObservableProperty]
    public partial double GridPower { get; set; }

    [JsonIgnore]
    [ObservableProperty]
    public partial double SolarPower { get; set; }

    [JsonIgnore]
    public IEnumerable<double> AllPowers => [StoragePower, GridPower, SolarPower, LoadPower];

    [JsonIgnore]
    public double DcInputPower => new[] { StoragePower, SolarPower }.Where(ps => ps > 0).Sum();

    [JsonIgnore]
    public double AcInputPower => new[] { GridPower, LoadPower }.Where(ps => ps > 0).Sum();

    [JsonIgnore]
    public double AcOutputPower => new[] { GridPower, LoadPower }.Where(ps => ps < 0).Sum();

    [JsonIgnore]
    public double DcOutputPower => new[] { StoragePower, SolarPower }.Where(ps => ps < 0).Sum();

    [JsonIgnore]
    public double PowerLoss => AllPowers.Sum();

    [JsonIgnore]
    public double? Efficiency => 1 - PowerLoss / (AcInputPower - Math.Min(AcInputPower, -AcOutputPower) + DcInputPower - Math.Min(DcInputPower, -DcOutputPower));

    public static double CalculateSmartMeterFactor(bool isProduced)
    {
        var list = history?.Where(item => double.IsFinite(isProduced ? item.ProducedOffset : item.ConsumedOffset)).ToList() ?? [];

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
