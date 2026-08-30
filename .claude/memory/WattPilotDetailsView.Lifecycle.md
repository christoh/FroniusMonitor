---
paths:
  - HomeAutomationClient/HomeAutomationClient/Views/WattPilotDetailsView.axaml
  - HomeAutomationClient/HomeAutomationClient/Views/WattPilotDetailsView.axaml.cs
  - HomeAutomationClient/HomeAutomationClient/ViewModels/WattPilotDetailsViewModel.cs
---

# Lifecycle contract: WattPilotDetailsView (Avalonia)

Port of the WPF `FroniusMonitor/Views/WattPilotDetailsView.xaml`. 7 gauge groups, 21 gauges, no group switches.

## Ownership and lifetimes

View and view model are **singletons** (`App.axaml.cs`), like the other detail views: one instance for the whole
application run, attached to and detached from the visual tree on every navigation.

## Navigation

`MainViewModel.ShowDetails`, `case WattPilot`. The device from the menu entry is assigned to the view model and
must be set before the view becomes `MainViewContent`; `WattPilot` is declared `null!` and every binding starts
there.

```csharp
case WattPilot wattPilot:
    wattPilotView.ViewModel.WattPilot = wattPilot;
```

**Holding the device is safe here, unlike for the battery and the smart meter.** `UpdateService.OnWattPilotUpdate`
calls `existingDevice.Device.CopyFrom(wattPilot)`, so the same `WattPilot` instance is updated in place and the
bindings keep working. The Gen24 devices behave differently: `Gen24System.CopyFrom` assigns a fresh `Sensors`
object, which is why the battery view holds the inverter and the smart meter view holds nothing at all. Do not
copy this pattern to those views.

## Attach, detach and theme

`Loaded` subscribes and `Unloaded` unsubscribes `Application.Current.ActualThemeVariantChanged`. Mandatory pairing:
on a singleton view a missed unsubscribe re-adds the handler on every navigation. Subscribe in `Loaded`, never in
the constructor.

`OnThemeChanged` re-notifies the view model's `WattPilot` property, because the gauge background comes from the
`DeviceBackgroundColor` converter, which resolves the theme brushes when it runs rather than through
`DynamicResource`.

## Gauges

Uses the shared `DetailsGauge` template from `Styles/Gauges.axaml`. The template paints
`{TemplateBinding Background}`; this view sets the gauge `Background` in its `WrapPanel` style from the `WattPilot`
itself via `DeviceBackgroundColor` - present gets the running brush, absent the neutral one. That mirrors the
`NullToBrush` of the WPF template, which asked whether the WattPilot service had a device at all.

Two details carried over from the WPF view: the neutral conductor gauges (`N`) deliberately use a different unit,
range and color map than the phases in the same group, and the power group mixes `W` for `N` with `kW` for the
phases.

## Known gaps

- The current gauges fall back to 32 A per phase and 96 A in total when
  `MaximumChargingCurrentPossiblePerPhase` / `MaximumChargingCurrentPossible` are null. **These fallbacks are
  invented** - the WPF view had none. Replace them if the real device limits are known.
- `ColorAllTicks` forwards to `DashboardViewModel` and carries the same `//BUG:` note as the other detail view
  models; it should move to `MainViewModel` or to settings.
- The WPF view's menu (settings, reboot, charging log, config PDF) and its multi-part title are not ported.
- The view has no public parameterless constructor, so the build reports `AVLN3001` for it. Expected: the view is
  only ever resolved from the container. Do not add one.
