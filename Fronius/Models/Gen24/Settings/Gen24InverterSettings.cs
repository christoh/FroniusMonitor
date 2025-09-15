namespace De.Hochstaetter.Fronius.Models.Gen24.Settings;

[SuppressMessage("ReSharper", "StringLiteralTypo")]
public partial class Gen24InverterSettings : Gen24ParsingBase
{
    [ObservableProperty]
    [FroniusProprietaryImport("systemName", FroniusDataType.Root)]
    public partial string? SystemName { get; set; } = string.Empty;

    [ObservableProperty]
    [FroniusProprietaryImport("timezone", FroniusDataType.Root)]
    public partial string? TimeZoneName { get; set; }

    [ObservableProperty]
    [FroniusProprietaryImport("timesync", FroniusDataType.Root)]
    public partial bool? TimeSync { get; set; }

    [ObservableProperty]
    public partial Gen24Mppt? Mppt { get; set; }

    [ObservableProperty]
    public partial Gen24PowerLimitSettings PowerLimitSettings { get; set; } = new();

    [ObservableProperty]
    public partial Gen24AcSystemSettings AcSystemSettings { get; set; } = new();

    public override object Clone()
    {
        var clone = new Gen24InverterSettings
        {
            SystemName = SystemName,
            TimeZoneName = TimeZoneName,
            TimeSync = TimeSync,
            Mppt = Mppt?.Clone() as Gen24Mppt,
            PowerLimitSettings = (Gen24PowerLimitSettings)PowerLimitSettings.Clone(),
        };

        return clone;
    }

    public static Gen24InverterSettings Parse(JToken? uiToken, JToken? mpptToken, JToken? powerLimitToken, JToken? systemToken)
    {
        var inverterSettings = Gen24JsonService.ReadFroniusData<Gen24InverterSettings>(uiToken);
        inverterSettings.Mppt = Gen24Mppt.Parse(mpptToken);
        inverterSettings.PowerLimitSettings = Gen24PowerLimitSettings.Parse(powerLimitToken);
        inverterSettings.AcSystemSettings = Gen24AcSystemSettings.Parse(systemToken);
        return inverterSettings;
    }

    public static Gen24InverterSettings Parse(JToken? configToken)
    {
        return Parse
        (
            configToken?["common"]?["ui"]?.Value<JToken>() ?? new JObject(),
            configToken?["powerunit"]?["powerunit"]?["mppt"]?.Value<JToken>() ?? new JObject(),
            configToken?["limit_settings"]?["powerLimits"]?.Value<JToken>() ?? new JObject(),
            configToken?["powerunit"]?["powerunit"]?["system"]?.Value<JToken>() ?? new JObject()
        );
    }

    public override string ToString() => SystemName ?? string.Empty;
}
