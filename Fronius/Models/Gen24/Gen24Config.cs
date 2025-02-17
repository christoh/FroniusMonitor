using De.Hochstaetter.Fronius.Models.Gen24.Settings;

namespace De.Hochstaetter.Fronius.Models.Gen24;

public class Gen24Config : BindableBase, ICloneable
{
    public Gen24Versions? Versions
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(VersionWarning)));
    }

    public Gen24Components? Components
    {
        get;
        set => Set(ref field, value);
    }

    public Gen24InverterSettings? InverterSettings
    {
        get;
        set => Set(ref field, value);
    }

    public Gen24BatterySettings? BatterySettings
    {
        get;
        set => Set(ref field, value);
    }

    public double? MaxAcPower
    {
        get;
        set => Set(ref field, value);
    }


    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    public string? VersionWarning
    {
        get
        {
            Version? firmwareVersion = null;
            Versions?.SwVersions.TryGetValue("DEVICEGROUP", out firmwareVersion);
            var firmwareVersionString = firmwareVersion?.ToLinuxString() ?? Resources.Unknown;

            if (Versions?.CommandApi < new Version(7,0) || Versions?.ConfigApi < new Version(8, 4))
            {
                return string.Format(Resources.FirmwareTooOld, firmwareVersionString, "1.33.8-1");
            }

            if (Versions?.CommandApi >= new Version(8, 0) || Versions?.ConfigApi >= new Version(9, 0))
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
