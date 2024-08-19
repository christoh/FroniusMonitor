namespace De.Hochstaetter.Fronius.Models.Gen24.Settings;

[SuppressMessage("ReSharper", "StringLiteralTypo")]
public class Gen24InverterSettings : Gen24ParsingBase
{
    private string? systemName = string.Empty;

    [FroniusProprietaryImport("systemName", FroniusDataType.Root)]
    public string? SystemName
    {
        get => systemName;
        set => Set(ref systemName, value);
    }

    private string? timeZoneName;

    [FroniusProprietaryImport("timezone", FroniusDataType.Root)]
    public string? TimeZoneName
    {
        get => timeZoneName;
        set => Set(ref timeZoneName, value);
    }

    private bool? timeSync;

    [FroniusProprietaryImport("timesync", FroniusDataType.Root)]
    public bool? TimeSync
    {
        get => timeSync;
        set => Set(ref timeSync, value);
    }

    private Gen24Mppt? mppt;

    public Gen24Mppt? Mppt
    {
        get => mppt;
        set => Set(ref mppt, value);
    }

    private Gen24PowerLimitSettings powerLimitSettings = new();

    public Gen24PowerLimitSettings PowerLimitSettings
    {
        get => powerLimitSettings;
        set => Set(ref powerLimitSettings, value);
    }

    private Gen24AcSystemSettings acSystemSettings = new();

    public Gen24AcSystemSettings AcSystemSettings
    {
        get => acSystemSettings;
        set => Set(ref acSystemSettings, value);
    }

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
