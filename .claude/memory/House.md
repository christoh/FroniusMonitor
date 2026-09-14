---
paths:
  - HomeAutomationClient/HomeAutomationClient/Controls/HouseControl.axaml
  - HomeAutomationClient/HomeAutomationClient/Controls/HouseControl.axaml.cs
  - HomeAutomationClient/HomeAutomationClient/ViewModels/HouseViewModel.cs
  - HomeAutomationClient/HomeAutomationClient/Models/HousePower.cs
  - HomeAutomationClient/HomeAutomationClient/Views/DashboardView.axaml
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

`Models/HousePower.From(Gen24PowerFlow? flow, double? carPower)` is the arithmetic, a pure function with its own
tests (`HousePowerTests`). `flow` is `IUpdateService.SitePowerFlow`, the sum over all inverters; `carPower` is the
sum of `WattPilot.PowerTotal`, `null` where there is no Wattpilot.

- **Signs are the Gen24's.** `LoadPower` is negative while the house consumes, `InverterAcPower` positive while
  the inverters deliver. The load includes the cars, so house = `-LoadPowerCorrected - carPower`, cut at 0 because
  a Wattpilot reading can be a moment newer than the inverter's.
- **Solar** is `Gen24PowerFlow.SolarPower`, the DC power of all panels.
- **Loss** is `Gen24PowerFlow.PowerLoss` = `StoragePower - InverterAcPower + SolarPower` (DC in that did not
  come out as AC).
- **Self-sufficiency** = `InverterAcPower / consumption`, **own consumption** = `consumption / InverterAcPower`,
  both clamped to 0..1 and given in percent. These are the formulas of the WPF `InverterControl`'s efficiency
  tab. The battery counts as own: it comes out of the inverter as AC. A ratio whose denominator is zero is
  `null` and shows `---`, not 100 %.
- **No inverter yet** (`Inverters.Count == 0`): the view model passes `flow: null`, because the site power flow
  is all zeros until the first inverter reports and zeros would read as a house that consumes nothing.

## Scales of the gauges

`HouseViewModel` owns them: the house gauge runs to `SitePvPeakPower` (10 kW while unknown), the loss gauge to a
twentieth of that, the solar gauge to 70 % of it (a clear summer day), the cars' gauge to the sum of
`MaximumChargingPowerPossibleSum` (11 kW per Wattpilot that has none). Colours: `HighIsBad` for house and loss, `AllIsGood` for the cars (charging at full power is not a fault),
`LowIsBad` for the two ratios and for the solar power.

## How it follows the data

`HouseViewModel` is a singleton (`App.axaml.cs`) resolved by `HouseControl`'s code behind, as the injection rule
wants. It subscribes to the update service's `PropertyChanged` (re-hooks when `SitePowerFlow` is replaced at
logout), to `SitePowerFlow.PropertyChanged` (`Refresh(true)` raises an empty name), to `Inverters` and
`AllPowerConsumers` `CollectionChanged`, and to every Wattpilot's `PropertyChanged`. All of that arrives on the
hub's thread and the view model sets plain properties there, which bindings tolerate; it never touches a view
collection - see the duplicate-menu incident noted in the Toshiba memory for why.

The `Power` property is one `HousePower` record replaced as a whole, so every binding under `Power.` updates
together. `HasCars` hides the cars' row: the four elements of that row carry `Classes="Cars"` and a style
`:is(Control).Cars` binds their `IsVisible`, because a Grid row cannot be hidden as one.

## Layout

`HouseControl` is laid out at `Width="1000"` and `DashboardView` wraps it in `<Viewbox Stretch="Uniform">`, which
is what makes it span the dashboard and grow with the window, as the plan asked. Do not give it a Height: the
Viewbox derives the scale from the width alone. The `HouseIcon` at the left is the one from `Assets/Images`.
