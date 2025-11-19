namespace De.Hochstaetter.Fronius.Models.Gen24;

public partial class Gen24Config : BindableBase, ICloneable
{
    private static readonly Version minimumCommandApi = new(8, 4, 1);
    private static readonly Version minimumConfigApi = new(10, 2, 0);
    private static readonly Version minimumComponentsApi = new(1, 1, 0);
    private static readonly Version minimumStatusApi = new(6, 1, 0);
    
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

            if (Versions == null)
            {
                return null;
            }

            if
            (
                Versions.CommandApi < minimumCommandApi ||
                Versions.ConfigApi < minimumConfigApi ||
                Versions.ComponentsApi < minimumComponentsApi ||
                Versions.StatusApi < minimumStatusApi
            )
            {
                return string.Format(Resources.FirmwareTooOld, firmwareVersionString, "1.39.5-1");
            }

            if
            (
                Versions.CommandApi?.Major > minimumCommandApi.Major ||
                Versions.ConfigApi?.Major > minimumConfigApi.Major ||
                Versions.ComponentsApi?.Major > minimumComponentsApi.Major ||
                Versions.StatusApi?.Major > minimumStatusApi.Major
            )
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
