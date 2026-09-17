using De.Hochstaetter.Fronius.Models;
using De.Hochstaetter.Fronius.Models.Charging;
using De.Hochstaetter.Fronius.Models.Gen24;
using De.Hochstaetter.Fronius.Models.Gen24.Settings;
using De.Hochstaetter.HomeAutomationClient.Contracts;
using De.Hochstaetter.HomeAutomationClient.Models;

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests;

/// <summary>
/// The figures of the power flow page, from what the update service holds. The signs are the Gen24's, the house
/// is the dashboard's, a tracker is a card of its own, and a consumer takes part exactly when it measures its power.
/// </summary>
public sealed class PowerFlowSnapshotTests
{
    private static Gen24PowerFlow Site(double load = -5100, double inverterAc = 5550, double grid = -450, double solar = 6910, double storage = -1120) => new()
    {
        LoadPower = load,
        InverterAcPower = inverterAc,
        GridPower = grid,
        SolarPower = solar,
        StoragePower = storage,
    };

    private static KeyedGen24System Inverter(string key, double? mppt1, double? mppt2, double ac, double? storage = null, double? soc = null, string? name = null) => new()
    {
        Key = key,
        Device = new Gen24System
        {
            Config = name is null ? null : new Gen24Config { InverterSettings = new Gen24InverterSettings { SystemName = name } },
            Sensors = new Gen24Sensors
            {
                Inverter = mppt1 is null && mppt2 is null ? null : new Gen24Inverter { Solar1Power = mppt1, Solar2Power = mppt2 },
                PowerFlow = new Gen24PowerFlow { SolarPower = (mppt1 ?? 0) + (mppt2 ?? 0), InverterAcPower = ac, StoragePower = storage ?? 0 },
                Storage = storage is null ? null : new Gen24Storage { StateOfCharge = soc, Model = "BYD HVS" },
            },
        },
    };

    private static KeyedFritzBoxDevice Plug(string key, string name, double? watts) => new()
    {
        Key = key,
        Device = new FritzBoxDevice
        {
            DisplayName = name,
            Features = watts is null ? FritzBoxFeatures.None : FritzBoxFeatures.PowerMeter,
            PowerMeter = watts is null ? null : new FritzBoxPowerMeter { PowerWatts = watts },
        },
    };

    private static KeyedWattPilot Car(string key, string name, double watts) => new() { Key = key, Device = new WattPilot { DeviceName = name, PowerTotal = watts } };

    [Fact]
    public void Consumers_are_the_metered_devices_and_the_rest_of_the_house()
    {
        var snapshot = PowerFlowSnapshot.From([Inverter("inv", 3620, 3290, 5550)], Site(), [Plug("hp", "Heat pump", 620), Car("wp", "Zoe", 3700), Plug("fridge", "Fridge", 95)]);

        Assert.Equal(["hp", "wp", "fridge", PowerFlowSnapshot.RestOfHouseKey], snapshot.Consumers.Select(node => node.Key));
        Assert.Equal([PowerFlowNodeKind.Consumer, PowerFlowNodeKind.Car, PowerFlowNodeKind.Consumer, PowerFlowNodeKind.RestOfHouse], snapshot.Consumers.Select(node => node.Kind));
        Assert.Equal("Heat pump", snapshot.Consumers[0].Name);
        Assert.Equal("Zoe", snapshot.Consumers[1].Name);

        // The house is the whole load, cars included; what is left after the metered consumers is the rest.
        Assert.Equal(5100, snapshot.House.Power);
        Assert.Equal(5100 - 620 - 3700 - 95, snapshot.Consumers[^1].Power);
        Assert.Equal(100, snapshot.SelfSufficiency);
        Assert.Equal(5100d / 5550 * 100, snapshot.SelfConsumption!.Value, 6);
    }

    [Fact]
    public void A_tracker_is_named_in_the_inverters_words_when_the_caller_has_them()
    {
        var snapshot = PowerFlowSnapshot.From([Inverter("inv", 100, 200, 300)], Site(), [], number => $"Tracker {number}");

        Assert.Equal(["Tracker 1", "Tracker 2"], snapshot.Inverters.Single().Solar.Select(node => node.Name));
    }

    [Fact]
    public void A_device_that_cannot_measure_its_power_is_left_out()
    {
        var snapshot = PowerFlowSnapshot.From([Inverter("inv", 0, 0, 0)], Site(), [Plug("switch", "Just a switch", null), Plug("hp", "Heat pump", 620)]);

        Assert.Equal(["hp", PowerFlowSnapshot.RestOfHouseKey], snapshot.Consumers.Select(node => node.Key));
    }

    [Fact]
    public void The_rest_of_the_house_may_go_negative()
    {
        // A car reading a moment fresher than the inverter's, or a source this software cannot see: shown, not hidden.
        var snapshot = PowerFlowSnapshot.From([Inverter("inv", 0, 0, 0)], Site(load: -1000), [Car("wp", "Zoe", 1500)]);

        Assert.Equal(-500, snapshot.Consumers[^1].Power);
    }

    [Fact]
    public void An_inverter_carries_its_own_name_and_a_card_per_tracker()
    {
        var snapshot = PowerFlowSnapshot.From([Inverter("inv", 3620, 1010, 4200, name: "Roof south")], Site(), []);

        var cluster = snapshot.Inverters.Single();
        Assert.Equal("Roof south", cluster.Inverter.Name);
        Assert.Equal(PowerFlowNodeKind.Inverter, cluster.Inverter.Kind);
        Assert.Equal(4200, cluster.Inverter.Power);

        Assert.Equal(["inv/mppt1", "inv/mppt2"], cluster.Solar.Select(node => node.Key));
        Assert.Equal(["MPPT 1", "MPPT 2"], cluster.Solar.Select(node => node.Name));
        Assert.Equal([3620d, 1010d], cluster.Solar.Select(node => node.Power));
        Assert.All(cluster.Solar, node => Assert.Equal(PowerFlowKind.Solar, node.FlowKind));

        // The DC side in the order the cards are stacked: the trackers, and no battery here.
        Assert.Equal(cluster.Solar, cluster.DcSources);
        Assert.Null(cluster.Battery);
    }

    [Fact]
    public void A_tracker_the_inverter_does_not_report_has_no_card_and_one_that_reports_nought_has()
    {
        var snapshot = PowerFlowSnapshot.From([Inverter("inv", 2000, null, 1900), Inverter("other", 0, 0, 0)], Site(), []);

        Assert.Equal(["inv/mppt1"], snapshot.Inverters[0].Solar.Select(node => node.Key));
        Assert.Equal(["other/mppt1", "other/mppt2"], snapshot.Inverters[1].Solar.Select(node => node.Key));
        Assert.All(snapshot.Inverters[1].Solar, node => Assert.True(node.IsIdle));
    }

    [Fact]
    public void An_inverter_without_tracker_sensors_shows_its_panels_as_a_whole()
    {
        var keyed = Inverter("inv", null, null, 950);
        keyed.Device.Sensors!.PowerFlow!.SolarPower = 1010;

        var snapshot = PowerFlowSnapshot.From([keyed], Site(), []);

        var solar = Assert.Single(snapshot.Inverters.Single().Solar);
        Assert.Equal("inv/solar", solar.Key);
        Assert.Equal(string.Empty, solar.Name);
        Assert.Equal(1010, solar.Power);
    }

    [Fact]
    public void The_grid_and_the_battery_carry_the_gen24_signs()
    {
        var snapshot = PowerFlowSnapshot.From([Inverter("inv", 3620, 0, 2410, storage: -1120, soc: 0.68)], Site(), []);

        var grid = Assert.IsType<PowerFlowNode>(snapshot.Grid);
        Assert.Equal(-450, grid.Power);
        Assert.Equal(PowerFlowKind.Grid, grid.FlowKind);
        Assert.Equal(PowerFlowState.FeedIn, grid.State);
        Assert.True(grid.IsReversed);

        var cluster = snapshot.Inverters.Single();
        var battery = Assert.IsType<PowerFlowNode>(cluster.Battery);
        Assert.Equal(-1120, battery.Power);
        Assert.Equal(0.68, battery.StateOfCharge);
        Assert.Equal("BYD HVS", battery.Name);
        Assert.Equal(PowerFlowKind.Battery, battery.FlowKind);
        Assert.Equal(PowerFlowState.Charging, battery.State);
        Assert.True(battery.IsReversed);

        // The battery is the last of the DC cards.
        Assert.Equal(battery, cluster.DcSources.Last());
    }

    [Fact]
    public void An_importing_grid_and_a_discharging_battery_run_forwards()
    {
        var snapshot = PowerFlowSnapshot.From([Inverter("inv", 0, 0, 800, storage: 800, soc: 0.4)], Site(grid: 1200, storage: 800), []);

        Assert.Equal(PowerFlowKind.Grid, snapshot.Grid!.FlowKind);
        Assert.Equal(PowerFlowState.GridImport, snapshot.Grid.State);
        Assert.False(snapshot.Grid.IsReversed);
        Assert.Equal(PowerFlowState.Discharging, snapshot.Inverters.Single().Battery!.State);
        Assert.False(snapshot.Inverters.Single().Battery!.IsReversed);
    }

    [Fact]
    public void Without_a_site_there_is_no_grid_and_no_rest_of_the_house()
    {
        // Before the first inverter reports: the consumers are known, the house is not.
        var snapshot = PowerFlowSnapshot.From([], null, [Plug("hp", "Heat pump", 620)]);

        Assert.Null(snapshot.Grid);
        Assert.Null(snapshot.House.Power);
        Assert.Null(snapshot.SelfSufficiency);
        Assert.Equal(["hp"], snapshot.Consumers.Select(node => node.Key));
    }

    /// <summary>A producer is idle below 10 W - an inverter at night still reports a few watts of its own - either way round.</summary>
    [Theory]
    [InlineData(0, true, PowerFlowState.Standby)]
    [InlineData(9.9, true, PowerFlowState.Standby)]
    [InlineData(-9.9, true, PowerFlowState.Standby)]
    [InlineData(10, false, PowerFlowState.Discharging)]
    [InlineData(-10, false, PowerFlowState.Charging)]
    public void A_producer_is_idle_below_ten_watts_either_way(double watts, bool isIdle, PowerFlowState state)
    {
        var node = new PowerFlowNode("b", PowerFlowNodeKind.Battery, string.Empty, watts, 0.5);

        Assert.Equal(isIdle, node.IsIdle);
        Assert.Equal(state, node.State);
        Assert.Equal(isIdle ? PowerFlowKind.Idle : PowerFlowKind.Battery, node.FlowKind);
    }

    /// <summary>A consumer is idle below 0.2 W: a plug that draws half a watt is switched on and worth seeing.</summary>
    [Theory]
    [InlineData(PowerFlowNodeKind.Consumer, 0.19, true)]
    [InlineData(PowerFlowNodeKind.Consumer, 0.2, false)]
    [InlineData(PowerFlowNodeKind.Car, 0.5, false)]
    [InlineData(PowerFlowNodeKind.RestOfHouse, 5, false)]
    [InlineData(PowerFlowNodeKind.House, 5, true)]
    [InlineData(PowerFlowNodeKind.Solar, 9, true)]
    public void A_consumer_is_idle_below_a_fifth_of_a_watt(PowerFlowNodeKind kind, double watts, bool isIdle)
    {
        var node = new PowerFlowNode("n", kind, string.Empty, watts);

        Assert.Equal(isIdle, node.IsIdle);
    }

    [Fact]
    public void A_node_that_has_not_reported_is_idle_and_says_nothing()
    {
        var consumer = new PowerFlowNode("c", PowerFlowNodeKind.Consumer, "Plug", null);
        var grid = new PowerFlowNode("g", PowerFlowNodeKind.Grid, string.Empty, null);

        Assert.True(consumer.IsIdle);
        Assert.Equal(PowerFlowState.None, consumer.State);
        Assert.Equal(PowerFlowState.Idle, grid.State);
        Assert.False(grid.IsReversed);
    }
}
