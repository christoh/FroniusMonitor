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
        private IEnumerable<double> AllPowers => new[] { StoragePower, GridPower, SolarPower, LoadPower }.Where(ps => ps.HasValue).Select(ps => ps!.Value);
        public double DcPower => (StoragePower ?? 0) + (SolarPower ?? 0);
        public double AcPower => (LoadPower ?? 0) + (GridPower ?? 0);
        public override string DisplayName => Resources.PowerFlow;
        public int? Version { get; init; }
        public bool? BackupMode { get; init; }
        public bool? StorageStandby { get; init; }
        public MeterLocation? MeterLocation { get; init; }
        public SiteType? SiteType { get; init; }
        public double? StoragePower { get; init; }
        public double? GridPower { get; init; }
        public double? SolarPower { get; init; }
        public double? LoadPower { get; init; }
        public double PowerLoss => DcPower + AcPower;
        public double? Input => AllPowers.Any() ? AllPowers.Where(ps => ps > 0).Sum() : null;
        public double? Output => AllPowers.Any() ? AllPowers.Where(ps => ps < 0).Sum() : null;
        public double? Autonomy { get; init; }
        public double? SelfConsumption { get; init; }

        public double? Efficiency => 1 - PowerLoss / Input;
    }
}
