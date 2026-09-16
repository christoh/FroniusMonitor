using De.Hochstaetter.Fronius.Models.Charging;

namespace De.Hochstaetter.HomeAutomationClient.Models;

/// <summary>What a node of the power flow page is, which decides its icon, its caption and the colour of its wire.</summary>
public enum PowerFlowNodeKind
{
    Grid,
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
/// The kind of power on a wire, which is what its colour says. Never the device: a battery's wire is battery
/// coloured whether it charges or discharges, and the grid's turns from AC drawn to AC exported with the sign.
/// </summary>
public enum PowerFlowKind
{
    Idle,
    Solar,
    Battery,
    Ac,
    Export,
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
/// <param name="Name">The device's own name where it has one: the inverter's system name, the battery's model, the plug's name. Empty for the grid and the house.</param>
/// <param name="Power">
/// In watts, with the Gen24's signs: a battery is positive while it discharges and negative while it charges, the
/// grid is positive while the house imports and negative while it exports. Everything else is positive while it does
/// anything. <see langword="null"/> where nothing has reported yet.
/// </param>
/// <param name="StateOfCharge">0 to 1, batteries only.</param>
public sealed record PowerFlowNode(string Key, PowerFlowNodeKind Kind, string Name, double? Power, double? StateOfCharge = null)
{
    /// <summary>Below this many watts a wire stands still and a figure is dimmed: a plug's standby draw is not a flow worth animating.</summary>
    public const double IdleThreshold = 5;

    public bool IsIdle => !(Math.Abs(Power ?? 0) >= IdleThreshold);

    public PowerFlowKind FlowKind => IsIdle ? PowerFlowKind.Idle : Kind switch
    {
        PowerFlowNodeKind.Solar => PowerFlowKind.Solar,
        PowerFlowNodeKind.Battery => PowerFlowKind.Battery,
        PowerFlowNodeKind.Grid when Power < 0 => PowerFlowKind.Export,
        _ => PowerFlowKind.Ac,
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

/// <summary>One inverter with what hangs off its DC side: its panels, and its battery where it has one.</summary>
public sealed record PowerFlowInverter(PowerFlowNode Inverter, PowerFlowNode Solar, PowerFlowNode? Battery);

/// <summary>
/// Everything the power flow page shows at one moment, worked out from what the update service holds. Immutable
/// and replaced as a whole, so the view never sees half an update; <see cref="PowerFlowViewModelItems"/> is what
/// turns a run of these into stable, bindable objects.
/// </summary>
/// <param name="Grid">The grid, or <see langword="null"/> before the first inverter has reported.</param>
/// <param name="SelfSufficiency">In percent, as <see cref="HousePower.SelfSufficiency"/> has it.</param>
/// <param name="Consumers">Every metered consumer plus, last, the rest of the house.</param>
public sealed record PowerFlowSnapshot(PowerFlowNode? Grid, IReadOnlyList<PowerFlowInverter> Inverters, PowerFlowNode House, double? SelfSufficiency, IReadOnlyList<PowerFlowNode> Consumers)
{
    public const string GridKey = "grid";
    public const string HouseKey = "house";
    public const string RestOfHouseKey = "rest-of-house";

    public static PowerFlowSnapshot Empty { get; } = new(null, [], new PowerFlowNode(HouseKey, PowerFlowNodeKind.House, string.Empty, null), null, []);

    /// <param name="inverters">The update service's inverters.</param>
    /// <param name="site">The site power flow, the sum over all inverters, or <see langword="null"/> while no inverter has reported - the sum is all zeros then and would read as a house that consumes nothing.</param>
    /// <param name="consumers">The update service's power consumers. Only those that measure their power take part; an air conditioner that cannot is left out rather than shown as nought.</param>
    public static PowerFlowSnapshot From(IReadOnlyList<KeyedGen24System> inverters, Gen24PowerFlow? site, IReadOnlyList<IKeyedDevice> consumers)
    {
        // The house figures are the dashboard's: the whole load, cars included, and the same self-sufficiency.
        var house = HousePower.From(site, carPower: null);
        var houseNode = new PowerFlowNode(HouseKey, PowerFlowNodeKind.House, string.Empty, house.HouseConsumption);
        var grid = site is null ? null : new PowerFlowNode(GridKey, PowerFlowNodeKind.Grid, string.Empty, site.GridPowerCorrected);

        var inverterNodes = inverters.Select(keyed =>
        {
            var flow = keyed.Device.Sensors?.PowerFlow;
            var storage = keyed.Device.Sensors?.Storage;

            return new PowerFlowInverter
            (
                new PowerFlowNode(keyed.Key, PowerFlowNodeKind.Inverter, keyed.Device.Model ?? string.Empty, flow?.InverterAcPower),
                // The panels carry the inverter's name: "Roof south" is what the user calls the roof, not the box under it.
                new PowerFlowNode($"{keyed.Key}/solar", PowerFlowNodeKind.Solar, keyed.ToString() ?? string.Empty, flow?.SolarPower),
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

        return new PowerFlowSnapshot(grid, inverterNodes, houseNode, house.SelfSufficiency, metered);
    }
}
