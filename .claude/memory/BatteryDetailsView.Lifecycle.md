---
paths:
  - HomeAutomationClient/HomeAutomationClient/Views/BatteryDetailsView.axaml
  - HomeAutomationClient/HomeAutomationClient/Views/BatteryDetailsView.axaml.cs
  - HomeAutomationClient/HomeAutomationClient/ViewModels/BatteryDetailsViewModel.cs
---

# Lifecycle contract: BatteryDetailsView (Avalonia)

Port of the WPF `FroniusMonitor/Views/BatteryDetailsView.xaml`. 7 gauge groups, 15 gauges, no group switches.

## Ownership and lifetimes

View and view model are **singletons** (`App.axaml.cs`), like the other detail views: one instance for the whole
application run, attached to and detached from the visual tree on every navigation. Everything below follows.

## The view model holds the inverter, not the battery

`BatteryDetailsViewModel.Gen24System` is a `Gen24System`, although the view is about its `Sensors.Storage`. That is
deliberate: the two **Net** gauges (net state of charge, net capacity) read `NetStateOfChange`,
`StorageNetCapacity` and `MaxStorageNetCapacity`, which live on `Gen24System`, not on `Gen24Storage`. The WPF
version took them from its `HomeAutomationSystem`, which has no counterpart in the client.

**Never cache the `Gen24Storage` in the view model.** `Gen24System.CopyFrom` assigns a fresh `Sensors` object on
every update, so a stored battery reference would freeze after the first refresh. All bindings therefore walk
`Gen24System.Sensors?.Storage?.…` from the stable `Gen24System` and re-resolve on each update.

## Navigation

`MainViewModel.ShowDetails`, `case Gen24Storage`. The clicked menu entry carries a `Gen24Storage` that is already
stale (see above), so the inverter **must not** be looked up by comparing against it — an earlier version matched
with `ReferenceEquals(inverter.Device.Sensors?.Storage, storage)` and never fired. It resolves
`UpdateService.BatteryGen24System` instead, which the update service keeps current:

```csharp
case Gen24Storage when UpdateService.BatteryGen24System is { } batteryInverter:
    batteryView.ViewModel.Gen24System = batteryInverter;
```

`Gen24System` must be assigned before the view becomes `MainViewContent`; it is declared `null!` and every binding
starts there. With more than one battery in the system this always shows the one the update service tracks.

## Attach, detach and theme

`Loaded` subscribes and `Unloaded` unsubscribes `Application.Current.ActualThemeVariantChanged`. Mandatory pairing:
on a singleton view a missed unsubscribe re-adds the handler on every navigation. Subscribe in `Loaded`, never in
the constructor.

`OnThemeChanged` re-notifies `Gen24Storage.IsAwake` because the gauge background comes from the
`DeviceBackgroundColor` converter, which resolves the theme brushes when it runs rather than through
`DynamicResource`. If the converter ever switches to `DynamicResource`, the handler and its subscriptions are dead
code.

## Gauges

Uses the shared `DetailsGauge` template from `Styles/Gauges.axaml`. The template paints
`{TemplateBinding Background}`, and this view sets the gauge `Background` in its `WrapPanel` style from
`Sensors?.Storage?.IsAwake` via `DeviceBackgroundColor` — awake gets the running brush, asleep the neutral one.
That setter is the only part that differs from the other three detail views.

## Known gaps

- `ColorAllTicks` forwards to `DashboardViewModel` and carries the same `//BUG:` note as the other detail view
  models; it should move to `MainViewModel` or to settings.
- The view has no public parameterless constructor, so the build reports `AVLN3001` for it. Expected: the view is
  only ever resolved from the container. Do not add one.
