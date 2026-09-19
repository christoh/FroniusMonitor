using De.Hochstaetter.Fronius.Models.Charging;
using De.Hochstaetter.Fronius.Models.Gen24;
using De.Hochstaetter.HomeAutomationClient.Models;

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests;

/// <summary>
/// The figures of the dashboard's house block. The Gen24 reports the load as a negative number while the house
/// draws, and the cars are part of it; these tests pin the signs and the two ratios.
/// </summary>
/// <remarks>
/// The house consumption may be **negative**, and that is shown rather than clamped away: a power source this
/// software knows nothing about feeds the house without being accounted for anywhere, and the cars can be read a
/// moment later than the inverter. The two ratios always have a value once there is an inverter: a house that
/// consumes nothing needs nothing from the grid, so it is fully self-sufficient, and where nothing is produced
/// none of the production stays in the house, so own consumption is nought. Only the no-inverter case is null.
/// </remarks>
public sealed class HousePowerTests
{
    private static Gen24PowerFlow Flow(double load, double inverterAc, double solar = 0, double storage = 0) => new()
    {
        LoadPower = load,
        InverterAcPower = inverterAc,
        SolarPower = solar,
        StoragePower = storage,
    };

    [Fact]
    public void The_house_is_the_load_without_the_cars()
    {
        var power = HousePower.From(Flow(load: -3000, inverterAc: 0), carPower: 1200);

        Assert.Equal(1800, power.HouseConsumption);
        Assert.Equal(1200, power.CarPower);
    }

    /// <summary>
    /// "Solar Web" mode: what the inverter loses between the panels and its AC side counts as consumption of the
    /// house, the way Fronius' own portal reports it.
    /// </summary>
    [Fact]
    public void Solar_web_mode_adds_the_inverter_loss_to_the_house()
    {
        // 6000 W off the roof, 5700 W out of the inverter: 300 W lost on the way.
        var flow = Flow(load: -3000, inverterAc: 5700, solar: 6000);
        Assert.Equal(300, flow.PowerLoss);

        var plain = HousePower.From(flow, carPower: 1200);
        var solarWeb = HousePower.From(flow, carPower: 1200, includeInverterPower: true);

        Assert.Equal(1800, plain.HouseConsumption);
        Assert.Equal(2100, solarWeb.HouseConsumption);

        // The cars are what they draw either way, and the loss is still shown as the loss it is.
        Assert.Equal(1200, solarWeb.CarPower);
        Assert.Equal(300, solarWeb.PowerLoss);

        // The two ratios stay on the consumption the house really has, so that they do not move when the switch
        // is thrown: the loss is the price of producing, not something the house asked for.
        Assert.Equal(plain.SelfSufficiency, solarWeb.SelfSufficiency);
        Assert.Equal(plain.SelfConsumption, solarWeb.SelfConsumption);
    }

    [Fact]
    public void The_house_goes_negative_when_the_cars_draw_more_than_the_load()
    {
        // Not clamped at zero: a Wattpilot reading can be a moment newer than the inverter's, and what the house
        // shows then is that the cars are drawing more than the load the inverter last reported.
        var power = HousePower.From(Flow(load: -1000, inverterAc: 0), carPower: 1500);

        Assert.Equal(-500, power.HouseConsumption);
    }

    [Fact]
    public void A_source_this_software_cannot_see_shows_as_a_negative_house()
    {
        // A diesel generator with no data interface, or a second inverter that reports to nobody: what it feeds
        // the house with is not accounted for anywhere, so it turns up as a positive load power.
        var power = HousePower.From(Flow(load: 500, inverterAc: 2000), carPower: null);

        Assert.Equal(-500, power.HouseConsumption);
        Assert.Equal(100, power.SelfSufficiency);
        Assert.Equal(0, power.SelfConsumption);
    }

    [Fact]
    public void Without_a_wattpilot_the_cars_are_unknown_and_the_house_is_the_whole_load()
    {
        var power = HousePower.From(Flow(load: -3000, inverterAc: 0), carPower: null);

        Assert.Null(power.CarPower);
        Assert.Equal(3000, power.HouseConsumption);
    }

    [Fact]
    public void Importing_from_the_grid_lowers_the_self_sufficiency_and_keeps_the_own_consumption_full()
    {
        // 3 kW consumed, the inverters deliver 1 kW, the other 2 kW come from the grid.
        var power = HousePower.From(Flow(load: -3000, inverterAc: 1000), carPower: null);

        Assert.NotNull(power.SelfSufficiency);
        Assert.Equal(33.3, power.SelfSufficiency.Value, 1);
        Assert.Equal(100, power.SelfConsumption);
    }

    [Fact]
    public void Exporting_keeps_the_self_sufficiency_full_and_lowers_the_own_consumption()
    {
        // 1 kW consumed, 4 kW delivered, 3 kW leave the house.
        var power = HousePower.From(Flow(load: -1000, inverterAc: 4000), carPower: null);

        Assert.Equal(100, power.SelfSufficiency);
        Assert.Equal(25, power.SelfConsumption);
    }

    [Fact]
    public void At_night_nothing_is_produced_so_none_of_the_production_stays_in_the_house()
    {
        var power = HousePower.From(Flow(load: -500, inverterAc: 0), carPower: null);

        Assert.Equal(0, power.SelfSufficiency);
        Assert.Equal(0, power.SelfConsumption);
    }

    [Fact]
    public void A_house_that_consumes_nothing_is_fully_self_sufficient()
    {
        var power = HousePower.From(Flow(load: 0, inverterAc: 2000), carPower: null);

        Assert.Equal(100, power.SelfSufficiency);
        Assert.Equal(0, power.HouseConsumption);
    }

    [Fact]
    public void The_loss_is_what_goes_in_as_dc_and_does_not_come_out_as_ac()
    {
        // 5 kW from the panels, 1 kW of it into the battery (a negative storage power is charging), 3.8 kW out as AC.
        var power = HousePower.From(Flow(load: -3800, inverterAc: 3800, solar: 5000, storage: -1000), carPower: null);

        Assert.NotNull(power.PowerLoss);
        Assert.Equal(200, power.PowerLoss.Value, 6);
        Assert.Equal(5000, power.SolarPower);
    }

    [Fact]
    public void Without_an_inverter_only_the_cars_are_known()
    {
        var power = HousePower.From(null, carPower: 7000);

        Assert.Equal(7000, power.CarPower);
        Assert.Null(power.HouseConsumption);
        Assert.Null(power.PowerLoss);
        Assert.Null(power.SolarPower);
        Assert.Null(power.SelfSufficiency);
        Assert.Null(power.SelfConsumption);
    }

    [Fact]
    public void The_cars_add_up_and_a_wattpilot_without_a_reading_counts_as_zero()
    {
        Assert.Null(HousePower.CarPowerOf([]));
        Assert.Equal(4200, HousePower.CarPowerOf([new WattPilot { PowerTotal = 4200 }, new WattPilot { PowerTotal = null }]));
    }
}
