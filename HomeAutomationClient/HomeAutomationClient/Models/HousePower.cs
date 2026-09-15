using De.Hochstaetter.Fronius.Models.Charging;

namespace De.Hochstaetter.HomeAutomationClient.Models;

/// <summary>
/// The figures of the house block on the dashboard, worked out from the site's power flow and the cars. Watts for
/// the powers, percent for the two ratios; <see langword="null"/> where a figure has no meaning at the moment.
/// </summary>
/// <remarks>
/// <para>
/// Sign convention of the Gen24: <see cref="Gen24PowerFlow.LoadPower"/> is negative while the house consumes,
/// <see cref="Gen24PowerFlow.InverterAcPower"/> is positive while the inverters deliver AC. The load includes the
/// cars, so the house alone is the load minus what the Wattpilots draw.
/// </para>
/// <para>
/// The ratios are the ones the WPF <c>InverterControl</c> shows on its efficiency tab: self-sufficiency is the
/// share of the consumption the inverters cover (battery included, since that comes out of the inverter as AC),
/// own consumption is the share of the AC production that stays in the house. Both are cut to 0..100 %, because a
/// surplus in one direction is not a percentage over a hundred in the other.
/// </para>
/// </remarks>
public sealed record HousePower(double? HouseConsumption, double? CarPower, double? SolarPower, double? PowerLoss, double? SelfSufficiency, double? SelfConsumption)
{
    public static HousePower None { get; } = new(null, null, null, null, null, null);

    /// <param name="flow">The sum over all inverters, or <see langword="null"/> while there is no inverter, in which case only the cars are known.</param>
    /// <param name="carPower">What all Wattpilots draw together, or <see langword="null"/> where there is no Wattpilot.</param>
    public static HousePower From(Gen24PowerFlow? flow, double? carPower)
    {
        if (flow == null)
        {
            return None with { CarPower = carPower };
        }

        var consumption = -flow.LoadPowerCorrected;
        var production = flow.InverterAcPower;

        return new HousePower
        (
            // Not below zero: a Wattpilot reading can be a moment newer than the inverter's, and then the cars
            // briefly draw more than the whole load.
            HouseConsumption: Math.Max(0, consumption - (carPower ?? 0)),
            CarPower: carPower,
            SolarPower: flow.SolarPower,
            PowerLoss: flow.PowerLoss,
            SelfSufficiency: consumption > 0 ? Math.Clamp(production / consumption, 0, 1) * 100 : null,
            SelfConsumption: production > 0 ? Math.Clamp(consumption / production, 0, 1) * 100 : null
        );
    }

    /// <summary>The sum of what the Wattpilots draw, or <see langword="null"/> where there is none to sum.</summary>
    public static double? CarPowerOf(IReadOnlyCollection<WattPilot> wattPilots) => wattPilots.Count == 0 ? null : wattPilots.Sum(w => w.PowerTotal ?? 0);
}
