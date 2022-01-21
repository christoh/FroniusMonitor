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

        private double consumedEnergyOffsetWattHours = 457000 - 2977910;

        [XmlElement("ConsumedEnergyOffSet")]
        public double ConsumedEnergyOffsetWattHours
        {
            get => consumedEnergyOffsetWattHours;
            set => Set(ref consumedEnergyOffsetWattHours, value);
        }

        private double producedEnergyOffsetWattHours=-360;

        [XmlElement("ConsumedEnergyOffSet")]
        public double ProducedEnergyOffsetWattHours
        {
            get => producedEnergyOffsetWattHours;
            set => Set(ref producedEnergyOffsetWattHours, value);
        }
    }
}
