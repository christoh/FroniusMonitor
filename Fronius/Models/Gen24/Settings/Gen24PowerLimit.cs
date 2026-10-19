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

    public Gen24PowerLimitDefinition SoftLimit
    {
        get;
        set => Set(ref field, value);
    } = new();

    public Gen24PowerLimitDefinition HardLimit
    {
        get;
        set => Set(ref field, value);
    } = new();

    [FroniusProprietaryImport("activated", FroniusDataType.Root)]
    public bool IsEnabled
    {
        get;
        set => Set(ref field, value, AdjustObsoleteProperties);
    }

    [FroniusProprietaryImport("networkMode", FroniusDataType.Root)]
    public NetworkMode NetWorkMode
    {
        get;
        set => Set(ref field, value, () => NotifyOfPropertyChange(nameof(IsNetworkModeEnabled)));
    }

    public bool IsNetworkModeEnabled
    {
        get => NetWorkMode == NetworkMode.Network;
        set => NetWorkMode = value ? NetworkMode.Network : NetworkMode.Local;
    }

    [FroniusProprietaryImport("phaseMode", FroniusDataType.Root)]
    public PhaseMode PhaseMode
    {
        get;
        set => Set(ref field, value, AdjustObsoleteProperties);
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

    [Obsolete("Use IsEnabled and PhaseMode instead to reflect firmware >= 1.30")]
    public PowerLimitMode PowerLimitMode
    {
        get;
        set => Set(ref field, value, () =>
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
    } = PowerLimitMode.Off;

    /// <summary>
    /// Whether the inverter keeps the watts of the soft limit in <c>activePower.exportLimit</c> rather than in
    /// <c>softLimit.powerLimit</c>.
    /// </summary>
    /// <remarks>
    /// Firmware 1.42.9-1 (config API 11) moved them there, because the PAV,E protection limit it brought uses the
    /// same number, and left only <c>enabled</c> in <c>softLimit</c>. <see cref="SoftLimit"/> carries the watts
    /// for both kinds of inverter, so only reading and writing have to know. Not one of the inverter's settings:
    /// it says how the inverter that was read stores them, and nothing ever sends it.
    /// </remarks>
    public bool HasExportLimit { get; set; }

    /// <summary>
    /// Whether the PAV,E protection limit of an inverter with <see cref="HasExportLimit"/> is on. It shares its
    /// number with the soft limit. Read only: neither app shows it or sends it.
    /// </summary>
    public bool IsProtectionLimitEnabled { get; set; }

    public static Gen24PowerLimit Parse(JsonNode? token)
    {
        var gen24PowerLimit = gen24JsonService.ReadFroniusData<Gen24PowerLimit>(token);
        gen24PowerLimit.SoftLimit = Gen24PowerLimitDefinition.Parse(token?["softLimit"]);
        gen24PowerLimit.HardLimit = Gen24PowerLimitDefinition.Parse(token?["hardLimit"]);

        if (token?["exportLimit"] is JsonValue exportLimit && exportLimit.TryGetValue<double>(out var watts))
        {
            gen24PowerLimit.HasExportLimit = true;
            gen24PowerLimit.SoftLimit.PowerLimit = watts;
            gen24PowerLimit.IsProtectionLimitEnabled = token["protectionLimit"]?["enabled"] is JsonValue enabled && enabled.TryGetValue<bool>(out var isEnabled) && isEnabled;
        }

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
            HasExportLimit = HasExportLimit,
            IsProtectionLimitEnabled = IsProtectionLimitEnabled,
        };

        return gen24PowerLimit;
    }
}
#pragma warning restore CS0618 // Type or member is obsolete
