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
        public double? PowerLossWatts => new[] { StoragePower, GridPower, SolarPower, LoadPower }.Where(ps => ps.HasValue).Sum(ps => ps!.Value);
        public double? Autonomy { get; init; }
        public double? SelfConsumption { get; init; }

        public double? Efficiency
        {
            get
            {
                IReadOnlyList<double> allPowers = new[] { StoragePower, GridPower, SolarPower, LoadPower }.Where(ps => ps.HasValue).Select(ps => ps!.Value).ToArray();

                if (allPowers.Count == 0)
                {
                    return null;
                }

                var output = allPowers.Where(ps => ps < 0).Sum();
                var input = allPowers.Where(ps => ps > 0).Sum();

                return input < .000000001 ? null : -output / input;
            }
        }
    }
}
