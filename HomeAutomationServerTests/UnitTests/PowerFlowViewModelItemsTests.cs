using De.Hochstaetter.HomeAutomationClient.Models;

namespace De.Hochstaetter.HomeAutomationServerTests.UnitTests;

/// <summary>
/// The stable side of the power flow page: a snapshot folded into items that live on, so that the cards are never
/// rebuilt for a reading and are rebuilt exactly when a device comes or goes.
/// </summary>
public sealed class PowerFlowViewModelItemsTests
{
    private static PowerFlowNode Node(string key, double? power, PowerFlowNodeKind kind = PowerFlowNodeKind.Consumer) => new(key, kind, key, power);

    private static PowerFlowInverter Cluster(string key, double ac, double? battery = null) => new
    (
        Node(key, ac, PowerFlowNodeKind.Inverter),
        Node($"{key}/solar", 1000, PowerFlowNodeKind.Solar),
        battery is { } power ? Node($"{key}/battery", power, PowerFlowNodeKind.Battery) : null
    );

    private static PowerFlowSnapshot Snapshot(PowerFlowNode? grid, IReadOnlyList<PowerFlowInverter> inverters, params PowerFlowNode[] consumers) =>
        new(grid, inverters, Node(PowerFlowSnapshot.HouseKey, 5000, PowerFlowNodeKind.House), 100, consumers);

    [Fact]
    public void A_new_reading_replaces_the_figures_and_keeps_the_items()
    {
        var items = new PowerFlowViewModelItems();
        Assert.True(items.Apply(Snapshot(Node("grid", 100, PowerFlowNodeKind.Grid), [Cluster("inv", 2000, -500)], Node("a", 10), Node("b", 20))));

        var grid = items.Grid;
        var inverter = items.Inverters.Single();
        var battery = inverter.Battery;
        var consumers = items.Consumers.ToList();

        var changed = items.Apply(Snapshot(Node("grid", -300, PowerFlowNodeKind.Grid), [Cluster("inv", 2500, 400)], Node("a", 11), Node("b", 0)));

        Assert.False(changed);
        Assert.Same(grid, items.Grid);
        Assert.Same(inverter, items.Inverters.Single());
        Assert.Same(battery, items.Inverters.Single().Battery);
        Assert.Equal(consumers, items.Consumers);

        Assert.Equal(-300, items.Grid!.Node.Power);
        Assert.Equal(2500, inverter.Inverter.Node.Power);
        Assert.Equal(400, battery!.Node.Power);
        Assert.Equal(11, items.Consumers[0].Node.Power);
        Assert.True(items.Consumers[1].Node.IsIdle);
    }

    [Fact]
    public void A_consumer_that_comes_or_goes_rebuilds_the_list()
    {
        var items = new PowerFlowViewModelItems();
        items.Apply(Snapshot(null, [], Node("a", 10), Node("b", 20)));
        var before = items.Consumers.ToList();

        Assert.True(items.Apply(Snapshot(null, [], Node("a", 10), Node("b", 20), Node("c", 30))));
        Assert.Equal(["a", "b", "c"], items.Consumers.Select(item => item.Key));
        Assert.DoesNotContain(before[0], items.Consumers);

        Assert.True(items.Apply(Snapshot(null, [], Node("a", 10), Node("c", 30))));
        Assert.Equal(["a", "c"], items.Consumers.Select(item => item.Key));
    }

    [Fact]
    public void A_battery_that_appears_or_disappears_is_a_change_of_structure()
    {
        var items = new PowerFlowViewModelItems();
        items.Apply(Snapshot(null, [Cluster("inv", 2000)]));
        var inverter = items.Inverters.Single();
        Assert.Null(inverter.Battery);

        Assert.True(items.Apply(Snapshot(null, [Cluster("inv", 2000, -500)])));
        Assert.Same(inverter, items.Inverters.Single());
        Assert.NotNull(inverter.Battery);

        Assert.False(items.Apply(Snapshot(null, [Cluster("inv", 2100, -600)])));

        Assert.True(items.Apply(Snapshot(null, [Cluster("inv", 2000)])));
        Assert.Null(inverter.Battery);
    }

    [Fact]
    public void The_grid_appears_with_the_first_inverter()
    {
        var items = new PowerFlowViewModelItems();
        Assert.False(items.Apply(Snapshot(null, [])));
        Assert.Null(items.Grid);

        Assert.True(items.Apply(Snapshot(Node("grid", 0, PowerFlowNodeKind.Grid), [])));
        Assert.NotNull(items.Grid);

        Assert.True(items.Apply(Snapshot(null, [])));
        Assert.Null(items.Grid);
    }

    [Fact]
    public void The_house_is_always_there_and_only_ever_updated()
    {
        var items = new PowerFlowViewModelItems();
        var house = items.House;

        items.Apply(Snapshot(null, []));
        Assert.Same(house, items.House);
        Assert.Equal(5000, house.Node.Power);
        Assert.Equal(100, items.SelfSufficiency);
    }
}
