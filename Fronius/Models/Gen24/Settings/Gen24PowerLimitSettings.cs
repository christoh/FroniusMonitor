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
    /// Settings for the same inverter with every limit off and everything else at its default, which is what the
    /// WPF app writes when export limiting is switched off.
    /// </summary>
    /// <remarks>
    /// What the inverter was read to keep its limits in stays, see <see cref="Gen24PowerLimit.HasExportLimit"/>:
    /// these become what the inverter holds once they are written, and a later write is built against them.
    /// </remarks>
    public Gen24PowerLimitSettings WithLimitingOff() => new()
    {
        ExportLimits =
        {
            ActivePower =
            {
                HasExportLimit = ExportLimits.ActivePower.HasExportLimit,
                IsProtectionLimitEnabled = ExportLimits.ActivePower.IsProtectionLimitEnabled,
            },
        },
    };

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
    /// The one thing here that is not a delta is the display mode of the soft limit. It says whether the soft
    /// limit is meant as watts or as a percentage of the peak power, and it travels with the soft limit rather than
    /// being read back and compared - a number the inverter reads in the other unit is not a small mistake. So it
    /// is sent whenever the soft limit is, and never otherwise.
    /// </para>
    /// <para>
    /// Where the soft limit goes depends on the firmware, see <see cref="Gen24PowerLimit.HasExportLimit"/>, and
    /// what <paramref name="oldSettings"/> were read from decides it, because that is the inverter being written.
    /// An older one takes the watts in <c>softLimit.powerLimit</c> and the display mode as
    /// <c>displayModeSoftLimit</c>; 1.42.9-1 takes them in <c>activePower.exportLimit</c> and as
    /// <c>displayModeExportLimit</c>, and would not know the old names.
    /// </para>
    /// <para>
    /// On 1.42.9-1 the PAV,E protection limit uses the same number. A soft limit that is switched off arrives here
    /// as zero watts, and while the protection limit is on, that zero would become its limit - so the watts are
    /// then left as they are, the way the inverter's own settings page leaves them.
    /// </para>
    /// </remarks>
    public JsonNode GetToken(Gen24PowerLimitSettings? oldSettings = null)
    {
        var json = IoC.Get<IGen24JsonService>();
        var inverter = (oldSettings ?? this).ExportLimits.ActivePower;

        var hardLimit = json.GetUpdateToken(ExportLimits.ActivePower.HardLimit, oldSettings?.ExportLimits.ActivePower.HardLimit);
        var softLimit = json.GetUpdateToken(ExportLimits.ActivePower.SoftLimit, oldSettings?.ExportLimits.ActivePower.SoftLimit);
        var activePower = json.GetUpdateToken(ExportLimits.ActivePower, oldSettings?.ExportLimits.ActivePower);
        var exportLimits = json.GetUpdateToken(ExportLimits, oldSettings?.ExportLimits);
        var visualization = json.GetUpdateToken(Visualization, oldSettings?.Visualization);

        if (hardLimit.HasAnyValue())
        {
            activePower.Add("hardLimit", hardLimit);
        }

        if (inverter.HasExportLimit && softLimit.TryGetPropertyValue("powerLimit", out var watts))
        {
            softLimit.Remove("powerLimit");

            if (ExportLimits.ActivePower.SoftLimit.IsEnabled || !inverter.IsProtectionLimitEnabled)
            {
                activePower.Add("exportLimit", watts);
            }
        }

        if (softLimit.HasAnyValue())
        {
            activePower.Add("softLimit", softLimit);
        }

        if (softLimit.HasAnyValue() || activePower.ContainsKey("exportLimit"))
        {
            var displayMode = inverter.HasExportLimit ? "displayModeExportLimit" : "displayModeSoftLimit";
            visualization.Add("exportLimits", new JsonObject { { "activePower", new JsonObject { { displayMode, "absolute" } } } });
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