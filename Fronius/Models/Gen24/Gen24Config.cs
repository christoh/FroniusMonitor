namespace De.Hochstaetter.Fronius.Models.Gen24;

public partial class Gen24Config : BindableBase, ICloneable
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(VersionWarning))]
    public partial Gen24Versions? Versions { get; set; }

    [ObservableProperty]
    public partial Gen24Components? Components { get; set; }

    [ObservableProperty]
    public partial Gen24InverterSettings? InverterSettings { get; set; }

    [ObservableProperty]
    public partial Gen24BatterySettings? BatterySettings { get; set; }

    [ObservableProperty]
    public partial double? MaxAcPower { get; set; }

    public string? VersionWarning
    {
        get
        {
            Version? firmwareVersion = null;
            Versions?.SwVersions.TryGetValue("GEN24", out firmwareVersion);
            var firmwareVersionString = firmwareVersion?.ToLinuxString() ?? Resources.Unknown;

            if (Versions?.CommandApi < new Version(8,0) || Versions?.ConfigApi < new Version(9, 1))
            {
                return string.Format(Resources.FirmwareTooOld, firmwareVersionString, "1.36.5-1");
            }

            if (Versions?.CommandApi >= new Version(9, 0) || Versions?.ConfigApi >= new Version(11, 0))
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
            BatterySettings = Gen24BatterySettings.Parse(configToken["batteries"]?["batteries"]),
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
