namespace De.Hochstaetter.Fronius.Models.Gen24.Settings;

[SuppressMessage("ReSharper", "StringLiteralTypo")]
public class Gen24PowerLimits : Gen24ParsingBase
{
    [FroniusProprietaryImport("failSafeModeEnabled", FroniusDataType.Root)]
    public bool EnableFailSafeMode
    {
        get;
        set => Set(ref field, value);
    }

    public Gen24PowerLimit ActivePower
    {
        get;
        set => Set(ref field, value);
    } = new();

    //private Gen24PowerLimit? reactivePower;

    //public Gen24PowerLimit? ReactivePower
    //{
    //    get => reactivePower;
    //    set => Set(ref reactivePower, value);
    //}

    public static Gen24PowerLimits ParseFromConfig(JToken? configToken)
    {
        var token = configToken?["limit_settings"]?["powerLimits"]?["exportLimits"]?.Value<JToken>();
        return Parse(token);
    }

    public static Gen24PowerLimits Parse(JToken? token)
    {
        var gen24PowerLimitSettings = Gen24JsonService.ReadFroniusData<Gen24PowerLimits>(token?.Value<JToken>());
        gen24PowerLimitSettings.ActivePower = Gen24PowerLimit.Parse(token?["activePower"]?.Value<JToken>());
        return gen24PowerLimitSettings;
    }

    public override object Clone()
    {
        var gen24PowerLimitSettings = new Gen24PowerLimits
        {
            EnableFailSafeMode = EnableFailSafeMode,
            ActivePower = (Gen24PowerLimit)ActivePower.Clone(),
        };

        return gen24PowerLimitSettings;
    }
}
