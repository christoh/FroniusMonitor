using De.Hochstaetter.Fronius.Models;
using De.Hochstaetter.Fronius.Models.Charging;
using De.Hochstaetter.Fronius.Models.Gen24;
using De.Hochstaetter.HomeAutomationClient.Contracts;
using De.Hochstaetter.HomeAutomationClient.Models;

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests;

/// <summary>
/// The figures of the power flow page, from what the update service holds. The signs are the Gen24's, the house
/// is the dashboard's, and a consumer takes part exactly when it measures its own power.
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

    private static KeyedGen24System Inverter(string key, double solar, double ac, double? storage = null, double? soc = null) => new()
    {
        Key = key,
        Device = new Gen24System
        {
            Sensors = new Gen24Sensors
            {
                PowerFlow = new Gen24PowerFlow { SolarPower = solar, InverterAcPower = ac, StoragePower = storage ?? 0 },
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
        var snapshot = PowerFlowSnapshot.From([Inverter("inv", 6910, 5550)], Site(), [Plug("hp", "Heat pump", 620), Car("wp", "Zoe", 3700), Plug("fridge", "Fridge", 95)]);

        Assert.Equal(["hp", "wp", "fridge", PowerFlowSnapshot.RestOfHouseKey], snapshot.Consumers.Select(node => node.Key));
        Assert.Equal([PowerFlowNodeKind.Consumer, PowerFlowNodeKind.Car, PowerFlowNodeKind.Consumer, PowerFlowNodeKind.RestOfHouse], snapshot.Consumers.Select(node => node.Kind));
        Assert.Equal("Heat pump", snapshot.Consumers[0].Name);
        Assert.Equal("Zoe", snapshot.Consumers[1].Name);

        // The house is the whole load, cars included; what is left after the metered consumers is the rest.
        Assert.Equal(5100, snapshot.House.Power);
        Assert.Equal(5100 - 620 - 3700 - 95, snapshot.Consumers[^1].Power);
        Assert.Equal(100, snapshot.SelfSufficiency);
    }

    [Fact]
    public void A_device_that_cannot_measure_its_power_is_left_out()
    {
        var snapshot = PowerFlowSnapshot.From([Inverter("inv", 0, 0)], Site(), [Plug("switch", "Just a switch", null), Plug("hp", "Heat pump", 620)]);

        Assert.Equal(["hp", PowerFlowSnapshot.RestOfHouseKey], snapshot.Consumers.Select(node => node.Key));
    }

    [Fact]
    public void The_rest_of_the_house_may_go_negative()
    {
        // A car reading a moment fresher than the inverter's, or a source this software cannot see: shown, not hidden.
        var snapshot = PowerFlowSnapshot.From([Inverter("inv", 0, 0)], Site(load: -1000), [Car("wp", "Zoe", 1500)]);

        Assert.Equal(-500, snapshot.Consumers[^1].Power);
    }

    [Fact]
    public void The_grid_and_the_battery_carry_the_gen24_signs()
    {
        var snapshot = PowerFlowSnapshot.From([Inverter("inv", 3620, 2410, storage: -1120, soc: 0.68)], Site(), []);

        var grid = Assert.IsType<PowerFlowNode>(snapshot.Grid);
        Assert.Equal(-450, grid.Power);
        Assert.Equal(PowerFlowKind.Export, grid.FlowKind);
        Assert.Equal(PowerFlowState.FeedIn, grid.State);
        Assert.True(grid.IsReversed);

        var cluster = snapshot.Inverters.Single();
        Assert.Equal("inv", cluster.Inverter.Key);
        Assert.Equal(2410, cluster.Inverter.Power);
        Assert.Equal(3620, cluster.Solar.Power);
        Assert.Equal(PowerFlowKind.Solar, cluster.Solar.FlowKind);

        var battery = Assert.IsType<PowerFlowNode>(cluster.Battery);
        Assert.Equal(-1120, battery.Power);
        Assert.Equal(0.68, battery.StateOfCharge);
        Assert.Equal("BYD HVS", battery.Name);
        Assert.Equal(PowerFlowKind.Battery, battery.FlowKind);
        Assert.Equal(PowerFlowState.Charging, battery.State);
        Assert.True(battery.IsReversed);
    }

    [Fact]
    public void An_importing_grid_and_a_discharging_battery_run_forwards()
    {
        var snapshot = PowerFlowSnapshot.From([Inverter("inv", 0, 800, storage: 800, soc: 0.4)], Site(grid: 1200, storage: 800), []);

        Assert.Equal(PowerFlowKind.Ac, snapshot.Grid!.FlowKind);
        Assert.Equal(PowerFlowState.GridImport, snapshot.Grid.State);
        Assert.False(snapshot.Grid.IsReversed);
        Assert.Equal(PowerFlowState.Discharging, snapshot.Inverters.Single().Battery!.State);
        Assert.False(snapshot.Inverters.Single().Battery!.IsReversed);
    }

    [Fact]
    public void An_inverter_without_a_battery_has_no_battery_node()
    {
        var snapshot = PowerFlowSnapshot.From([Inverter("inv", 1010, 950)], Site(), []);

        Assert.Null(snapshot.Inverters.Single().Battery);
        Assert.Equal("inv/solar", snapshot.Inverters.Single().Solar.Key);
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

    [Theory]
    [InlineData(0, true, PowerFlowState.Standby)]
    [InlineData(4.9, true, PowerFlowState.Standby)]
    [InlineData(-4.9, true, PowerFlowState.Standby)]
    [InlineData(5, false, PowerFlowState.Discharging)]
    [InlineData(-5, false, PowerFlowState.Charging)]
    public void Idle_is_below_five_watts_either_way(double watts, bool isIdle, PowerFlowState state)
    {
        var node = new PowerFlowNode("b", PowerFlowNodeKind.Battery, string.Empty, watts, 0.5);

        Assert.Equal(isIdle, node.IsIdle);
        Assert.Equal(state, node.State);
        Assert.Equal(isIdle ? PowerFlowKind.Idle : PowerFlowKind.Battery, node.FlowKind);
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
