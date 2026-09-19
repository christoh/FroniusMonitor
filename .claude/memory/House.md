---
paths:
  - HomeAutomationClient/HomeAutomationClient/Controls/HouseControl.axaml
  - HomeAutomationClient/HomeAutomationClient/Controls/HouseControl.axaml.cs
  - HomeAutomationClient/HomeAutomationClient/ViewModels/HouseViewModel.cs
  - HomeAutomationClient/HomeAutomationClient/Models/HousePower.cs
  - HomeAutomationClient/HomeAutomationClient/Views/DashboardView.axaml
  - HomeAutomationClient/HomeAutomationClient/Contracts/IPowerDisplayOptions.cs
  - HomeAutomationClient/HomeAutomationClient/Misc/CollectionFollowing.cs
  - HomeAutomationServerTests/UnitTests/HouseViewModelTests.cs
  - HomeAutomationServerTests/UnitTests/Fakes/FakeUpdateService.cs
---

# The house block on the dashboard

Built 2026-09-14 from the developer's `House-Plan.md`: the gray stub under the inverter row of `DashboardView`
became a block that shows the house as a whole. Six figures, each as a `LinearRectangleGauge` with a label, a
value and a unit, in two columns so the block stays flat:

| Left column (W)       | Right column        |
| --------------------- | ------------------- |
| Solar (all panels)    | Self-sufficiency    |
| House (without cars)  | Own consumption     |
| Cars (all Wattpilots) | Loss (inverters)    |

## Where the numbers come from

`Models/HousePower.From(Gen24PowerFlow? flow, double? carPower, bool includeInverterPower = false)` is the
arithmetic, a pure function with its own
tests (`HousePowerTests`). `flow` is `IUpdateService.SitePowerFlow`, the sum over all inverters; `carPower` is the
sum of `WattPilot.PowerTotal`, `null` where there is no Wattpilot.

- **Signs are the Gen24's.** `LoadPower` is negative while the house consumes, `InverterAcPower` positive while
  the inverters deliver. The load includes the cars, so house = `-LoadPowerCorrected - carPower`. **It is not cut
  at 0** (since 2026-09-15). The load is what is left once the grid and the inverters are accounted for, so it
  turns positive whenever something feeds the house that this software cannot see - an old diesel generator with
  no data interface, a second inverter that reports to nobody - and briefly when a Wattpilot reading is newer
  than the inverter's and the cars appear to draw more than the whole load. A zero would hide both, and the
  unmonitored source is worth seeing: a house that reads negative is being fed from somewhere else.
- **Solar** is `Gen24PowerFlow.SolarPower`, the DC power of all panels.
- **Loss** is `Gen24PowerFlow.PowerLoss` = `StoragePower - InverterAcPower + SolarPower` (DC in that did not
  come out as AC).
- **Self-sufficiency** = `InverterAcPower / consumption`, **own consumption** = `consumption / InverterAcPower`,
  both clamped to 0..1 and given in percent. These are the formulas of the WPF `InverterControl`'s efficiency
  tab. The battery counts as own: it comes out of the inverter as AC. **Neither is ever null once there is an
  inverter** (since 2026-09-15): a house that consumes nothing needs nothing from the grid, so self-sufficiency is
  100 %, and where nothing is produced there is no production that could stay in the house, so own consumption is
  0 %. Only `flow: null` leaves them null.
- **No inverter yet** (`Inverters.Count == 0`): the view model passes `flow: null`, because the site power flow
  is all zeros until the first inverter reports and zeros would read as a house that consumes nothing.

## "Solar Web" mode

Added 2026-09-19, the WPF app's `AddInverterPowerToConsumption` under the name the developer uses for it. **The
inverters' loss counts as consumption of the house**, the way Fronius' own portal reports it: `HousePower.From`
adds `flow.PowerLoss` to the consumption when `includeInverterPower` is true, and the same switch reaches the
power flow page (see [[PowerFlowPage.Lifecycle]]). The WPF app puts the same arithmetic in
`MainWindow.OnPowerFlowChanged`, where it is spelled out as `LoadPowerCorrected + SolarPower + GridPowerCorrected
+ StoragePower` - that sum *is* `PowerLoss`, because the load is what the grid and the inverters deliver.

What it does **not** touch, each on purpose:

- **The loss itself stays on the block.** The developer asked for that explicitly: the loss row goes on showing
  `PowerLoss`, so the figure that was added to the consumption is still there to be read.
- **Self-sufficiency and own consumption stay on the real consumption.** Otherwise throwing the switch would make
  the house look less self-sufficient than it is, which is an artefact of the display and not a change in the
  house. The WPF app does not move them either.
- **The cars** are what they draw, and the grid is what the meter says.

On the power flow page one more thing follows from it: **each inverter card carries its own loss** there, because
that page draws wires from a running sum and would otherwise show the site's loss as power coming out of an
inverter that is switched off. See [[PowerFlowPage.Lifecycle]]. The dashboard has no such sum - its inverter
controls show what the device reports, as they always have.

The switch is `MainViewModel.IncludeInverterPower`, the third `ToggleButton` at the bottom of `MainView`, beside
the gauge colouring and the dark mode. The block does not take the whole main view model for it: `MainViewModel`
implements **`Contracts/IPowerDisplayOptions`**, a contract of that one property, registered in `App.axaml.cs`
as the same singleton and faked in the tests by `FakePowerDisplayOptions`. It is an `INotifyPropertyChanged`, and
`HouseViewModel` works the figures out again when it fires - a switch is not a device and says nothing through
the update service. Like the other two switches, it is **not saved**: it is on for as long as the app runs. (The
WPF app saves its copy with the settings.)

## Scales of the gauges

`HouseViewModel` owns them: the house gauge runs to `SitePvPeakPower` (10 kW while unknown), the loss gauge to a
twentieth of that, the solar gauge to 70 % of it (a clear summer day), the cars' gauge to the sum of
`MaximumChargingPowerPossibleSum` (11 kW per Wattpilot that has none). Colours: `HighIsBad` for house and loss, `AllIsGood` for the cars (charging at full power is not a fault),
`LowIsBad` for the two ratios and for the solar power.

## How it follows the data

`HouseViewModel` is a singleton (`App.axaml.cs`) resolved by `HouseControl`'s code behind, as the injection rule
wants. It subscribes to the update service's `PropertyChanged`, to `SitePowerFlow.PropertyChanged`
(`Refresh(true)` raises an empty name), to `Inverters` and `AllPowerConsumers` `CollectionChanged`, and to every
Wattpilot's `PropertyChanged`. All of that arrives on the hub's thread and the view model sets plain properties
there, which bindings tolerate; it never touches a view collection - see the duplicate-menu incident noted in the
Toshiba memory for why.

**Of the service it hears only what `UpdateNow` reads** (since 2026-09-17): `SitePowerFlow`, `Inverters`,
`AllPowerConsumers` and `SitePvPeakPower`. Every report of an inverter also has the service announce `SmartMeter`,
`MeterStatus` and `PrimaryGen24Config` - each a new object, because `Gen24System.CopyFrom` replaces the sensors -
and the catch-all `Update()` that used to answer them worked the house out four times per report for one change
of the flow. None of those is a figure of this block.

**The service replaces its collections**, both at logout and `Inverters` whenever an inverter appears, and this
singleton outlives all of that. So the collection it has a handler on is kept in a field and re-followed through
`Misc/CollectionFollowing.Follow` whenever the service announces the property - a no-op while it is the same
instance. Before that, the handlers stayed on the collections of the first login: after logging out and back in
the car row kept the Wattpilots of the session before and heard none of the new ones. `HouseViewModelTests` pins
the following; the fake service (`FakeUpdateService`) replaces its collections the way the real one does.

The `Power` property is one `HousePower` record replaced as a whole, so every binding under `Power.` updates
together. `HasCars` hides the cars' row: the four elements of that row carry `Classes="Cars"` and a style
`:is(Control).Cars` binds their `IsVisible`, because a Grid row cannot be hidden as one.

## Layout

`HouseControl` is laid out at `Width="1000"` and `DashboardView` wraps it in `<Viewbox Stretch="Uniform">`, which
is what makes it span the dashboard and grow with the window, as the plan asked. Do not give it a Height: the
Viewbox derives the scale from the width alone. The `HouseIcon` at the left is the one from `Assets/Images`.
