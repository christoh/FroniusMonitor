namespace De.Hochstaetter.Fronius.Models.Gen24.Settings;

[SuppressMessage("ReSharper", "StringLiteralTypo")]
public class Gen24PowerLimitSettings : BindableBase, ICloneable
{
    private static readonly IGen24JsonService gen24JsonService = IoC.TryGet<IGen24JsonService>()!;

    private bool? enableFailSafeMode;
    [FroniusProprietaryImport("failSafeModeEnabled", FroniusDataType.Root)]
    public bool? EnableFailSafeMode
    {
        get => enableFailSafeMode;
        set => Set(ref enableFailSafeMode, value);
    }

    private Gen24PowerLimit? limit;

    public Gen24PowerLimit? Limit
    {
        get => limit;
        set => Set(ref limit, value);
    }

    public static Gen24PowerLimitSettings ParseFromConfig(JToken? configToken)
    {
        var token = configToken?["powerlimits"]?["powerLimits"]?["exportLimits"]?.Value<JToken>();
        return Parse(token);
    }
    
    public static Gen24PowerLimitSettings Parse(JToken? token)
    {
        var gen24PowerLimitSettings = gen24JsonService.ReadFroniusData<Gen24PowerLimitSettings>(token?.Value<JToken>());
        gen24PowerLimitSettings.Limit = Gen24PowerLimit.Parse(token?["activePower"]?.Value<JToken>());
        return gen24PowerLimitSettings;
    }

    public object Clone()
    {
        var gen24PowerLimitSettings = new Gen24PowerLimitSettings
        {
            EnableFailSafeMode = EnableFailSafeMode,
            Limit = Limit?.Clone() as Gen24PowerLimit,
        };
        
        return gen24PowerLimitSettings;
    }
}