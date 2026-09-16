---
paths:
  - HomeAutomationClient/HomeAutomationClient/Models/PowerFlowSnapshot.cs
  - HomeAutomationClient/HomeAutomationClient/Models/PowerFlowViewModelItems.cs
  - HomeAutomationClient/HomeAutomationClient/ViewModels/Dialogs/PowerFlowViewModel.cs
  - HomeAutomationClient/HomeAutomationClient/Views/Dialogs/PowerFlowView.axaml
  - HomeAutomationClient/HomeAutomationClient/Views/Dialogs/PowerFlowView.axaml.cs
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

Where the power of the house comes from and where it goes, as cards with wires between them. Left to right,
because it is for the desktop and the desktop is wide:

| Column | Cards | Wire |
|---|---|---|
| Sources | The grid; then one **cluster** per inverter - its panels above, its battery below, the inverter to their right | Panels and battery into the inverter's left edge (two DC taps, 12 px above and below the middle); inverter and grid right into the **trunk** |
| Trunk | a vertical bus between the sources and the house | one tap per source, a dot at each |
| House | one wide card: consumption, self-sufficiency, grid | trunk into its left edge; its right edge into the **spine** |
| Consumers | every consumer that measures its power, wrapping to the width there is, and last **the rest of the house** | a spine down the left, one **rail** above each row, one **stub** down to each card |

**Topology, not a star.** Every line goes to a bus. With twenty consumers that is twenty short stubs and no
crossing; a star with a line from the house to every card is unreadable at eight. This is the decision the
whole page rests on, and the reason the wires are drawn from the laid out cards rather than the cards placed
around a drawing - see "The wires follow the cards".

**Motion carries the number.** An active wire is a dashed line over a faint trace, and the dashes move at a speed
that follows the watts on a log scale: `seconds per dash period = max(0.45, 3.2 − 0.72·log10(W))`, so 20 W
crawls, 7 kW is brisk, and neither blurs or stands still. Below `PowerFlowNode.IdleThreshold` (5 W) a wire is a
solid faint trace and the figure is dimmed. **Direction follows the sign:** a wire is drawn the way power usually
goes - battery into inverter, grid into house - and `PowerFlowNode.IsReversed` (power < 0) runs the dashes back
the other way. So a charging battery and an exporting house are the two wires that run backwards.

**Colour is the kind of power, never the device** (`PowerFlowKind`): solar DC green, battery DC blue-green, AC
drawn orange, AC exported blue, idle a trace of the ground. The grid's wire turns from orange to blue with the
sign. Brushes are `Flow*` in `App.axaml`, both variants; the page sits on `DialogBackground`, which in the dark
variant is the deep navy the design was drawn on.

**Only name and figure on a consumer**, as the plan asks. A source has a state line under its figure - what a
battery or the grid is doing, with the state of charge for the battery - and nothing else.

## Where the figures come from

`PowerFlowSnapshot.From(inverters, site, consumers)` is the arithmetic, a pure function with its tests in
`PowerFlowSnapshotTests`. Nothing is computed that the dashboard does not have already:

- **The house is `HousePower.From(site, carPower: null)`**: the whole load, cars included, and the same
  self-sufficiency. The signs are the Gen24's (see [[House]]): a battery is positive while it discharges, the
  grid positive while the house imports, `LoadPower` negative while the house draws.
- **Per inverter** the nodes read `Sensors.PowerFlow` - `SolarPower`, `InverterAcPower`, `StoragePower` - and
  the battery card `Sensors.Storage.StateOfCharge` (0..1, so `P0` prints it). The panels carry the inverter's
  name (`KeyedGen24System.ToString()`, the system name), the inverter card its model.
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

## Two halves, for two threads

The update service raises on the hub's thread, and a charging Wattpilot raises several times a second. Bound to
the snapshot's lists directly, every reading would have the items controls throw away twenty cards and build
them again. So:

1. **`PowerFlowViewModel.Snapshot`** is rebuilt on the hub's thread for every report of every followed device
   (each inverter's `Gen24System`, each consumer, the site flow, the two collections). It is cheap; nothing
   filters property names.
2. **`PowerFlowViewModel.Items`** (`PowerFlowViewModelItems`) is what the controls bind to: `Grid`, `House`,
   `SelfSufficiency`, `Inverters`, `Consumers` as stable `PowerFlowNodeItem`s whose `Node` is replaced in
   place. `Apply()` folds the latest snapshot in and returns **true only when a card came or went** - a device, a
   battery, the grid. Same keys in the same order update in place; anything else rebuilds the collection.
3. **The view marshals** (`PowerFlowView.RequestRefresh`, one `Dispatcher.UIThread.Post` per burst) and calls
   `Apply` on the UI thread, as the interaction rule wants: the folding is the view model's, the thread is the
   view's. `PowerFlowViewModelItemsTests` covers the folding without a view.

`AbortAsync` unsubscribes from everything before it closes, and `PowerFlowViewTests.Closing_the_page_lets_go_of_the_devices`
checks that a device reporting afterwards changes nothing.

## The wires follow the cards

`PowerFlowView.Route()` runs on every `LayoutUpdated` of the stage, finds every `Border.Card` with a
`PowerFlowNodeItem` for a data context, reads its rectangle in stage coordinates (`TranslatePoint`), and draws
the wires listed above into the `Wires` canvas, which is the first child of the stage so it lies under the cards.
Rows of consumers are cards with the same rounded top; a rail is 14 px above them, in the margin the consumer
card style leaves; the spine is 34 px right of the house, in the consumers' left margin.

**It writes to a wire only what changed** - the path string, the dot positions, the label text, the thickness
and kind - because setting a path's geometry invalidates layout, and an unconditional write on every layout pass
would loop for ever. Wires are keyed (`solar:<inverter>`, `ac:<inverter>`, `grid`, `trunk`, `house`, `spine`,
`rail:<row>`, `stub:<consumer>`); after `Apply` says the structure changed, all of them are thrown away and
routed anew, otherwise they are updated in place.

**One animation frame callback** (`TopLevel.RequestAnimationFrame`) advances every moving wire's
`StrokeDashOffset`; there is no timer per wire. Dash lengths are 10 px on, 14 px off, stated in multiples of the
stroke thickness as `StrokeDashArray` wants, so the period changes with the thickness (3 px, 4 px above 2 kW).
The offset is kept in `0..period` and decreases for a forward flow - a smaller dash offset shifts the pattern
towards the end of the path. The loop starts when the view is attached and ends when it is detached.

The whole stage is in a `ScrollViewer` (vertical only) inside a `ZoomBox`, so a site with more inverters than
fit scrolls, and Ctrl with the wheel scales the picture.

## How it opens

From the **Energy** menu of the menu bar, beside the price chart, which moved there from a button of its own -
the plan said "Settings", but a Settings menu already existed for the devices and would have made two. Like the
price chart it is a resizable dialog, so a window of its own on the desktop and the dialog frame elsewhere
(`MainViewModel.ShowPowerFlow`, concurrent and gated like the others; see [[DialogSystem.Lifecycle]]).

The body declares **`c:InitialWindowSize.Width="1500" Height="860"`** and has no size of its own, so the window
opens at that size and the consumers wrap to whatever width the user then gives it. That property was a page's
until this day; `WindowDialogPresentation.Open` now reads it off a dialog body too. Inside the dialog frame the
`MaxWidth` of 1600 does the wrapping job.

## Where this is tested

- `PowerFlowSnapshotTests` - the arithmetic, the signs, who takes part, idle, the rest of the house.
- `PowerFlowViewModelItemsTests` - the folding: figures in place, structure changes and only those.
- `PowerFlowViewTests` (headless, in the Avalonia collection) - the window at its declared size, a card per node
  and the wires between, a reading that updates a card **without rebuilding it** (same `Border`, same item),
  a consumer that appears and gets a card and a stub, and a closed page that lets go of its devices.
- `WattPilotModelTests` - the charger as a consumer, and the JSON it does not leak into.

**Not tested, and not testable here:** how it looks, and that the dashes run the right way. The headless platform
draws nothing. The mockup the layout was agreed on is an HTML page with the same topology and the same speed
formula; if the Avalonia page ever looks wrong, that is the reference.

## Known gaps

- No legend. Colour is the kind of power and the mockup had a legend for it; the page relies on the state lines.
- A Toshiba air conditioner does not measure its power and so is not on the page at all, not even as idle.
- The rails are one per row of cards, so the row height is whatever the tallest card in it is; a very long
  consumer name wraps and pushes its row's rail up with it.
- `PowerFlowNode.Name` of an inverter without a `Config` is "Fronius " with a trailing space, which is what
  `KeyedDevice.ToString()` produces there; the real app always has a config by the time the page can be opened.
