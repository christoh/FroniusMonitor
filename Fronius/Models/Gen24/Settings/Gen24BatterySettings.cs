namespace De.Hochstaetter.Fronius.Models.Gen24.Settings
{
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
    public class Gen24BatterySettings : Gen24ParsingBase
    {
        [FroniusProprietaryImport("BAT_ENABLED", FroniusDataType.Root)]
        public bool? IsEnabled
        {
            get;
            set => Set(ref field, value);
        }

        [FroniusProprietaryImport("BAT_CALIBRATION", FroniusDataType.Root)]
        public bool? IsInCalibration
        {
            get;
            set => Set(ref field, value);
        }

        [FroniusProprietaryImport("BAT_M0_SOC_MODE", FroniusDataType.Root)]
        public SocLimits Limits
        {
            get;
            set => Set(ref field, value);
        }

        [FroniusProprietaryImport("BAT_M0_SOC_MAX", FroniusDataType.Root)]
        public byte? SocMax
        {
            get;
            set => Set(ref field, value);
        }

        [FroniusProprietaryImport("BAT_M0_SOC_MIN", FroniusDataType.Root)]
        public byte? SocMin
        {
            get;
            set => Set(ref field, value);
        }

        [FroniusProprietaryImport("BAT_TYPE", FroniusDataType.Root)]
        public string? Model
        {
            get;
            set => Set(ref field, value);
        }

        [FroniusProprietaryImport("HYB_BACKUP_CRITICALSOC", FroniusDataType.Root)]
        public byte? BackupCriticalSoc
        {
            get;
            set => Set(ref field, value);
        }

        [FroniusProprietaryImport("HYB_BACKUP_RESERVED", FroniusDataType.Root)]
        public byte? BackupReserve
        {
            get;
            set => Set(ref field, value);
        }

        [FroniusProprietaryImport("HYB_BM_CHARGEFROMAC", FroniusDataType.Root)]
        public bool? ChargeFromAc
        {
            get;
            set => Set(ref field, value);
        }

        [FroniusProprietaryImport("HYB_EVU_CHARGEFROMGRID", FroniusDataType.Root)]
        public bool? ChargeFromGrid
        {
            get;
            set => Set(ref field, value);
        }

        [FroniusProprietaryImport("HYB_BM_PACMIN", FroniusDataType.Root)]
        public int? BatteryAcChargingMaxPower
        {
            get;
            set => Set(ref field, value);
        }

        [FroniusProprietaryImport("HYB_EM_MODE", FroniusDataType.Root)]
        public OptimizationMode? Mode
        {
            get;
            set => Set(ref field, value);
        }

        [FroniusProprietaryImport("HYB_EM_POWER", FroniusDataType.Root)]
        public int? RequestedGridPower
        {
            get;
            set => Set(ref field, value);
        }

        [FroniusProprietaryImport("HYB_EVU_ACCOUPLED", FroniusDataType.Root)]
        public bool? IsAcCoupled
        {
            get;
            set => Set(ref field, value);
        }

        [FroniusProprietaryImport("PV_PEAK_POWER", FroniusDataType.Root)]
        public int? SolarPeakPower
        {
            get;
            set => Set(ref field, value);
        }

        [FroniusProprietaryImport("HYB_BACKUP_SYSTEMDEADLOCKPREVENTION", FroniusDataType.Root)]
        public bool? EnableSystemDeadlockPrevention
        {
            get;
            set => Set(ref field, value);
        }

        [FroniusProprietaryImport("supportSoc", FroniusDataType.Root)]
        public byte? SupportSocPercent
        {
            get;
            set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(SupportSoC)));
        }

        [FroniusProprietaryImport("supportSocHysteresisEnergy", FroniusDataType.Root)]
        public int? SupportSocHysteresisJoule
        {
            get;
            set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(SupportSocHysteresisWatts)));
        }

        public double? SupportSocHysteresisWatts
        {
            get => SupportSocHysteresisJoule / 3600d;
            set => SupportSocHysteresisJoule = !value.HasValue ? null : (int)Math.Round(value.Value * 3600, MidpointRounding.AwayFromZero);
        }

        [FroniusProprietaryImport("supportSocHysteresisMin", FroniusDataType.Root)]
        public byte? SupportSocHysteresisMinimumPercent
        {
            get;
            set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(SupportSocHysteresisMinimum)));
        }

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

        [FroniusProprietaryImport("supportSocMode", FroniusDataType.Root)]
        public SocLimits? SupportSocMode
        {
            get;
            set => Set(ref field, value);
        }

        [FroniusProprietaryImport("supportSocActive", FroniusDataType.Root)]
        public bool? EnableSupportSoc
        {
            get;
            set => Set(ref field, value);
        }

        [FroniusProprietaryImport("BAT_SERVICE_ON", FroniusDataType.Root)]
        public bool IsInServiceMode
        {
            get;
            set => Set(ref field, value);
        }

        public static Gen24BatterySettings Parse(JToken? token) => Gen24JsonService.ReadFroniusData<Gen24BatterySettings>(token);

        public override object Clone() => MemberwiseClone();
    }
}
