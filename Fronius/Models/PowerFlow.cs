using De.Hochstaetter.Fronius.Localization;

namespace De.Hochstaetter.Fronius.Models
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
        ProduceOnly,
        Meter,
        VagueMeter,
        BiDirectional,
        AcCoupled,
        Unknown,
    }

    public class PowerFlow : EnergyCounterBase
    {
        private int? version;

        public int? Version
        {
            get => version;
            set => Set(ref version, value);
        }

        private bool? backupMode;

        public bool? BackupMode
        {
            get => backupMode;
            set => Set(ref backupMode, value);
        }

        private bool? storageStandby;

        public bool? StorageStandby
        {
            get => storageStandby;
            set => Set(ref storageStandby, value);
        }


        private MeterLocation? meterLocation;

        public MeterLocation? MeterLocation
        {
            get => meterLocation;
            set => Set(ref meterLocation, value);
        }

        private SiteType? siteType;

        public SiteType? SiteType
        {
            get => siteType;
            set => Set(ref siteType, value);
        }

        private double? storagePower;

        public double? StoragePower
        {
            get => storagePower;
            set => Set(ref storagePower, value, NotifyDependencies);
        }

        private double? gridPower;

        public double? GridPower
        {
            get => gridPower;
            set => Set(ref gridPower, value, NotifyDependencies);
        }

        private double? solarPower;

        public double? SolarPower
        {
            get => solarPower;
            set => Set(ref solarPower, value, NotifyDependencies);
        }

        private double? loadPower;

        public double? LoadPower
        {
            get => loadPower;
            set => Set(ref loadPower, value, NotifyDependencies);
        }

        private double? autonomy;

        public double? Autonomy
        {
            get => autonomy;
            set => Set(ref autonomy, value);
        }

        private double? selfConsumption;

        public double? SelfConsumption
        {
            get => selfConsumption;
            set => Set(ref selfConsumption, value);
        }

        private IEnumerable<double> AllPowers => new[] { StoragePower, GridPower, SolarPower, LoadPower }.Where(ps => ps.HasValue).Select(ps => ps!.Value);
        public double DcPower => (StoragePower ?? 0) + (SolarPower ?? 0);
        public double AcPower => (LoadPower ?? 0) + (GridPower ?? 0);
        public double PowerLoss => DcPower + AcPower;
        public double? Input => AllPowers.Any() ? AllPowers.Where(ps => ps > 0).Sum() : null;
        public double? Output => AllPowers.Any() ? AllPowers.Where(ps => ps < 0).Sum() : null;
        public double? Efficiency => 1 - PowerLoss / Input;
        public override string DisplayName => Resources.PowerFlow;

        private void NotifyDependencies()
        {
            NotifyOfPropertyChange(nameof(AllPowers));
            NotifyOfPropertyChange(nameof(DcPower));
            NotifyOfPropertyChange(nameof(AcPower));
            NotifyOfPropertyChange(nameof(PowerLoss));
            NotifyOfPropertyChange(nameof(Input));
            NotifyOfPropertyChange(nameof(Output));
            NotifyOfPropertyChange(nameof(Efficiency));
        }
    }
}
