using De.Hochstaetter.Fronius.Models.Charging;

namespace De.Hochstaetter.HomeAutomationClient.Models;

/// <summary>What a node of the power flow page is, which decides its icon, its caption, its idle threshold and the colour of its wire.</summary>
public enum PowerFlowNodeKind
{
    Grid,

    /// <summary>One tracker of an inverter - a Gen24 has two - or, where the inverter does not report them one by one, its panels as a whole.</summary>
    Solar,
    Inverter,
    Battery,
    House,

    /// <summary>A wallbox. A consumer like the others, drawn with a car.</summary>
    Car,

    /// <summary>A metered consumer: a Fritz!DECT plug, and whatever else measures its own power.</summary>
    Consumer,

    /// <summary>What the house draws beyond the metered consumers - the lights, the oven, everything without a plug of its own.</summary>
    RestOfHouse,
}

/// <summary>
/// The kind of power on a wire, which is what its colour says. Never the device, and never the direction: a
/// battery's wire is battery coloured whether it charges or discharges, the grid's whether the house imports or
/// exports - the moving dashes say which way.
/// </summary>
public enum PowerFlowKind
{
    Idle,
    Solar,
    Battery,
    Grid,

    /// <summary>AC inside the house: from the inverters to the house, and from the house to its consumers.</summary>
    House,
}

/// <summary>The one line under a source's figure that says what it is doing. <see cref="None"/> for a node that has nothing to say.</summary>
public enum PowerFlowState
{
    None,
    Idle,
    Charging,
    Discharging,
    Standby,
    FeedIn,
    GridImport,
}

/// <summary>
/// One card of the power flow page and the wire that leads to it.
/// </summary>
/// <param name="Key">What tells this node from every other across snapshots - the device key, or a fixed name for the grid and the house.</param>
/// <param name="Name">The device's own name where it has one: the inverter's system name, "MPPT 1" for a tracker, the battery's model, the plug's name. Empty for the grid and the house.</param>
/// <param name="Power">
/// In watts, with the Gen24's signs: a battery is positive while it discharges and negative while it charges, the
/// grid is positive while the house imports and negative while it exports. Everything else is positive while it does
/// anything. <see langword="null"/> where nothing has reported yet.
/// </param>
/// <param name="StateOfCharge">0 to 1, batteries only.</param>
public sealed record PowerFlowNode(string Key, PowerFlowNodeKind Kind, string Name, double? Power, double? StateOfCharge = null)
{
    /// <summary>Below this many watts a producer - and the house - is idle: an inverter at night still reports a few watts of its own.</summary>
    public const double ProducerIdleThreshold = 10;

    /// <summary>Below this many watts a consumer is idle. Tighter than the producers, because a plug that draws half a watt is switched on and worth seeing.</summary>
    public const double ConsumerIdleThreshold = 0.2;

    public double IdleThreshold => Kind is PowerFlowNodeKind.Consumer or PowerFlowNodeKind.Car or PowerFlowNodeKind.RestOfHouse ? ConsumerIdleThreshold : ProducerIdleThreshold;

    /// <summary>Whether the wire stands still and the figure is dimmed. A node that has not reported is idle.</summary>
    public bool IsIdle => !(Math.Abs(Power ?? 0) >= IdleThreshold);

    public PowerFlowKind FlowKind => IsIdle ? PowerFlowKind.Idle : Kind switch
    {
        PowerFlowNodeKind.Solar => PowerFlowKind.Solar,
        PowerFlowNodeKind.Battery => PowerFlowKind.Battery,
        PowerFlowNodeKind.Grid => PowerFlowKind.Grid,
        _ => PowerFlowKind.House,
    };

    /// <summary>
    /// Whether the power runs against the direction the wire is drawn in. A wire is drawn the way power usually
    /// goes - from the battery into the inverter, from the grid into the house - so a charging battery and an
    /// exporting house are the two that run backwards.
    /// </summary>
    public bool IsReversed => Power < 0;

    public PowerFlowState State => Kind switch
    {
        PowerFlowNodeKind.Battery when IsIdle => PowerFlowState.Standby,
        PowerFlowNodeKind.Battery => Power > 0 ? PowerFlowState.Discharging : PowerFlowState.Charging,
        PowerFlowNodeKind.Grid when IsIdle => PowerFlowState.Idle,
        PowerFlowNodeKind.Grid => Power > 0 ? PowerFlowState.GridImport : PowerFlowState.FeedIn,
        _ => PowerFlowState.None,
    };
}

/// <summary>One inverter with what hangs off its DC side: its trackers, and its battery where it has one.</summary>
public sealed record PowerFlowInverter(PowerFlowNode Inverter, IReadOnlyList<PowerFlowNode> Solar, PowerFlowNode? Battery)
{
    /// <summary>The DC side in the order the cards are stacked: the trackers from the top, the battery last.</summary>
    public IEnumerable<PowerFlowNode> DcSources => Battery is { } battery ? Solar.Append(battery) : Solar;
}

/// <summary>
/// Everything the power flow page shows at one moment, worked out from what the update service holds. Immutable
/// and replaced as a whole, so the view never sees half an update; <see cref="PowerFlowViewModelItems"/> is what
/// turns a run of these into stable, bindable objects.
/// </summary>
/// <param name="Grid">The grid, or <see langword="null"/> before the first inverter has reported.</param>
/// <param name="SelfSufficiency">In percent, as <see cref="HousePower.SelfSufficiency"/> has it.</param>
/// <param name="SelfConsumption">In percent, as <see cref="HousePower.SelfConsumption"/> has it.</param>
/// <param name="Consumers">Every metered consumer plus, last, the rest of the house.</param>
public sealed record PowerFlowSnapshot(PowerFlowNode? Grid, IReadOnlyList<PowerFlowInverter> Inverters, PowerFlowNode House, double? SelfSufficiency, double? SelfConsumption, IReadOnlyList<PowerFlowNode> Consumers)
{
    public const string GridKey = "grid";
    public const string HouseKey = "house";
    public const string RestOfHouseKey = "rest-of-house";

    public static PowerFlowSnapshot Empty { get; } = new(null, [], new PowerFlowNode(HouseKey, PowerFlowNodeKind.House, string.Empty, null), null, null, []);

    /// <param name="inverters">The update service's inverters.</param>
    /// <param name="site">The site power flow, the sum over all inverters, or <see langword="null"/> while no inverter has reported - the sum is all zeros then and would read as a house that consumes nothing.</param>
    /// <param name="consumers">The update service's power consumers. Only those that measure their power take part; an air conditioner that cannot is left out rather than shown as nought.</param>
    /// <param name="trackerName">
    /// The name of a tracker from its number, the inverter's own words where they are known - the caller has the
    /// inverter's localization, this record does not. "MPPT 1" where nothing is passed.
    /// </param>
    /// <param name="includeInverterPower">
    /// "Solar Web" mode, see <see cref="IPowerDisplayOptions.IncludeInverterPower"/>. The house counts the
    /// inverters' loss as its own consumption, and since the loss is nothing a plug can measure, what is left of
    /// it after the metered consumers lands in the rest of the house - which is where everything unmetered goes.
    /// Each inverter carries its own loss on the way out, so that what the sources deliver is still exactly what
    /// the grid and the house take: the page draws its wires from that balance, and a house that took more than
    /// the sources gave would show the difference as power out of an inverter that is switched off.
    /// </param>
    public static PowerFlowSnapshot From(IReadOnlyList<KeyedGen24System> inverters, Gen24PowerFlow? site, IReadOnlyList<IKeyedDevice> consumers, Func<int, string>? trackerName = null, bool includeInverterPower = false)
    {
        trackerName ??= number => $"MPPT {number}";

        // The house figures are the dashboard's: the whole load, cars included, and the same self-sufficiency.
        var house = HousePower.From(site, carPower: null, includeInverterPower);
        var houseNode = new PowerFlowNode(HouseKey, PowerFlowNodeKind.House, string.Empty, house.HouseConsumption);
        var grid = site is null ? null : new PowerFlowNode(GridKey, PowerFlowNodeKind.Grid, string.Empty, site.GridPowerCorrected);

        var inverterNodes = inverters.Select(keyed =>
        {
            var sensors = keyed.Device.Sensors;
            var flow = sensors?.PowerFlow;
            var storage = sensors?.Storage;

            // One card per tracker the inverter reports. Where it reports none one by one, its panels as a whole,
            // so that the picture never lacks the sun.
            var trackers = Trackers(sensors?.Inverter)
                .Select(tracker => new PowerFlowNode($"{keyed.Key}/mppt{tracker.Number}", PowerFlowNodeKind.Solar, trackerName(tracker.Number), tracker.Power))
                .ToList();

            if (trackers.Count == 0)
            {
                trackers.Add(new PowerFlowNode($"{keyed.Key}/solar", PowerFlowNodeKind.Solar, string.Empty, flow?.SolarPower));
            }

            // In Solar Web mode the inverter hands its whole DC input to the house: what leaves it as AC, plus
            // what it kept for itself and the house is now counting as consumption. **Per inverter, from its own
            // PowerFlow.** The site's loss added to the house alone leaves exactly that many watts running down
            // the trunk past the house to whatever tap is below it - on 2026-09-19 the picture showed 41 W coming
            // out of an inverter that was switched off. With every inverter carrying its own loss the sum closes
            // again, and one that produces nothing loses nothing and injects nothing.
            var acPower = flow is null ? (double?)null : includeInverterPower ? flow.InverterAcPower + flow.PowerLoss : flow.InverterAcPower;

            return new PowerFlowInverter
            (
                // The inverter carries the name the user gave it - "Roof south" - not the model on its type plate.
                new PowerFlowNode(keyed.Key, PowerFlowNodeKind.Inverter, keyed.ToString() ?? string.Empty, acPower),
                trackers,
                storage is null ? null : new PowerFlowNode($"{keyed.Key}/battery", PowerFlowNodeKind.Battery, storage.Model ?? string.Empty, flow?.StoragePower, storage.StateOfCharge)
            );
        }).ToList();

        // The device's own display name, not the menu entry's text: a Fritz!DECT prints itself as "AVM FRITZ!DECT
        // 200: Heat pump" and the card wants the "Heat pump".
        var metered = consumers
            .Select(keyed => (keyed.Key, Meter: keyed.Device as IPowerMeter1P))
            .Where(consumer => consumer.Meter is { CanMeasurePower: true })
            .Select(consumer => new PowerFlowNode(consumer.Key, consumer.Meter is WattPilot ? PowerFlowNodeKind.Car : PowerFlowNodeKind.Consumer, consumer.Meter!.DisplayName, consumer.Meter.ActivePower))
            .ToList();

        if (house.HouseConsumption is { } consumption)
        {
            // Not clamped at zero, for the reasons HousePower gives: a reading that is a moment fresher than the
            // inverter's, or a source this software cannot see, shows here as what it is instead of being hidden.
            metered.Add(new PowerFlowNode(RestOfHouseKey, PowerFlowNodeKind.RestOfHouse, string.Empty, consumption - metered.Sum(node => node.Power ?? 0)));
        }

        return new PowerFlowSnapshot(grid, inverterNodes, houseNode, house.SelfSufficiency, house.SelfConsumption, metered);
    }

    /// <summary>
    /// The trackers an inverter reports, numbered as on its type plate. This is the one place that knows which
    /// sensor is which tracker: a Gen24 has two, and an inverter with four gets two more lines here and nothing
    /// anywhere else. A tracker whose sensor is missing is not there; one that reports nought is.
    /// </summary>
    private static IEnumerable<(int Number, double? Power)> Trackers(Gen24Inverter? inverter)
    {
        if (inverter is null)
        {
            yield break;
        }

        if (inverter.Solar1Power is { } first)
        {
            yield return (1, first);
        }

        if (inverter.Solar2Power is { } second)
        {
            yield return (2, second);
        }
    }
}
