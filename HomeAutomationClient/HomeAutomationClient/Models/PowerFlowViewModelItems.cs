using System.Collections.ObjectModel;
using De.Hochstaetter.Fronius.Models;

namespace De.Hochstaetter.HomeAutomationClient.Models;

/// <summary>
/// One card's bindable object. The node inside is replaced with every snapshot; the item itself lives as long as
/// its key is in the picture, so the control that shows it is built once and its bindings do the rest.
/// </summary>
public sealed partial class PowerFlowNodeItem(PowerFlowNode node) : BindableBase
{
    public string Key { get; } = node.Key;

    [ObservableProperty]
    public partial PowerFlowNode Node { get; set; } = node;
}

/// <summary>One inverter cluster: three cards that move together, the battery one coming and going with the battery.</summary>
public sealed partial class PowerFlowInverterItem : BindableBase
{
    public PowerFlowInverterItem(PowerFlowInverter cluster)
    {
        Key = cluster.Inverter.Key;
        Inverter = new PowerFlowNodeItem(cluster.Inverter);
        Solar = new PowerFlowNodeItem(cluster.Solar);
        Battery = cluster.Battery is { } battery ? new PowerFlowNodeItem(battery) : null;
    }

    public string Key { get; }

    public PowerFlowNodeItem Inverter { get; }

    public PowerFlowNodeItem Solar { get; }

    [ObservableProperty]
    public partial PowerFlowNodeItem? Battery { get; private set; }

    /// <summary>Takes the new figures; true when a battery appeared or went, which is a new card and a new wire.</summary>
    public bool Update(PowerFlowInverter cluster)
    {
        Inverter.Node = cluster.Inverter;
        Solar.Node = cluster.Solar;

        switch (Battery, cluster.Battery)
        {
            case (null, null):
                return false;

            case ({ } item, { } battery):
                item.Node = battery;
                return false;

            case (_, { } battery):
                Battery = new PowerFlowNodeItem(battery);
                return true;

            default:
                Battery = null;
                return true;
        }
    }
}

/// <summary>
/// The stable side of the power flow page: what the controls bind to. A <see cref="PowerFlowSnapshot"/> arrives
/// on the hub's thread whenever anything reports; <see cref="Apply"/> folds it into these collections and items
/// on the UI thread, replacing figures in place and touching the collections only when a device came or went.
/// </summary>
/// <remarks>
/// The split is what keeps the page still: bound to the snapshot's lists directly, every update would have the
/// items controls throw away twenty cards and build them again, and a charging Wattpilot reports several times a
/// second. It is also what makes the wiring cheap for the view, which redraws only when <see cref="Apply"/> says
/// the structure changed.
/// </remarks>
public sealed partial class PowerFlowViewModelItems : BindableBase
{
    [ObservableProperty]
    public partial PowerFlowNodeItem? Grid { get; private set; }

    [ObservableProperty]
    public partial PowerFlowNodeItem House { get; private set; } = new(PowerFlowSnapshot.Empty.House);

    [ObservableProperty]
    public partial double? SelfSufficiency { get; private set; }

    public ObservableCollection<PowerFlowInverterItem> Inverters { get; } = [];

    public ObservableCollection<PowerFlowNodeItem> Consumers { get; } = [];

    /// <summary>
    /// Folds a snapshot in. Call it on the thread the collections are bound on.
    /// </summary>
    /// <returns>True when a card came or went - a device, a battery, the grid - so whoever draws the wires knows to draw them anew.</returns>
    public bool Apply(PowerFlowSnapshot snapshot)
    {
        var structureChanged = false;

        switch (Grid, snapshot.Grid)
        {
            case (null, null):
                break;

            case ({ } item, { } grid):
                item.Node = grid;
                break;

            case (_, { } grid):
                Grid = new PowerFlowNodeItem(grid);
                structureChanged = true;
                break;

            default:
                Grid = null;
                structureChanged = true;
                break;
        }

        House.Node = snapshot.House;
        SelfSufficiency = snapshot.SelfSufficiency;

        structureChanged |= Sync(Inverters, snapshot.Inverters, item => item.Key, cluster => cluster.Inverter.Key, (item, cluster) => item.Update(cluster), cluster => new PowerFlowInverterItem(cluster));
        structureChanged |= Sync(Consumers, snapshot.Consumers, item => item.Key, node => node.Key, (item, node) => { item.Node = node; return false; }, node => new PowerFlowNodeItem(node));
        return structureChanged;
    }

    /// <summary>
    /// The same keys in the same order update in place; anything else rebuilds the collection. A device list
    /// changes when something is plugged in or a session starts, not with every reading, so the simple rule
    /// costs nothing and never leaves a card showing a device that has gone.
    /// </summary>
    private static bool Sync<TItem, TSource>(ObservableCollection<TItem> items, IReadOnlyList<TSource> sources, Func<TItem, string> itemKey, Func<TSource, string> sourceKey, Func<TItem, TSource, bool> update, Func<TSource, TItem> create)
    {
        if (items.Count == sources.Count && items.Select(itemKey).SequenceEqual(sources.Select(sourceKey), StringComparer.Ordinal))
        {
            var changed = false;

            for (var i = 0; i < items.Count; i++)
            {
                changed |= update(items[i], sources[i]);
            }

            return changed;
        }

        items.Clear();

        foreach (var source in sources)
        {
            items.Add(create(source));
        }

        return true;
    }
}
