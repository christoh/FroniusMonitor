namespace De.Hochstaetter.Fronius.Models.Gen24
{
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
        [NotifyPropertyChangedFor(nameof(PowerLoss), nameof(Efficiency))]
        [ObservableProperty]
        public partial double StoragePower { get; set; }

        [JsonIgnore]
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(LoadPowerCorrected), nameof(PowerLoss), nameof(Efficiency))]
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
        [NotifyPropertyChangedFor(nameof(Efficiency), nameof(PowerLoss))]
        public partial double InverterAcPower { get; set; }

        [JsonIgnore]
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(GridPowerCorrected), nameof(LoadPowerCorrected), nameof(PowerLoss), nameof(Efficiency))]
        public partial double GridPower { get; set; }

        [JsonIgnore]
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(PowerLoss), nameof(Efficiency))]
        public partial double SolarPower { get; set; }

        [JsonIgnore]
        public double PowerLoss => StoragePower - InverterAcPower + SolarPower;

        [JsonIgnore]
        public double? Efficiency
        {
            get
            {
                var dcInputPower = SolarPower + (StoragePower > 0 ? StoragePower : 0);
                var dcOutputPower= StoragePower < 0 ? StoragePower : 0;
                return dcInputPower < 0.000001 ? null : (InverterAcPower - dcOutputPower) / dcInputPower;
            }
        }

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
}
