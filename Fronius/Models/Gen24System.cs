using System.Collections.ObjectModel;

namespace De.Hochstaetter.Fronius.Models
{
    public class Gen24System : BindableBase
    {
        private Gen24Inverter? inverter;

        public Gen24Inverter? Inverter
        {
            get => inverter;
            set => Set(ref inverter, value);
        }

        private Gen24Storage? storage;

        public Gen24Storage? Storage
        {
            get => storage;
            set => Set(ref storage, value);
        }

        private Gen24Restrictions? restrictions;

        public Gen24Restrictions? Restrictions
        {
            get => restrictions;
            set => Set(ref restrictions, value);
        }

        public ObservableCollection<Gen24PowerMeter> Meters { get; } = new ObservableCollection<Gen24PowerMeter>();

        public Gen24PowerMeter? PrimaryPowerMeter => Meters.SingleOrDefault(m => m.Usage == MeterUsage.Inverter);
    }
}
