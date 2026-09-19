---
paths:
  - HomeAutomationClient/HomeAutomationClient/Views/SmartMeterDetailsView.axaml
  - HomeAutomationClient/HomeAutomationClient/Views/SmartMeterDetailsView.axaml.cs
  - HomeAutomationClient/HomeAutomationClient/ViewModels/SmartMeterDetailsViewModel.cs
---

# Lifecycle contract: SmartMeterDetailsView (Avalonia)

Port of the WPF `FroniusMonitor/Views/SmartMeterDetailsView.xaml`. 9 gauge groups, 30 gauges, no group switches.

## Ownership and lifetimes

View and view model are **transient** (`App.axaml.cs`), like the other detail views. How many there are is the
presenter's business, not the container's: inside `MainView` one page per view type is built and reused, which is
the single instance these views had until 2026-09-15, and on the desktop there is one page, one view model and one
window per device. See [[DialogSystem.Lifecycle]].

## This is the one view that takes no device

`SmartMeterDetailsViewModel` exposes only `IUpdateService`, and all gauges bind `UpdateService.SmartMeter?.…`.
Nothing is assigned before the view is shown, so `MainViewModel.ShowDetails` just swaps it in:

```csharp
case Gen24PowerMeter3P:
    MainViewContent = IoC.Get<SmartMeterDetailsView>();
```

**Do not "improve" this by handing the clicked meter to the view model.** The menu entry's `Gen24PowerMeter3P`
comes from `Sensors.PrimaryPowerMeter`, and `Gen24System.CopyFrom` assigns a fresh `Sensors` on every update — a
meter stored once would leave the whole view frozen after the first refresh. `UpdateService.SmartMeter` is an
`[ObservableProperty]`, so binding through it re-resolves to each new meter object and the gauges keep moving.
An earlier version did assign it and had exactly that bug.

The same reasoning covers the two supporting values, both taken from the update service:

- `UpdateService.MeterStatus` for the gauge background,
- `UpdateService.PrimaryGen24Config` for the export limits that drive the power gauges' minimum and maximum.

**Consequence:** the update service tracks exactly one meter, one status and one config, all of the primary
inverter. On a site with several inverters, picking any meter entry from the Details menu shows the primary one.
Fixing that means giving the update service per-inverter meters first; it cannot be solved in this view.

## Attach, detach and theme

`Loaded` subscribes and `Unloaded` unsubscribes `Application.Current.ActualThemeVariantChanged`. Mandatory pairing:
on a page that is kept and shown again a missed unsubscribe re-adds the handler on every navigation. Subscribe in `Loaded`, never in
the constructor.

`OnThemeChanged` re-notifies `Gen24Status.StatusCode` on `UpdateService.MeterStatus`, because the gauge background
comes from the `InverterBackgroundColor` converter, which resolves the theme brushes when it runs rather than
through `DynamicResource`.

## Gauges

Uses the shared `DetailsGauge` template from `Styles/Gauges.axaml`. The template paints
`{TemplateBinding Background}`; this view sets the gauge `Background` in its `WrapPanel` style with a
`MultiBinding` on `InverterBackgroundColor` carrying a single value, `UpdateService.MeterStatus?.StatusCode`. The
converter treats a `values.Count` of one as "no running override" and falls back to the neutral brush for unknown
status codes, which is what this view wants.

The frequency gauge uses `ValueStringFormat="N1"`, unlike the inverter's `N3`: the smart meter reports the
frequency with less precision.

**`ColorAllTicks` comes from neither this view nor its view model.** The `WrapPanel.GaugeGroups` style in
`Styles/DetailViews.axaml` sets it from the application resource `MainViewModel.ColorAllTicksResourceKey`, which
the main view's switch writes. A resource and not a binding up the tree, because on the desktop this page stands
in a window that has no `MainView` above it - see [[DialogSystem.Lifecycle]].

**The cos(phi) gauges read the amount, not the value.** `DialShowsAbsoluteValue` (on `Gauge`, set in the group's
style) puts the needle at `|cos phi|` while the read-out under it keeps the sign, asked for by the developer on
2026-09-19: the sign says which way the reactive power flows, the dial is about how good the power factor is, and
a needle crossing the whole scale when the sign flips reports a change that did not happen. It is read in
`Gauge.SetValue`, where the fraction of the scale is worked out, so both kinds of gauge get it from one place;
`Gauge2Text`, which builds the read-out, never sees it. The group's scale went from -1 to 1
with `MidIsBad` to **0 to 1 with `LowIsBad`** in the same breath, and its `Origin` setter went with it: with the
sign gone there is no lower half to show, and 1 is the good end of the dial rather than both ends being good.

## Known gaps

- The view has no public parameterless constructor, so the build reports `AVLN3001` for it. Expected: the view is
  only ever resolved from the container. Do not add one.
