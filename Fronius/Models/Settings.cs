using System.Xml.Serialization;

namespace De.Hochstaetter.Fronius.Models
{
    public class Settings : BindableBase
    {
        private string? baseUrl = "http://192.168.44.10";//"http://neufahrn.hochstaetter.de:10";

        [XmlElement]
        public string? BaseUrl
        {
            get => baseUrl;
            set => Set(ref baseUrl, value);
        }

        private double consumedEnergyOffSetWatts = 457000 - 2977910;

        [XmlElement("ConsumedEnergyOffSet")]
        public double ConsumedEnergyOffSetWatts
        {
            get => consumedEnergyOffSetWatts;
            set => Set(ref consumedEnergyOffSetWatts, value);
        }

        private double producedEnergyOffsetWatts=3;

        [XmlElement("ConsumedEnergyOffSet")]
        public double ProducedEnergyOffsetWatts
        {
            get => producedEnergyOffsetWatts;
            set => Set(ref producedEnergyOffsetWatts, value);
        }
    }
}
