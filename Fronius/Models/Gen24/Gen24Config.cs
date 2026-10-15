namespace De.Hochstaetter.Fronius.Models.Gen24;

public partial class Gen24Config : BindableBase, ICloneable
{
    private static readonly Version minimumCommandApi = new(10, 1, 0);
    private static readonly Version minimumConfigApi = new(10, 3, 0);
    private static readonly Version minimumComponentsApi = new(1, 1, 0);
    private static readonly Version minimumStatusApi = new(6, 2, 0);

    // The newest major version of each API this software has been checked against. A major version above it may
    // have breaking changes. configApi 11 came with firmware 1.42.9-1; everything this software reads from the
    // config was still where it was, so it is accepted.
    private const int MaximumCommandApiMajor = 10;
    private const int MaximumConfigApiMajor = 11;
    private const int MaximumComponentsApiMajor = 1;
    private const int MaximumStatusApiMajor = 6;

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
                return string.Format(Resources.FirmwareTooOld, firmwareVersionString, "1.41.10-1");
            }

            if
            (
                Versions.CommandApi?.Major > MaximumCommandApiMajor ||
                Versions.ConfigApi?.Major > MaximumConfigApiMajor ||
                Versions.ComponentsApi?.Major > MaximumComponentsApiMajor ||
                Versions.StatusApi?.Major > MaximumStatusApiMajor
            )
            {
                return string.Format(Resources.FirmwareTooNew, firmwareVersionString);
            }

            return null;
        }
    }

    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    public static Gen24Config Parse(JsonNode versionsToken, JsonNode componentsToken, JsonNode configToken)
    {
        var gen24Config = new Gen24Config
        {
            Versions = Gen24Versions.Parse(versionsToken),
            Components = Gen24Components.Parse(componentsToken),
            InverterSettings = Gen24InverterSettings.Parse(configToken),
            MaxAcPower = configToken["powerunit"]?["powerunit"]?["system"]?["DEVICE_POWERACTIVE_NOMINAL_F32"].AsDouble(),
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
