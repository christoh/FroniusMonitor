using De.Hochstaetter.Fronius.Models.Gen24.Settings;

namespace De.Hochstaetter.Fronius.Models.Gen24;

public class Gen24Config : BindableBase, ICloneable
{
    private Gen24Versions? versions;

    public Gen24Versions? Versions
    {
        get => versions;
        set => Set(ref versions, value);
    }

    private Gen24Components? components;

    public Gen24Components? Components
    {
        get => components;
        set => Set(ref components, value);
    }

    private Gen24InverterSettings? inverterSettings;

    public Gen24InverterSettings? InverterSettings
    {
        get => inverterSettings;
        set => Set(ref inverterSettings, value);
    }

    public static Gen24Config Parse(JToken versionsToken, JToken componentsToken, JToken commonToken, JToken mpptToken)
    {
        var gen24Config = new Gen24Config
        {
            Versions = Gen24Versions.Parse(versionsToken),
            Components = Gen24Components.Parse(componentsToken),
            InverterSettings = Gen24InverterSettings.Parse(commonToken, mpptToken),
        };

        return gen24Config;
    }

    public object Clone()
    {
        var gen24Config = new Gen24Config()
        {
            Versions = Versions,
            Components = Components,
            InverterSettings = InverterSettings?.Clone() as Gen24InverterSettings,
        };

        return gen24Config;
    }
}
