namespace De.Hochstaetter.Fronius.Models.Gen24.Settings;

public partial class Gen24PowerLimitSettings : BindableBase, ICloneable
{
    [ObservableProperty]
    public partial Gen24PowerLimits ExportLimits { get; set; } = new();

    [ObservableProperty]
    public partial Gen24PowerLimitsVisualization Visualization { get; set; } = new();

    public static Gen24PowerLimitSettings ParseFromConfig(JsonNode? configToken)
    {
        var token = configToken?["powerLimits"];
        return Parse(token);
    }

    public static Gen24PowerLimitSettings Parse(JsonNode? token)
    {
        var gen24PowerLimitSettings = new Gen24PowerLimitSettings
        {
            ExportLimits = Gen24PowerLimits.Parse(token?["exportLimits"]),
            Visualization = Gen24PowerLimitsVisualization.Parse(token?["visualization"]),
        };

        return gen24PowerLimitSettings;
    }

    public object Clone()
    {
        return new Gen24PowerLimitSettings
        {
            ExportLimits = (Gen24PowerLimits)ExportLimits.Clone(),
            Visualization = (Gen24PowerLimitsVisualization)Visualization.Clone(),
        };
    }

    /// <summary>
    /// What has to go to <c>api/config/limit_settings/powerLimits</c> to turn <paramref name="oldSettings"/> into
    /// this, and an empty object where the two say the same thing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Four objects nested three deep, each with a delta of its own, and an object is left out where nothing in
    /// it changed - which is what keeps an unchanged settings page from writing anything at all. A tree of empty
    /// objects would not: it counts as having values, and the inverter would be asked to change nothing.
    /// </para>
    /// <para>
    /// The one thing here that is not a delta is <c>displayModeSoftLimit</c>. It says whether the soft limit is
    /// meant as watts or as a percentage of the peak power, and it travels with the soft limit rather than being
    /// read back and compared - a number the inverter reads in the other unit is not a small mistake. So it is
    /// sent whenever the soft limit is, and never otherwise.
    /// </para>
    /// </remarks>
    public JsonNode GetToken(Gen24PowerLimitSettings? oldSettings = null)
    {
        var json = IoC.Get<IGen24JsonService>();

        var hardLimit = json.GetUpdateToken(ExportLimits.ActivePower.HardLimit, oldSettings?.ExportLimits.ActivePower.HardLimit);
        var softLimit = json.GetUpdateToken(ExportLimits.ActivePower.SoftLimit, oldSettings?.ExportLimits.ActivePower.SoftLimit);
        var activePower = json.GetUpdateToken(ExportLimits.ActivePower, oldSettings?.ExportLimits.ActivePower);
        var exportLimits = json.GetUpdateToken(ExportLimits, oldSettings?.ExportLimits);
        var visualization = json.GetUpdateToken(Visualization, oldSettings?.Visualization);

        if (hardLimit.HasAnyValue())
        {
            activePower.Add("hardLimit", hardLimit);
        }

        if (softLimit.HasAnyValue())
        {
            activePower.Add("softLimit", softLimit);
            visualization.Add("exportLimits", new JsonObject { { "activePower", new JsonObject { { "displayModeSoftLimit", "absolute" } } } });
        }

        if (activePower.HasAnyValue())
        {
            exportLimits.Add("activePower", activePower);
        }

        var token = new JsonObject();

        if (visualization.HasAnyValue())
        {
            token.Add("visualization", visualization);
        }

        if (exportLimits.HasAnyValue())
        {
            token.Add("exportLimits", exportLimits);
        }

        return token;
    }
}