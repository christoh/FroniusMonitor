namespace De.Hochstaetter.Fronius.Contracts;

/// <summary>
/// A consumer that draws on all three phases - a wallbox, a heat pump. It is an <see cref="IPowerConsumer1P"/> as
/// well, whose <see cref="IPowerMeter1P.ActivePower"/> is the total over the phases: everything that lists consumers
/// by what they draw sees it there without knowing about phases, and only what wants to show the phases asks here.
/// </summary>
/// <remarks>
/// <para>
/// The names follow <see cref="IPowerMeter3P"/>, the smart meter's contract, so the two read alike. It is not that
/// contract, though: a smart meter counts energy per phase in both directions and a consumer does not, and a
/// wallbox would have had to answer <see langword="null"/> to forty members.
/// </para>
/// <para>
/// What the single phase members mean for a three phase device is the implementer's to say and to document:
/// the Wattpilot answers the average phase voltage for <see cref="IPowerMeter1P.Voltage"/> and the sum of the phase
/// currents for <see cref="IPowerMeter1P.Current"/>.
/// </para>
/// </remarks>
public interface IPowerConsumer3P : IPowerConsumer1P
{
    double? ActivePowerL1 { get; }
    double? ActivePowerL2 { get; }
    double? ActivePowerL3 { get; }
    double? CurrentL1 { get; }
    double? CurrentL2 { get; }
    double? CurrentL3 { get; }
    double? PhaseVoltageL1 { get; }
    double? PhaseVoltageL2 { get; }
    double? PhaseVoltageL3 { get; }
}
