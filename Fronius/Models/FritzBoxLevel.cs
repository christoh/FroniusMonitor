namespace De.Hochstaetter.Fronius.Models;

[XmlType("levelcontrol")]
[SuppressMessage("ReSharper", "StringLiteralTypo")]
public partial class FritzBoxLevel : BindableBase
{
    [XmlElement("level")]
    [JsonIgnore]
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

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LevelString))]
    [XmlIgnore]
    public partial double? Level { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LevelAbsoluteString))]
    [XmlIgnore]
    public partial double? LevelAbsolute { get; set; }
}