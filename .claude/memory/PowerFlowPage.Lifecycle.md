---
paths:
  - HomeAutomationClient/HomeAutomationClient/Models/PowerFlowSnapshot.cs
  - HomeAutomationClient/HomeAutomationClient/Models/PowerFlowViewModelItems.cs
  - HomeAutomationClient/HomeAutomationClient/ViewModels/PowerFlowViewModel.cs
  - HomeAutomationClient/HomeAutomationClient/Views/PowerFlowView.axaml
  - HomeAutomationClient/HomeAutomationClient/Views/PowerFlowView.axaml.cs
  - HomeAutomationClient/HomeAutomationClient/App.axaml.cs
  - HomeAutomationClient/HomeAutomationClient/ViewModels/MainViewModel.cs
  - HomeAutomationClient/HomeAutomationClient/Converters/PowerFlowConverters.cs
  - HomeAutomationClient/HomeAutomationClient/App.axaml
  - HomeAutomationClient/HomeAutomationClient/Views/MainView.axaml
  - Fronius/Contracts/IPowerConsumer1P.cs
  - Fronius/Contracts/IPowerConsumer3P.cs
  - Fronius/Contracts/IPowerMeter1P.cs
  - Fronius/Contracts/IDimmable.cs
  - Fronius/Contracts/IHsvColorControl.cs
  - Fronius/Contracts/IColorTemperatureControl.cs
  - Fronius/Contracts/ISwitchable.cs
  - Fronius/Models/Charging/WattPilot.cs
  - HomeAutomationServer/Services/Modbus/ModbusServerService.cs
  - HomeAutomationServerTests/UnitTests/PowerFlowSnapshotTests.cs
  - HomeAutomationServerTests/UnitTests/PowerFlowViewModelItemsTests.cs
  - HomeAutomationServerTests/UnitTests/Avalonia/PowerFlowViewTests.cs
  - Plans/PowerflowPage-plan.md
---

# Lifecycle contract: the power flow page

Built 2026-09-16 from the developer's `Plans/PowerflowPage-plan.md`, with a dark design they found as the look
to aim for: cards on a deep ground, orange and green wires with a moving dash pattern between them. The plan is
theirs and is not edited here; what was decided in building it is written down below.

## What it shows

Where the power of the house comes from and where it goes, as cards with wires between them, under a title and a
legend. Left to right, because it is for the desktop and the desktop is wide:

| Column | Cards | Wire |
|---|---|---|
| Sources | The grid; then one **cluster** per inverter - its DC side stacked on the left (**one card per tracker**, then the battery), the inverter to their right | Each DC card into its own tap on the inverter's left edge, the taps 14 px apart around the middle; inverter and grid right into the **trunk** |
| Trunk | a vertical bus between the sources and the house | one tap per source and one for the house, a dot at each; **one wire per gap between two taps**, carrying the net of every tap above it, drawn downwards so that a negative net runs upwards |
| House | one wide card: consumption, self-sufficiency, own consumption | trunk into its left edge; its right edge into the **spine** junction |
| Consumers | every consumer that measures its power, wrapping to the width there is, and last **the rest of the house** | a spine down the left in two runs from the junction, one up and one down; one **rail** above each row, one **stub** down to each card |

**Topology, not a star.** Every line goes to a bus. With twenty consumers that is twenty short stubs and no
crossing; a star with a line from the house to every card is unreadable at eight. This is the decision the
whole page rests on, and the reason the wires are drawn from the laid out cards rather than the cards placed
around a drawing - see "The wires follow the cards".

**Motion carries the number.** An active wire is a dashed line over a faint trace, and the dashes move at a speed
that follows the watts on a log scale: `seconds per dash period = max(0.45, 3.2 − 0.72·log10(W))`, so 20 W
crawls, 7 kW is brisk, and neither blurs or stands still. **Direction follows the sign:** a wire is drawn the way
power usually goes - battery into inverter, grid into house - and `PowerFlowNode.IsReversed` (power < 0) runs
the dashes back the other way. So a charging battery and an exporting house are the two wires that run backwards.

**Idle is per kind** (`PowerFlowNode.IdleThreshold`): a producer - grid, tracker, inverter, battery - and the
house below **10 W**, an inverter at night still reporting a few watts of its own; a consumer below **0.2 W**,
because a plug that draws half a watt is switched on and worth seeing. **The spine and the rails are consumers
for this rule**: the synthetic nodes the view makes for them carry the sum of their rows and the consumer
threshold, so a rail above two lamps at 5 W moves like the lamps do. Made a `House` node at first, with the
producers' 10 W, a rail stood still above lit lamps. An idle wire is a solid trace in the idle colour with its
junction dot, never nothing: **every card has a connector**, on or off. The figure is dimmed.

**Colour is the kind of power, never the device and never the direction** (`PowerFlowKind`, the developer's
choice): **solar yellow, battery green, grid grey, everything inside the house blue** - from the inverters to the
house and from the house to its consumers. The grid's wire is grey whether the house imports or exports; the
dashes say which way. The legend at the top right lists the four and idle. Brushes are `Flow*` in `App.axaml`,
both variants. **The dark variant is the design's, not the app's**: ground `#0A1018`, cards `#131C2B`, on
purpose one step deeper than the rest of the app, and the page paints its own `FlowPageBackground` because as a
page it would otherwise sit on the window background. The light variant is the dialog's `#E8E8E8` ground, where
the developer approved the light look.

**Only name and figure on a consumer**, as the plan asks. A source has a state line under its figure - what a
battery or the grid is doing - and a battery a bar for its state of charge: a `Border.SocTrack` with a
`Border.SocFill` whose width is the track's times the charge (`Fraction` multi converter). Not a `ProgressBar`:
Fluent's, at four pixels high with `Maximum="1"`, drew full whatever the value.

**The inverter card carries the name the user gave the inverter** ("Roof south"); it and the grid card are ten
pixels wider than the rest (`Border.Card.Wide`, 160) so that such a name fits on two lines. The tracker cards are
named **in the inverter's own words**: the view model passes
`gen24Loc.GetLocalizedString(Gen24LocalizationSection.Channels, "MPPT1")` into `PowerFlowSnapshot.From`, which
takes a `Func<int, string>` for exactly that and falls back to "MPPT 1" when given none - the record has no
localization service and does not want one.

**The header** is the title, centred (`Resources.PowerFlow`, "Power Flow" as a headline in English), and under it
the legend in a box with the cards' corner rounding. **The house card** carries the app's own `HouseIcon` and,
under its figure, the two ratios of the dashboard's house block - self-sufficiency and own consumption; not the
grid figure, which the grid card says already.

## Where the figures come from

`PowerFlowSnapshot.From(inverters, site, consumers)` is the arithmetic, a pure function with its tests in
`PowerFlowSnapshotTests`. Nothing is computed that the dashboard does not have already:

- **The house is `HousePower.From(site, carPower: null)`**: the whole load, cars included, and the same
  self-sufficiency. The signs are the Gen24's (see [[House]]): a battery is positive while it discharges, the
  grid positive while the house imports, `LoadPower` negative while the house draws.
- **Per inverter** the inverter card reads `Sensors.PowerFlow.InverterAcPower` and is named
  `KeyedGen24System.ToString()`, the system name. **The trackers are `PowerFlowSnapshot.Trackers`**, the one
  place that knows which sensor is which tracker: `Sensors.Inverter.Solar1Power` and `Solar2Power` today, two
  more lines there for an inverter with four and nothing anywhere else. A tracker whose sensor is null is not
  there; one that reports nought is, idle. An inverter without tracker sensors gets one Solar card with
  `PowerFlow.SolarPower`, so the picture never lacks the sun. The battery card reads `PowerFlow.StoragePower`
  and `Sensors.Storage.StateOfCharge` (0..1, so `P0` prints it) and is named after the storage's model.
- **A consumer takes part exactly when it is an `IPowerMeter1P` with `CanMeasurePower`.** Its name is the
  device's own `DisplayName`, not the keyed device's text, which for a Fritz!DECT is
  "AVM FRITZ!DECT 200: Heat pump". A Wattpilot is drawn as a car; an air conditioner, which measures nothing, is
  left out rather than shown as nought.
- **The rest of the house** is the house consumption less every metered consumer, **not clamped at zero**, for
  the reasons `HousePower` gives: a reading a moment fresher than the inverter's, or a source this software
  cannot see, shows as what it is.
- `site` is passed as **null while no inverter has reported**: the site power flow is all zeros until then and
  would read as a house that consumes nothing. With no site there is no grid card and no rest of the house.

## The Wattpilot became a consumer, and three contracts got defaults

The plan wanted the consumers as a list of `IPowerConsumer1P` with `CanMeasurePower`, and the Wattpilot was not
one. Now it is: **`WattPilot : IPowerConsumer3P`**, a new contract that is an `IPowerConsumer1P` with the three
phases added (`ActivePowerL1..3`, `CurrentL1..3`, `PhaseVoltageL1..3`, named after `IPowerMeter3P`). The totals
sit on the single phase side, so everything that lists consumers by their power sees the charger without knowing
about phases. Every member is **implemented explicitly** so that none of it turns up in the JSON the server pushes
- `WattPilotModelTests.The_consumer_view_of_the_charger_stays_out_of_its_json` pins that. What the single phase
members mean for a three phase device is documented on the interface: the average phase voltage, the sum of the
currents.

- The charger cannot be switched (`CanSwitch` false, `TurnOnOff` throws): whether it charges is its mode, its
  rules and the car, and a switch would have to pick one thing to mean. `IsTurnedOn` is `IsChargingAllowed`.
- **`IDimmable`, `IHsvColorControl` and `IColorTemperatureControl` now have defaults that say "cannot"**, so a
  device that implements the consumer contract for its power meter alone does not have to say so in fifteen
  members. `FritzBoxDevice` overrides all of them explicitly and nothing changed for it. The two hue properties
  fall back on each other and so cannot default; a device without a hue implements `HueDegrees` as null once
  (the Wattpilot does), or the pair recurses. `ITemperatureSensor` and the power/voltage/current triple of
  `IPowerMeter1P` are the same kind of mutual fallback.
- **The Modbus server had to learn to look away.** `ModbusServerService.OnDeviceUpdate` switches on
  `IPowerMeter1P { CanMeasurePower: true, EnergyConsumed: > 0 }` for every device update the server sees and
  auto-maps a match as a single phase meter. A Wattpilot would have matched from this day on and put a third of
  the truth on the bus, so `case IPowerConsumer3P: break;` stands before that case. Nothing serves three phase
  consumers over Modbus yet; the TODO above the switch says so.

## A page, not a dialog

`MainViewModel.ShowPowerFlow` calls `pagePresenter.Show<PowerFlowView>(PowerFlowView.PageKey, …)`: a window of
its own on the desktop, the main view on the browser and the phones, exactly like a detail page and unlike the
price chart beside it in the **View** menu (`Resources.View`, the WPF menu's `_View` / `_Ansicht`; the menu
button template got `RecognizesAccessKey` so the underscore is a mnemonic and not a character). The plan said
"Settings", but a Settings menu already exists for the devices. One page, so the key is fixed. The page declares
`c:InitialWindowSize.Width="1500"`, `c:InitialWindowSize.LimitHeightToScreen="False"` and
`c:ZoomBox.IsScope="True"` on its root, has no size of its own, and reflows to whatever width it is given. On the
desktop its window is therefore 1500 wide and **as tall as its content, following it**: the cards arrive after
`Loaded`, so the window grows when they do and when a consumer is added, and shrinks when one goes, until the
user drags its height (see `DialogSystem.Lifecycle`, "keeps following it"). The screen bounds it, which Avalonia
and Windows enforce on their own. With a fixed height of 860 it opened with the lowest cards cut off, and
measured once on opening it was the height of its title and legend.

`PowerFlowView` and `PowerFlowViewModel` are transient in `App.axaml.cs`; the view resolves its view model in its
constructor, as the injection rule wants - **after** hooking `DataContextChanged`, or it never hears about it,
which is exactly the bug the headless tests caught first. The view model follows the devices from the view's
`Loaded` (`Initialize`, idempotent) to its `Unloaded` (`Stop`): on the browser that is every trip to the
dashboard and back, and the same page comes back with the same cards, because `MainViewPresenter` keeps one page
per type.

The page has no address of its own in the browser (see [[Navigation.Lifecycle]]); the Dashboard menu entry is
the way back, as for a detail page.

## Two halves, for two threads

The update service raises on the hub's thread, and a charging Wattpilot raises several times a second. Bound to
the snapshot's lists directly, every reading would have the items controls throw away twenty cards and build
them again. So:

1. **`PowerFlowViewModel.Snapshot`** is rebuilt on the hub's thread for every report of every followed device
   (each inverter's `Gen24System`, each consumer, the site flow, the two collections). It is cheap; nothing
   filters property names.
2. **`PowerFlowViewModel.Items`** (`PowerFlowViewModelItems`) is what the controls bind to: `Grid`, `House`,
   `SelfSufficiency`, `SelfConsumption`, `Inverters` - each a `PowerFlowInverterItem` with its inverter and a `DcSources`
   collection of trackers and battery - and `Consumers`, as stable `PowerFlowNodeItem`s whose `Node` is
   replaced in place. `Apply()` folds the latest snapshot in and returns **true only when a card came or went**
   - a device, a tracker, a battery, the grid. Same keys in the same order update in place; anything else
   rebuilds that collection.
3. **The view marshals** (`PowerFlowView.RequestRefresh`, one `Dispatcher.UIThread.Post` per burst) and calls
   `Apply` on the UI thread, as the interaction rule wants: the folding is the view model's, the thread is the
   view's. `PowerFlowViewModelItemsTests` covers the folding without a view.

## The wires follow the cards

`PowerFlowView.Route()` runs on every `LayoutUpdated` of the stage, finds every `Border.Card` with a
`PowerFlowNodeItem` for a data context, reads its rectangle in stage coordinates (`TranslatePoint`), and draws
the wires listed above into the `Wires` canvas, which is the first child of the stage so it lies under the cards.
Rows of consumers are cards with the same rounded top; a rail is 14 px above them, in the margin the consumer
card style leaves; the spine is 34 px right of the house, in the consumers' left margin.

**The trunk is a bus, not a pipe from the house.** Each tap on it feeds something in: the grid its import, an
inverter its AC output, the house its draw as a negative. The segment between two neighbouring taps carries the
sum of the taps above it (`trunk:<gap>`, a `House` node made by the view), so with the grid on top, then a
producing inverter, then the house, then an idle inverter, a few watts run up to the grid, the rest down to the
house, and nothing below it. The taps are signed, so **an inverter that charges its battery from the AC side** -
from the grid or from another inverter - has a negative AC power, takes from the bus, and the segments on the way
to it run towards it; its own wire runs back into it through `IsReversed` as it always did. As one wire with the
house's figure the trunk ran downwards from the top tap to the bottom one, past the house. `PowerFlowViewTests.The_trunk_carries_the_net_between_its_taps` holds it to this,
through `WireStates`, which also tells whether a wire runs reversed.

**Dots lie over wires, labels over dots** (`ZIndex` 1 and 2 on the canvas children, paths at 0). A wire's paths
and dots are added to the canvas as it is routed, so without this the trunk segments, routed after the taps on
them, painted over the taps' dots and the spine runs over the rails'. Every tap on the trunk has a dot, the
house's inlet included; the trunk segments and the spine runs have none of their own, the wires meeting them do.

**No wire passes a point twice.** The spine is a stub from the house to the junction (`spine`) and then a run
from the junction up to the highest rail (`spine:up`) and one down to the lowest (`spine:down`), each drawn
starting at the junction so that its dashes run away from the house, and each left out when there is no rail on
that side. As one path - house, up to the first rail, back down to the last - it passed the part above the house
twice and the two runs of dashes crossed over each other there. `PowerFlowViewTests.No_wire_passes_a_point_twice`
holds every wire to this. `PowerFlowView.WireStates` is internal for exactly these tests: the path and whether the
dashes move, per key.

**It writes to a wire only what changed** - the path string, the dot positions, the label text, the thickness
and kind - because setting a path's geometry invalidates layout, and an unconditional write on every layout pass
would loop for ever. Wires are keyed (`dc:<node>`, `ac:<inverter>`, `grid`, `trunk:<gap>`, `house`, `spine`,
`spine:up`, `spine:down`, `rail:<row>`, `stub:<consumer>`); after `Apply` says the structure changed, all of them are thrown away and
routed anew, otherwise they are updated in place. **A wire's kind starts out as null**, so that the first update
styles it whatever it is: with `Idle` as the initial value an idle-from-birth wire was never given a stroke or a
thickness, and the first screenshot had connectors on some idle devices and none on others.

**One animation frame callback** (`TopLevel.RequestAnimationFrame`) advances every moving wire's
`StrokeDashOffset`; there is no timer per wire. Dash lengths are 10 px on, 14 px off, stated in multiples of the
stroke thickness as `StrokeDashArray` wants, so the period changes with the thickness (3 px, 4 px above 2 kW).
The offset is kept in `0..period` and decreases for a forward flow - a smaller dash offset shifts the pattern
towards the end of the path. The loop starts when the view is attached and ends when it is detached.

The whole stage is in a `ScrollViewer` (vertical only) inside a `ZoomBox`, so a site with more inverters than
fit scrolls, and Ctrl with the wheel scales the picture.

## Where this is tested

- `PowerFlowSnapshotTests` - the arithmetic, the signs, who takes part, idle, the rest of the house.
- `PowerFlowViewModelItemsTests` - the folding: figures in place, structure changes and only those.
- `PowerFlowViewTests` (headless, in the Avalonia collection) - the page in its window at the declared size, a
  card per node - trackers and battery included - and a wire with a stroke to every one of them, idle ones too;
  a reading that updates a card **without rebuilding it** (same `Border`, same item); a consumer that appears and
  gets a card and a stub; a closed page that lets go of its devices.
- `WattPilotModelTests` - the charger as a consumer, and the JSON it does not leak into.

**Not tested, and not testable here:** how it looks, and that the dashes run the right way. The headless platform
draws nothing. The mockup the layout was agreed on is an HTML page with the same topology and the same speed
formula; if the Avalonia page ever looks wrong, that is the reference.

## Known gaps

- No address of its own in the browser, so a reload lands on the dashboard; the menu is the way back.
- A Toshiba air conditioner does not measure its power and so is not on the page at all, not even as idle.
- A tracker that reports nought is a card - an unused second MPPT shows as an idle "MPPT 2". Hiding it would
  need the configured peak power, and a peak of nought is as likely an unconfigured one as an unused tracker.
- The rails are one per row of cards, so the row height is whatever the tallest card in it is; a very long
  consumer name wraps and pushes its row's rail up with it.
- `PowerFlowNode.Name` of an inverter without a `Config` is "Fronius " with a trailing space, which is what
  `KeyedDevice.ToString()` produces there; the real app always has a config by the time the page can be opened.
