using De.Hochstaetter.Fronius.Models.Charging;
using De.Hochstaetter.Fronius.Models.Gen24;
using De.Hochstaetter.HomeAutomationClient.Models;
using De.Hochstaetter.HomeAutomationClient.ViewModels;
using De.Hochstaetter.HomeAutomationServerTests.UnitTests.Fakes;

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests;

/// <summary>
/// How the house block follows the update service. The arithmetic is <c>HousePowerTests</c>; this is about the
/// view model hearing what it has to hear - and going on hearing it after the service has replaced a collection,
/// which it does at every logout and whenever an inverter appears. The view model is a singleton that outlives
/// all of that.
/// </summary>
public sealed class HouseViewModelTests
{
    private readonly FakeUpdateService service = new();
    private readonly FakePowerDisplayOptions options = new();
    private readonly HouseViewModel house;

    public HouseViewModelTests() => house = new HouseViewModel(service, options);

    /// <summary>
    /// The Solar Web switch is not a device and says nothing through the update service, so the block has to hear
    /// it from the options themselves and work the figures out again from the readings it already has.
    /// </summary>
    [Fact]
    public void Throwing_the_solar_web_switch_works_the_figures_out_again()
    {
        service.Inverters.Add(new KeyedGen24System { Key = "inv", Device = new Gen24System() });
        service.SitePowerFlow.LoadPower = -3000;
        service.SitePowerFlow.SolarPower = 6000;
        service.SitePowerFlow.InverterAcPower = 5700;

        Assert.Equal(3000, house.Power.HouseConsumption);

        options.IncludeInverterPower = true;
        Assert.Equal(3300, house.Power.HouseConsumption);
        Assert.Equal(300, house.Power.PowerLoss);

        options.IncludeInverterPower = false;
        Assert.Equal(3000, house.Power.HouseConsumption);
    }

    [Fact]
    public void The_cars_are_followed_in_the_collection_the_service_answers_now()
    {
        // As at logout: the service forgets its consumers and starts over with a collection of its own.
        service.AllPowerConsumers = [];
        var pilot = new WattPilot { PowerTotal = 3000 };
        service.AllPowerConsumers.Add(new KeyedWattPilot { Key = "wp", Device = pilot });

        Assert.Equal(3000, house.Power.CarPower);
        Assert.True(house.HasCars);

        // And the Wattpilot in it is heard when it reports.
        pilot.PowerTotal = 1000;

        Assert.Equal(1000, house.Power.CarPower);
    }

    [Fact]
    public void A_wattpilot_of_the_session_before_is_no_longer_heard()
    {
        var old = new WattPilot { PowerTotal = 3000 };
        service.AllPowerConsumers.Add(new KeyedWattPilot { Key = "old", Device = old });
        Assert.Equal(3000, house.Power.CarPower);

        service.AllPowerConsumers = [];

        Assert.Null(house.Power.CarPower);
        Assert.False(house.HasCars);

        old.PowerTotal = 5000;

        Assert.Null(house.Power.CarPower);
    }

    [Fact]
    public void The_figures_come_once_the_service_has_an_inverter_in_a_new_collection()
    {
        service.SitePowerFlow.LoadPower = -500;
        service.SitePowerFlow.InverterAcPower = 500;
        Assert.Null(house.Power.HouseConsumption);

        // As when an inverter appears: the service answers a new, sorted collection rather than adding to the old one.
        service.Inverters = [new KeyedGen24System { Key = "inverter", Device = new Gen24System() }];

        Assert.Equal(500, house.Power.HouseConsumption);
    }

    [Fact]
    public void The_gauges_scale_to_the_peak_power_the_service_reports()
    {
        service.SitePvPeakPower = 8000;

        Assert.Equal(8000, house.PowerMaximum);
        Assert.Equal(8000 * 0.7, house.SolarMaximum);
    }

    [Fact]
    public void The_flow_the_service_replaces_at_logout_is_followed()
    {
        service.Inverters.Add(new KeyedGen24System { Key = "inverter", Device = new Gen24System() });
        service.SitePowerFlow = new Gen24PowerFlow();

        service.SitePowerFlow.LoadPower = -700;
        service.SitePowerFlow.Refresh(true);

        Assert.Equal(700, house.Power.HouseConsumption);
    }
}
