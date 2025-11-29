namespace De.Hochstaetter.Fronius.Models.Gen24.Settings;

public enum SocLimits : byte
{
    [EnumParse(ParseAs = "auto")] UseManufacturerDefault,
    [EnumParse(ParseAs = "manual")] Override
}

public enum OptimizationMode
{
    [EnumParse(ParseNumeric = true)] Manual = 1,
    [EnumParse(ParseNumeric = true)] Automatic = 0
}

[SuppressMessage("ReSharper", "StringLiteralTypo")]
public partial class Gen24BatterySettings : Gen24ParsingBase
{
    [ObservableProperty]
    public partial double? SocMinPreserve { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("BAT_ENABLED", FroniusDataType.Root)]
    public partial bool? IsEnabled { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("BAT_CALIBRATION", FroniusDataType.Root)]
    public partial bool? IsInCalibration { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("BAT_M0_SOC_MODE", FroniusDataType.Root)]
    public partial SocLimits Limits { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("BAT_M0_SOC_MAX", FroniusDataType.Root)]
    public partial byte? SocMax { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("BAT_M0_SOC_MIN", FroniusDataType.Root)]
    public partial byte? SocMin { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("BAT_TYPE", FroniusDataType.Root)]
    public partial string? Model { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("HYB_BACKUP_CRITICALSOC", FroniusDataType.Root)]
    public partial byte? BackupCriticalSoc { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("HYB_BACKUP_RESERVED", FroniusDataType.Root)]
    public partial byte? BackupReserve { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("HYB_BM_CHARGEFROMAC", FroniusDataType.Root)]
    public partial bool? ChargeFromAc { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("HYB_EVU_CHARGEFROMGRID", FroniusDataType.Root)]
    public partial bool? ChargeFromGrid { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("HYB_BM_PACMIN", FroniusDataType.Root)]
    public partial int? BatteryAcChargingMaxPower { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("HYB_EM_MODE", FroniusDataType.Root)]
    public partial OptimizationMode? Mode { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("HYB_EM_POWER", FroniusDataType.Root)]
    public partial int? RequestedGridPower { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("HYB_EVU_ACCOUPLED", FroniusDataType.Root)]
    public partial bool? IsAcCoupled { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("PV_PEAK_POWER", FroniusDataType.Root)]
    public partial int? SolarPeakPower { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("HYB_BACKUP_SYSTEMDEADLOCKPREVENTION", FroniusDataType.Root)]
    public partial bool? EnableSystemDeadlockPrevention { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SupportSoC))]
    [FroniusProprietaryImport("supportSoc", FroniusDataType.Root)]
    public partial byte? SupportSocPercent { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SupportSocHysteresisWatts))]
    [FroniusProprietaryImport("supportSocHysteresisEnergy", FroniusDataType.Root)]
    public partial int? SupportSocHysteresisJoule { get; set; }

    public double? SupportSocHysteresisWatts
    {
        get => SupportSocHysteresisJoule / 3600d;
        set => SupportSocHysteresisJoule = !value.HasValue ? null : (int)Math.Round(value.Value * 3600, MidpointRounding.AwayFromZero);
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SupportSocHysteresisMinimum))]
    [FroniusProprietaryImport("supportSocHysteresisMin", FroniusDataType.Root)]
    public partial byte? SupportSocHysteresisMinimumPercent { get; set; }

    public double? SupportSocHysteresisMinimum
    {
        get => SupportSocHysteresisMinimumPercent / 100d;
        set => SupportSocHysteresisMinimumPercent = !value.HasValue ? null : (byte)Math.Round(value.Value * 100, MidpointRounding.AwayFromZero);
    }

    public double? SupportSoC
    {
        get => SupportSocPercent / 100d;
        set => SupportSocPercent = !value.HasValue ? null : (byte)Math.Round(value.Value * 100d, MidpointRounding.AwayFromZero);
    }

    [ObservableProperty]
    [FroniusProprietaryImport("supportSocMode", FroniusDataType.Root)]
    public partial SocLimits? SupportSocMode { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("supportSocActive", FroniusDataType.Root)]
    public partial bool? EnableSupportSoc { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("BAT_SERVICE_ON", FroniusDataType.Root)]
    public partial bool IsInServiceMode { get; set; }

    public static Gen24BatterySettings Parse(JToken? token) => Gen24JsonService.ReadFroniusData<Gen24BatterySettings>(token);

    public override object Clone() => MemberwiseClone();
}