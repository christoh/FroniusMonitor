namespace De.Hochstaetter.Fronius.Models
{
    public abstract class EnergyCounterBase : BaseResponse
    {
        private double? totalEnergyWattHours;

        public double? TotalEnergyWattHours
        {
            get => totalEnergyWattHours;
            set => Set(ref totalEnergyWattHours, value, () =>
            {
                NotifyOfPropertyChange(nameof(TotalEnergyKiloWattHours));
                NotifyOfPropertyChange(nameof(TotalEnergyMegaWattHours));
            });
        }

        private double? dayEnergyWattHours;

        public double? DayEnergyWattHours
        {
            get => dayEnergyWattHours;

            set => Set(ref dayEnergyWattHours, value, () =>
            {
                NotifyOfPropertyChange(nameof(DayEnergyKiloWattHours));
                NotifyOfPropertyChange(nameof(DayEnergyMegaWattHours));
            });
        }

        private double? yearEnergyWattHours;

        public double? YearEnergyWattHours
        {
            get => yearEnergyWattHours;

            set => Set(ref yearEnergyWattHours, value, () =>
            {
                NotifyOfPropertyChange(nameof(YearEnergyKiloWattHours));
                NotifyOfPropertyChange(nameof(YearEnergyMegaWattHours));
            });
        }

        public double? DayEnergyKiloWattHours => DayEnergyWattHours / 1000;
        public double? YearEnergyKiloWattHours => YearEnergyWattHours / 1000;
        public double? TotalEnergyKiloWattHours => TotalEnergyWattHours / 1000;
        public double? DayEnergyMegaWattHours => DayEnergyWattHours / 1_000_000;
        public double? YearEnergyMegaWattHours => YearEnergyWattHours / 1_000_000;
        public double? TotalEnergyMegaWattHours => TotalEnergyWattHours / 1_000_000;
    }
}
