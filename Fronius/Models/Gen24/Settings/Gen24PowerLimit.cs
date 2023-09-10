namespace De.Hochstaetter.Fronius.Models.Gen24.Settings;

public enum PowerLimitMode : byte
{
    [EnumParse(ParseAs = "off")] Off,
    [EnumParse(ParseAs = "entireSystem")] EntireSystem,
    [EnumParse(ParseAs = "weakestPhase")] WeakestPhase,
}

public class Gen24PowerLimit : BindableBase, ICloneable
{
    private static readonly IGen24JsonService gen24JsonService = IoC.TryGet<IGen24JsonService>()!;

    private Gen24PowerLimitDefinition? softLimit;

    public Gen24PowerLimitDefinition? SoftLimit
    {
        get => softLimit;
        set => Set(ref softLimit, value);
    }

    private Gen24PowerLimitDefinition? hardLimit;

    public Gen24PowerLimitDefinition? HardLimit
    {
        get => hardLimit;
        set => Set(ref hardLimit, value);
    }

    private PowerLimitMode? powerLimitMode;

    [FroniusProprietaryImport("mode", FroniusDataType.Root)]
    public PowerLimitMode? PowerLimitMode
    {
        get => powerLimitMode;
        set => Set(ref powerLimitMode, value);
    }

    public static Gen24PowerLimit Parse(JToken? token)
    {
        var gen24PowerLimit = gen24JsonService.ReadFroniusData<Gen24PowerLimit>(token);
        gen24PowerLimit.SoftLimit = Gen24PowerLimitDefinition.Parse(token?["softLimit"]);
        gen24PowerLimit.HardLimit = Gen24PowerLimitDefinition.Parse(token?["hardLimit"]);
        return gen24PowerLimit;
    }

    public object Clone()
    {
        var gen24PowerLimit = new Gen24PowerLimit
        {
            PowerLimitMode = PowerLimitMode,
            HardLimit = HardLimit?.Clone() as Gen24PowerLimitDefinition,
            SoftLimit = SoftLimit?.Clone() as Gen24PowerLimitDefinition,
        };

        return gen24PowerLimit;
    }
}
