using De.Hochstaetter.HomeAutomationClient.Models;

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests;

/// <summary>
/// The colouring rule of the Type 2 plug on the Wattpilot card, as the WPF drawing had it: what a phase shows for
/// each combination the charger reports, and what N and PE make of the three.
/// </summary>
public sealed class WattPilotPhasesTests
{
    [Theory]
    [InlineData(null, true, 10.0, PhaseState.Unknown)]
    [InlineData(true, null, 10.0, PhaseState.Unknown)]
    [InlineData(true, true, 10.0, PhaseState.Charging)]
    [InlineData(true, true, 1.0, PhaseState.Ready)]    // exactly one ampere is not yet charging
    [InlineData(true, true, null, PhaseState.Ready)]
    [InlineData(false, true, 0.0, PhaseState.ChargerOnly)]
    [InlineData(true, false, 0.0, PhaseState.CableOnly)]
    [InlineData(false, false, 0.0, PhaseState.CableOnly)]
    public void A_phase_shows_what_cable_charger_and_current_say(bool? cable, bool? charger, double? current, PhaseState expected)
    {
        Assert.Equal(expected, WattPilotPhases.Of(cable, charger, current));
    }

    [Theory]
    [InlineData(PhaseState.Charging, PhaseState.Unknown, PhaseState.Unknown, PhaseState.Charging)]
    [InlineData(PhaseState.Ready, PhaseState.CableOnly, PhaseState.Unknown, PhaseState.Charging)]     // the return path is live as soon as one phase is ready
    [InlineData(PhaseState.CableOnly, PhaseState.ChargerOnly, PhaseState.Unknown, PhaseState.CableOnly)]
    [InlineData(PhaseState.ChargerOnly, PhaseState.Unknown, PhaseState.Unknown, PhaseState.ChargerOnly)]
    [InlineData(PhaseState.Unknown, PhaseState.Unknown, PhaseState.Unknown, PhaseState.Unknown)]
    public void Neutral_and_earth_take_the_best_of_the_three_phases(PhaseState l1, PhaseState l2, PhaseState l3, PhaseState expected)
    {
        Assert.Equal(expected, WattPilotPhases.Neutral(l1, l2, l3));
    }
}
