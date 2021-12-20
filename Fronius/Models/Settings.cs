namespace De.Hochstaetter.Fronius.Models
{
    public class Settings : BindableBase
    {
        private string? baseUrl = "http://fronius1.hochstaetter.de";

        public string? BaseUrl
        {
            get => baseUrl;
            set => Set(ref baseUrl, value);
        }

        private double consumedEnergyOffSet;

        public double ConsumedEnergyOffSet
        {
            get => consumedEnergyOffSet;
            set => Set(ref consumedEnergyOffSet, value);
        }

        private double producedEnergyOffset;

        public double ProducedEnergyOffset
        {
            get => producedEnergyOffset;
            set => Set(ref producedEnergyOffset, value);
        }
    }
}
