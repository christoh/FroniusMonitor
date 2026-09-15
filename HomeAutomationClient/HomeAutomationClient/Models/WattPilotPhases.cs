namespace De.Hochstaetter.HomeAutomationClient.Models;

/// <summary>What one contact of the Type 2 plug is doing, as the charger reports it.</summary>
public enum PhaseState
{
    /// <summary>The charger has not said; drawn hollow.</summary>
    Unknown,

    /// <summary>Cable and charger both switch the phase and current flows: green.</summary>
    Charging,

    /// <summary>Cable and charger both switch the phase, but no current flows yet: salmon.</summary>
    Ready,

    /// <summary>The charger would switch the phase but the cable does not carry it: white.</summary>
    ChargerOnly,

    /// <summary>The cable carries the phase but the charger does not switch it: yellow.</summary>
    CableOnly,
}

/// <summary>
/// The colouring rule of the Type 2 plug, ported from the WPF <c>Typ2</c> drawing: what each of L1..L3 shows, and
/// what N and PE show as the sum of the three. Pure, so the converters stay one line and the rule is tested here.
/// </summary>
public static class WattPilotPhases
{
    /// <summary>A phase carries current once more than one ampere flows; below that it is only ready.</summary>
    public const double ChargingCurrentThreshold = 1;

    public static PhaseState Of(bool? cableEnabled, bool? chargerEnabled, double? current)
    {
        if (cableEnabled == null || chargerEnabled == null)
        {
            return PhaseState.Unknown;
        }

        if (cableEnabled.Value && chargerEnabled.Value)
        {
            return current > ChargingCurrentThreshold ? PhaseState.Charging : PhaseState.Ready;
        }

        return chargerEnabled.Value ? PhaseState.ChargerOnly : PhaseState.CableOnly;
    }

    /// <summary>
    /// N and PE take the "best" of the three phases: green as soon as one phase charges or is ready (the return
    /// path is live either way), otherwise yellow, white or hollow like the phases themselves.
    /// </summary>
    public static PhaseState Neutral(PhaseState l1, PhaseState l2, PhaseState l3)
    {
        PhaseState[] phases = [l1, l2, l3];

        if (phases.Contains(PhaseState.Charging) || phases.Contains(PhaseState.Ready))
        {
            return PhaseState.Charging;
        }

        if (phases.Contains(PhaseState.CableOnly))
        {
            return PhaseState.CableOnly;
        }

        return phases.Contains(PhaseState.ChargerOnly) ? PhaseState.ChargerOnly : PhaseState.Unknown;
    }
}
