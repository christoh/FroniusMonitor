namespace De.Hochstaetter.Fronius.Models.Gen24.Settings;
#pragma warning disable CS0618 // Type or member is obsolete

public enum PowerLimitMode : byte
{
    [EnumParse(ParseAs = "off")] Off,
    [EnumParse(ParseAs = "entireSystem")] EntireSystem,
    [EnumParse(ParseAs = "weakestPhase")] WeakestPhase,
}

public enum PhaseMode : byte
{
    [EnumParse(ParseAs = "weakestPhase")] WeakestPhase,
    [EnumParse(ParseAs = "phaseSum")] PhaseSum,
}

public enum NetworkMode : byte
{
    [EnumParse(ParseAs = "limitLocal")] Local,
    [EnumParse(ParseAs = "limitNetwork")] Network,
}

public class Gen24PowerLimit : BindableBase, ICloneable
{
    private static readonly IGen24JsonService gen24JsonService = IoC.TryGet<IGen24JsonService>()!;

    private Gen24PowerLimitDefinition softLimit = new();

    public Gen24PowerLimitDefinition SoftLimit
    {
        get => softLimit;
        set => Set(ref softLimit, value);
    }

    private Gen24PowerLimitDefinition hardLimit = new();

    public Gen24PowerLimitDefinition HardLimit
    {
        get => hardLimit;
        set => Set(ref hardLimit, value);
    }

    private bool isEnabled;
    [FroniusProprietaryImport("activated", FroniusDataType.Root)]
    public bool IsEnabled
    {
        get => isEnabled;
        set => Set(ref isEnabled, value, AdjustObsoleteProperties);
    }

    private NetworkMode netWorkMode;
    [FroniusProprietaryImport("networkMode", FroniusDataType.Root)]
    public NetworkMode NetWorkMode
    {
        get => netWorkMode;
        set => Set(ref netWorkMode, value, () => NotifyOfPropertyChange(nameof(IsNetworkModeEnabled)));
    }

    public bool IsNetworkModeEnabled
    {
        get => NetWorkMode == NetworkMode.Network;
        set => NetWorkMode = value ? NetworkMode.Network : NetworkMode.Local;
    }

    private PhaseMode phaseMode;
    [FroniusProprietaryImport("phaseMode", FroniusDataType.Root)]
    public PhaseMode PhaseMode
    {
        get => phaseMode;
        set => Set(ref phaseMode, value, AdjustObsoleteProperties);
    }

    private void AdjustObsoleteProperties()
    {
        PowerLimitMode = IsEnabled switch
        {
            false => PowerLimitMode.Off,
            true when PhaseMode == PhaseMode.WeakestPhase => PowerLimitMode.WeakestPhase,
            true when PhaseMode == PhaseMode.PhaseSum => PowerLimitMode.EntireSystem,
            _ => throw new NotSupportedException($"Unsupported combination of {nameof(IsEnabled)} and {nameof(PhaseMode)}")
        };

    }

    private PowerLimitMode powerLimitMode = PowerLimitMode.Off;

    [Obsolete("Use IsEnabled and PhaseMode instead to reflect firmware >= 1.30")]
    public PowerLimitMode PowerLimitMode
    {
        get => powerLimitMode;
        set => Set(ref powerLimitMode, value, () =>
        {
            switch (value)
            {
                case PowerLimitMode.EntireSystem:
                    IsEnabled = true;
                    PhaseMode = PhaseMode.PhaseSum;
                    break;

                case PowerLimitMode.WeakestPhase:
                    IsEnabled = true;
                    PhaseMode = PhaseMode.WeakestPhase;
                    break;
                case PowerLimitMode.Off:
                    IsEnabled = false;
                    break;
            }
        });
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
            PhaseMode = PhaseMode,
            IsEnabled = IsEnabled,
            NetWorkMode = NetWorkMode,
            HardLimit = (Gen24PowerLimitDefinition)HardLimit.Clone(),
            SoftLimit = (Gen24PowerLimitDefinition)SoftLimit.Clone(),
        };

        return gen24PowerLimit;
    }
}
#pragma warning restore CS0618 // Type or member is obsolete
