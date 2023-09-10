namespace De.Hochstaetter.Fronius.Models.Gen24.Settings;

[SuppressMessage("ReSharper", "StringLiteralTypo")]
public class Gen24InverterSettings : BindableBase, ICloneable
{
    private static readonly IGen24JsonService gen24JsonService = IoC.TryGet<IGen24JsonService>()!;

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

    private Gen24PowerLimitSettings? exportLimit;

    public Gen24PowerLimitSettings? ExportLimit
    {
        get => exportLimit;
        set => Set(ref exportLimit, value);
    }

    public object Clone()
    {
        var clone = new Gen24InverterSettings
        {
            SystemName = SystemName,
            TimeZoneName = TimeZoneName,
            TimeSync = TimeSync,
            Mppt = Mppt?.Clone() as Gen24Mppt,
            ExportLimit = ExportLimit?.Clone() as Gen24PowerLimitSettings,
        };

        return clone;
    }

    public static Gen24InverterSettings Parse(JToken? uiToken, JToken? mpptToken, JToken? exportLimitToken)
    {
        var inverterSettings = gen24JsonService.ReadFroniusData<Gen24InverterSettings>(uiToken);
        inverterSettings.Mppt = Gen24Mppt.Parse(mpptToken);
        inverterSettings.ExportLimit = Gen24PowerLimitSettings.Parse(exportLimitToken);
        return inverterSettings;
    }

    public static Gen24InverterSettings Parse(JToken? configToken)
    {
        return Parse
        (
            configToken?["common"]?["ui"]?.Value<JToken>() ?? new JObject(),
            configToken?["setup"]?["powerunit"]?["mppt"]?.Value<JToken>() ?? new JObject(),
            configToken?["powerlimits"]?["powerLimits"]?["exportLimits"]?.Value<JToken>() ?? new JObject()
        );
    }

    public override string ToString() => SystemName ?? string.Empty;
}
