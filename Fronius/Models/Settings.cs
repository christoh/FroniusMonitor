using System.Xml.Serialization;

namespace De.Hochstaetter.Fronius.Models
{
    public class Settings : BindableBase
    {
        private string? baseUrlFronius = "http://192.168.44.10";//"http://neufahrn.hochstaetter.de:10";

        [XmlElement]
        public string? BaseUrlFronius
        {
            get => baseUrlFronius;
            set => Set(ref baseUrlFronius, value);
        }

        private string? baseUrlFritzBox = "http://192.168.44.11";

        public string? BaseUrlFritzBox
        {
            get => baseUrlFritzBox;
            set => Set(ref baseUrlFritzBox, value);
        }

        private double consumedEnergyOffsetWatts = 457000 - 2977910;

        [XmlElement("ConsumedEnergyOffSet")]
        public double ConsumedEnergyOffsetWatts
        {
            get => consumedEnergyOffsetWatts;
            set => Set(ref consumedEnergyOffsetWatts, value);
        }

        private double producedEnergyOffsetWatts=-310;

        [XmlElement("ConsumedEnergyOffSet")]
        public double ProducedEnergyOffsetWatts
        {
            get => producedEnergyOffsetWatts;
            set => Set(ref producedEnergyOffsetWatts, value);
        }
    }
}
