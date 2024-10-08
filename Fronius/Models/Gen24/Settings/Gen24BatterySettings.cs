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
        private bool? isEnabled;

        [FroniusProprietaryImport("BAT_ENABLED", FroniusDataType.Root)]
        public bool? IsEnabled
        {
            get => isEnabled;
            set => Set(ref isEnabled, value);
        }

        private bool? isInCalibration;

        [FroniusProprietaryImport("BAT_CALIBRATION", FroniusDataType.Root)]
        public bool? IsInCalibration
        {
            get => isInCalibration;
            set => Set(ref isInCalibration, value);
        }

        private SocLimits limits;

        [FroniusProprietaryImport("BAT_M0_SOC_MODE", FroniusDataType.Root)]
        public SocLimits Limits
        {
            get => limits;
            set => Set(ref limits, value);
        }

        private byte? socMax;

        [FroniusProprietaryImport("BAT_M0_SOC_MAX", FroniusDataType.Root)]
        public byte? SocMax
        {
            get => socMax;
            set => Set(ref socMax, value);
        }

        private byte? socMin;

        [FroniusProprietaryImport("BAT_M0_SOC_MIN", FroniusDataType.Root)]
        public byte? SocMin
        {
            get => socMin;
            set => Set(ref socMin, value);
        }

        private string? model;

        [FroniusProprietaryImport("BAT_TYPE", FroniusDataType.Root)]
        public string? Model
        {
            get => model;
            set => Set(ref model, value);
        }

        private byte? backupCriticalSoc;

        [FroniusProprietaryImport("HYB_BACKUP_CRITICALSOC", FroniusDataType.Root)]
        public byte? BackupCriticalSoc
        {
            get => backupCriticalSoc;
            set => Set(ref backupCriticalSoc, value);
        }

        private byte? backupReserve;

        [FroniusProprietaryImport("HYB_BACKUP_RESERVED", FroniusDataType.Root)]
        public byte? BackupReserve
        {
            get => backupReserve;
            set => Set(ref backupReserve, value);
        }

        private bool? chargeFromAc;

        [FroniusProprietaryImport("HYB_BM_CHARGEFROMAC", FroniusDataType.Root)]
        public bool? ChargeFromAc
        {
            get => chargeFromAc;
            set => Set(ref chargeFromAc, value);
        }

        private bool? chargeFromGrid;

        [FroniusProprietaryImport("HYB_EVU_CHARGEFROMGRID", FroniusDataType.Root)]
        public bool? ChargeFromGrid
        {
            get => chargeFromGrid;
            set => Set(ref chargeFromGrid, value);
        }

        private int? batteryAcChargingMaxPower;

        [FroniusProprietaryImport("HYB_BM_PACMIN", FroniusDataType.Root)]
        public int? BatteryAcChargingMaxPower
        {
            get => batteryAcChargingMaxPower;
            set => Set(ref batteryAcChargingMaxPower, value);
        }

        private OptimizationMode? mode;

        [FroniusProprietaryImport("HYB_EM_MODE", FroniusDataType.Root)]
        public OptimizationMode? Mode
        {
            get => mode;
            set => Set(ref mode, value);
        }

        private int? requestedGridPower;

        [FroniusProprietaryImport("HYB_EM_POWER", FroniusDataType.Root)]
        public int? RequestedGridPower
        {
            get => requestedGridPower;
            set => Set(ref requestedGridPower, value);
        }

        private bool? isAcCoupled;

        [FroniusProprietaryImport("HYB_EVU_ACCOUPLED", FroniusDataType.Root)]
        public bool? IsAcCoupled
        {
            get => isAcCoupled;
            set => Set(ref isAcCoupled, value);
        }

        private int? solarPeakPower;

        [FroniusProprietaryImport("PV_PEAK_POWER", FroniusDataType.Root)]
        public int? SolarPeakPower
        {
            get => solarPeakPower;
            set => Set(ref solarPeakPower, value);
        }

        private bool? enableSystemDeadlockPrevention;

        [FroniusProprietaryImport("HYB_BACKUP_SYSTEMDEADLOCKPREVENTION", FroniusDataType.Root)]
        public bool? EnableSystemDeadlockPrevention
        {
            get => enableSystemDeadlockPrevention;
            set => Set(ref enableSystemDeadlockPrevention, value);
        }

        private byte? supportSocPercent;
        [FroniusProprietaryImport("supportSoc", FroniusDataType.Root)]
        public byte? SupportSocPercent
        {
            get => supportSocPercent;
            set => Set(ref supportSocPercent, value, () => NotifyOfPropertyChange(nameof(SupportSoC)));
        }

        private int? supportSocHysteresisJoule;
        [FroniusProprietaryImport("supportSocHysteresisEnergy", FroniusDataType.Root)]
        public int? SupportSocHysteresisJoule
        {
            get => supportSocHysteresisJoule;
            set => Set(ref supportSocHysteresisJoule, value, ()=>NotifyOfPropertyChange(nameof(SupportSocHysteresisWatts)));
        }

        public double? SupportSocHysteresisWatts
        {
            get => SupportSocHysteresisJoule / 3600d;
            set => SupportSocHysteresisJoule = !value.HasValue ? null : (int)Math.Round(value.Value * 3600, MidpointRounding.AwayFromZero);
        }

        private byte? supportSocHysteresisMinimumPercent;
        [FroniusProprietaryImport("supportSocHysteresisMin", FroniusDataType.Root)]
        public byte? SupportSocHysteresisMinimumPercent
        {
            get => supportSocHysteresisMinimumPercent;
            set => Set(ref supportSocHysteresisMinimumPercent, value,()=>NotifyOfPropertyChange(nameof(SupportSocHysteresisMinimum)));
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

        private SocLimits? supportSocMode;
        [FroniusProprietaryImport("supportSocMode", FroniusDataType.Root)]
        public SocLimits? SupportSocMode
        {
            get => supportSocMode;
            set => Set(ref supportSocMode, value);
        }

        private bool? enableSupportSoc;
        [FroniusProprietaryImport("supportSocActive", FroniusDataType.Root)]
        public bool? EnableSupportSoc
        {
            get => enableSupportSoc;
            set => Set(ref enableSupportSoc, value);
        }

        private bool isInServiceMode;

        [FroniusProprietaryImport("BAT_SERVICE_ON", FroniusDataType.Root)]
        public bool IsInServiceMode
        {
            get => isInServiceMode;
            set => Set(ref isInServiceMode, value);
        }

        public static Gen24BatterySettings Parse(JToken? token) => Gen24JsonService.ReadFroniusData<Gen24BatterySettings>(token);

        public override object Clone() => MemberwiseClone();
    }
}
