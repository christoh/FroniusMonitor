namespace De.Hochstaetter.Fronius.Models
{
    public class Settings : BindableBase
    {
        private string? baseUrl = "http://neufahrn.hochstaetter.de:10";

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
