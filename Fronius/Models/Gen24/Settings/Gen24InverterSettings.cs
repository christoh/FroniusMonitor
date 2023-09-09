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

    public object Clone()
    {
        var clone = new Gen24InverterSettings
        {
            SystemName = SystemName,
            TimeZoneName = TimeZoneName,
            TimeSync = TimeSync,
            Mppt = Mppt?.Clone() as Gen24Mppt,
        };

        return clone;
    }

    public static Gen24InverterSettings Parse(JToken? uiToken, JToken? mpptToken)
    {
        var inverterSettings = gen24JsonService.ReadFroniusData<Gen24InverterSettings>(uiToken);
        inverterSettings.Mppt=Gen24Mppt.Parse(mpptToken);
        return inverterSettings;
    }

    public override string ToString() => SystemName ?? string.Empty;
}
