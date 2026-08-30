---
paths:
  - HomeAutomationClient/HomeAutomationClient/Views/SmartMeterDetailsView.axaml
  - HomeAutomationClient/HomeAutomationClient/Views/SmartMeterDetailsView.axaml.cs
  - HomeAutomationClient/HomeAutomationClient/ViewModels/SmartMeterDetailsViewModel.cs
---

# Lifecycle contract: SmartMeterDetailsView (Avalonia)

Port of the WPF `FroniusMonitor/Views/SmartMeterDetailsView.xaml`. 9 gauge groups, 30 gauges, no group switches.

## Ownership and lifetimes

View and view model are **singletons** (`App.axaml.cs`), like the other detail views: one instance for the whole
application run, attached to and detached from the visual tree on every navigation.

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
on a singleton view a missed unsubscribe re-adds the handler on every navigation. Subscribe in `Loaded`, never in
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

## Known gaps

- `ColorAllTicks` forwards to `DashboardViewModel` and carries the same `//BUG:` note as the other detail view
  models; it should move to `MainViewModel` or to settings.
- The view has no public parameterless constructor, so the build reports `AVLN3001` for it. Expected: the view is
  only ever resolved from the container. Do not add one.
