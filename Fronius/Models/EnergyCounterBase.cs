namespace De.Hochstaetter.Fronius.Models
{
    public abstract class EnergyCounterBase : BaseResponse
    {
        public double? TotalEnergyWattHours { get; init; }
        public double? DayEnergyWattHours { get; init; }
        public double? YearEnergyWattHours { get; init; }
        public double? DayEnergyKiloWattHours => DayEnergyWattHours / 1000;
        public double? YearEnergyKiloWattHours => YearEnergyWattHours / 1000;
        public double? TotalEnergyKiloWattHours => TotalEnergyWattHours / 1000;
        public double? DayEnergyMegaWattHours => DayEnergyWattHours / 1_000_000;
        public double? YearEnergyMegaWattHours => YearEnergyWattHours / 1_000_000;
        public double? TotalEnergyMegaWattHours => TotalEnergyWattHours / 1_000_000;
    }
}
