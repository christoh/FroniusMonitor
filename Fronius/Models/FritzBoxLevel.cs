using System.Xml.Serialization;

namespace De.Hochstaetter.Fronius.Models
{
    [XmlType("levelcontrol")]
    public class FritzBoxLevel : BindableBase
    {
        [XmlElement("level")]
        public string? LevelAbsoluteString
        {
            get => FritzBoxDevice.GetStringValue(LevelAbsolute, 1);
            set => LevelAbsolute = FritzBoxDevice.GetDoubleValue(value, 1);
        }

        [XmlElement("levelpercentage")]
        public string? LevelString
        {
            get => FritzBoxDevice.GetStringValue(Level, 100);
            set => Level = FritzBoxDevice.GetDoubleValue(value, 100);
        }

        private double? level;
        [XmlIgnore]
        public double? Level
        {
            get => level;
            set => Set(ref level, value);
        }

        private double? levelAbsolute;
        [XmlIgnore]
        public double? LevelAbsolute
        {
            get => levelAbsolute;
            set => Set(ref levelAbsolute, value);
        }
    }
}
