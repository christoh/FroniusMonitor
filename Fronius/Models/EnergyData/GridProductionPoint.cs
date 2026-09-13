namespace De.Hochstaetter.Fronius.Models.EnergyData;

/// <summary>
///     What the grid of a price zone produced or is forecast to produce from sun and wind in one slot, in megawatts.
///     From Awattar's <c>power/productions</c>; the Wattpilot has nothing like it.
/// </summary>
public sealed class GridProductionPoint
{
    /// <summary>Start of the slot, UTC.</summary>
    public DateTime StartTime { get; set; }

    /// <summary>End of the slot, UTC.</summary>
    public DateTime EndTime { get; set; }

    public double SolarMegaWatt { get; set; }

    public double WindMegaWatt { get; set; }

    public override string ToString() => FormattableString.Invariant($"{StartTime:yyyy-MM-dd HH:mm}Z solar {SolarMegaWatt:N0} MW, wind {WindMegaWatt:N0} MW");
}
