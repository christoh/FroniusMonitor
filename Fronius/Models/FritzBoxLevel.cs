namespace De.Hochstaetter.Fronius.Models;

[XmlType("levelcontrol")]
[SuppressMessage("ReSharper", "StringLiteralTypo")]
public class FritzBoxLevel : BindableBase
{
    [XmlElement("level")]
    public string? LevelAbsoluteString
    {
        get => FritzBoxDevice.GetStringValue(LevelAbsolute, 1);
        set => LevelAbsolute = FritzBoxDevice.GetDoubleValue(value, 1);
    }

    [XmlElement("levelpercentage")]
    [JsonIgnore]
    public string? LevelString
    {
        get => FritzBoxDevice.GetStringValue(Level, 100);
        set => Level = FritzBoxDevice.GetDoubleValue(value, 100);
    }

    [XmlIgnore]
    public double? Level
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(LevelString)));
    }

    [XmlIgnore]
    public double? LevelAbsolute
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(LevelAbsoluteString)));
    }
}