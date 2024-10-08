using De.Hochstaetter.Fronius.Models.Gen24.Settings;

namespace De.Hochstaetter.Fronius.Models.Gen24;

public class Gen24Config : BindableBase, ICloneable
{
    private Gen24Versions? versions;

    public Gen24Versions? Versions
    {
        get => versions;
        set => Set(ref versions, value, () => NotifyOfPropertyChange(nameof(VersionWarning)));
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

    private Gen24BatterySettings? batterySettings;

    public Gen24BatterySettings? BatterySettings
    {
        get => batterySettings;
        set => Set(ref batterySettings, value);
    }

    private double? maxAcPower;

    public double? MaxAcPower
    {
        get => maxAcPower;
        set => Set(ref maxAcPower, value);
    }


    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    public string? VersionWarning
    {
        get
        {
            Version? firmwareVersion = null;
            Versions?.SwVersions.TryGetValue("DEVICEGROUP", out firmwareVersion);
            var firmwareVersionString = firmwareVersion?.ToLinuxString() ?? Resources.Unknown;

            if (Versions?.CommandApi < new Version(6, 1) || Versions?.ConfigApi < new Version(8, 4))
            {
                return string.Format(Resources.FirmwareTooOld, firmwareVersionString, "1.33.8-1");
            }

            if (Versions?.CommandApi >= new Version(7, 0) || Versions?.ConfigApi >= new Version(9, 0))
            {
                return string.Format(Resources.FirmwareTooNew, firmwareVersionString);
            }

            return null;
        }
    }

    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    public static Gen24Config Parse(JToken versionsToken, JToken componentsToken, JToken configToken)
    {
        var gen24Config = new Gen24Config
        {
            Versions = Gen24Versions.Parse(versionsToken),
            Components = Gen24Components.Parse(componentsToken),
            InverterSettings = Gen24InverterSettings.Parse(configToken),
            MaxAcPower = configToken["powerunit"]?["powerunit"]?["system"]?.Value<double>("DEVICE_POWERACTIVE_NOMINAL_F32"),
            BatterySettings = Gen24BatterySettings.Parse(configToken?["batteries"]?["batteries"]),
        };

        return gen24Config;
    }

    public object Clone()
    {
        var gen24Config = new Gen24Config
        {
            Versions = Versions,
            Components = Components,
            InverterSettings = InverterSettings?.Clone() as Gen24InverterSettings,
        };

        return gen24Config;
    }
}
