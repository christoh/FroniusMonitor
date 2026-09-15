namespace De.Hochstaetter.Fronius.Models;

/// <summary>
/// One hour of Awattar's <c>/v1/power/productions</c>. Nullable on purpose: the feed answers every hour of the span it
/// is asked for, and the hours beyond its forecast - one day ahead, verified 2026-09-15 - carry <c>null</c> for both
/// values. A plain double made the whole answer unreadable, good hours included.
/// </summary>
public partial class AwattarEnergy : AwattarBase
{
    [ObservableProperty]
    [JsonPropertyName("solar")]
    public partial double? SolarProductionMegaWatt { get; set; }

    [ObservableProperty]
    [JsonPropertyName("wind")]
    public partial double? WindProductionMegaWatt { get; set; }

    /// <summary>
    /// Whether this hour is within the forecast. An hour outside it is not zero production and is left out of
    /// charts and history rather than drawn as an empty bar. Both values go null together in practice; either
    /// alone missing is treated the same way.
    /// </summary>
    [JsonIgnore]
    public bool HasValues => SolarProductionMegaWatt != null && WindProductionMegaWatt != null;
}
