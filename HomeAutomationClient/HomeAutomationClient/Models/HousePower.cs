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
/// surplus in one direction is not a percentage over a hundred in the other. Neither is ever null once there is an
/// inverter: a house that consumes nothing needs nothing from the grid and is fully self-sufficient, and where
/// nothing is produced there is no production that could stay in the house, so own consumption is nought.
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
            // May be negative, and is shown as it is. The load is what is left of the grid and the inverters once
            // everything this software knows about is accounted for, so it goes the other way whenever something
            // feeds the house that this software cannot see - an old diesel generator with no data interface, a
            // second inverter that reports to nobody - and briefly when a Wattpilot reading is newer than the
            // inverter's, so the cars appear to draw more than the whole load. Clamping at zero would hide all
            // three, and the first of them is worth seeing: it says the house is being fed from somewhere else.
            HouseConsumption: consumption - (carPower ?? 0),
            CarPower: carPower,
            SolarPower: flow.SolarPower,
            PowerLoss: flow.PowerLoss,
            SelfSufficiency: consumption > 0 ? Math.Clamp(production / consumption, 0, 1) * 100 : 100,
            SelfConsumption: production > 0 ? Math.Clamp(consumption / production, 0, 1) * 100 : 0
        );
    }

    /// <summary>The sum of what the Wattpilots draw, or <see langword="null"/> where there is none to sum.</summary>
    public static double? CarPowerOf(IReadOnlyCollection<WattPilot> wattPilots) => wattPilots.Count == 0 ? null : wattPilots.Sum(w => w.PowerTotal ?? 0);
}
