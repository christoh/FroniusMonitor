---
paths:
  - HomeAutomationClient/HomeAutomationClient/Views/InverterDetailsView.axaml
  - HomeAutomationClient/HomeAutomationClient/Views/InverterDetailsView.axaml.cs
  - HomeAutomationClient/HomeAutomationClient/ViewModels/InverterDetailsViewModel.cs
---

# Lifecycle contract: InverterDetailsView (Avalonia)

This is the Avalonia port of the WPF `FroniusMonitor/Views/InverterDetailsView.xaml`, which was a separate
`ScalableWindow`. The new architecture is single-window (see `Rules/PortingFroniusMonitor.md`), so the view is a
page swapped into the main content instead.

## Ownership and lifetimes

| Participant | Lifetime | Registered in |
| --- | --- | --- |
| `InverterDetailsView` | **Singleton** | `App.axaml.cs` → `AddSingleton<InverterDetailsView>()` |
| `InverterDetailsViewModel` | **Singleton** | `App.axaml.cs` → `AddSingleton<InverterDetailsViewModel>()` |
| `Gen24System` (the inverter) | Owned by `IUpdateService`, **not** by the view | assigned per navigation |

One view instance and one view model instance exist for the whole application run. The view is never disposed and
never re-created; it is attached to and detached from the visual tree repeatedly. Every rule below follows from that.

## Construction (once per application run)

1. The IoC container calls `InverterDetailsView(InverterDetailsViewModel viewModel)`.
2. `InitializeComponent()` runs, then `DataContext = viewModel`.
3. `DataContext` is assigned exactly once and never changed. `ViewModel` is a cast of `DataContext` and relies on
   this; do not re-assign `DataContext` elsewhere.
The constructor does nothing else: the gauge group switches and the `ShowAll` logic live in the view model
(`Rules/ViewModelsForInteractionLogic.md`), so there is no wiring to do here.

There is no public parameterless constructor, so the runtime XAML loader cannot instantiate this view — the build
emits `AVLN3001` for it. That is expected: the view must always come from the container. Do not "fix" the warning by
adding a parameterless constructor, because that would create an instance with no view model.

## Navigation (once per shown inverter)

`MainViewModel.ShowDetails` is the only entry point:

```csharp
var detailsView = IoC.Get<InverterDetailsView>();
detailsView.ViewModel.Gen24System = gen24System;   // must happen BEFORE the view is shown
MainViewContent = detailsView;
```

Contract for callers:

- **`Gen24System` must be assigned before the view becomes `MainViewContent`.** The property is declared
  `Gen24System { get; set; } = null!` and every binding in the view starts at `Gen24System.…`; a null would throw
  during the first render pass, not at assignment.
- `Gen24System.Sensors`, `.Config` and everything below them **are** allowed to be null. All XAML paths use `?.`
  and the gauges display `---` for a null `Value`. Do not add non-null-safe paths.
- Showing a different inverter means re-assigning `Gen24System` on the same view model instance. Because both view
  and view model are singletons, **only one inverter can be displayed at a time**, and the 19 group switches keep
  their state across inverters (they are plain XAML `ToggleButton`s, not view-model state).

## Attach and detach (once per navigation)

`Loaded` subscribes and `Unloaded` unsubscribes `Application.Current.ActualThemeVariantChanged`.

**This pairing is mandatory, not stylistic.** The view is a singleton, so a subscription that is not removed in
`Unloaded` is re-added on every navigation and the handler runs N times per theme change, forever. Any future
event subscription in this view must follow the same pattern: subscribe in `Loaded`, unsubscribe in `Unloaded`,
never in the constructor.

Nothing else is attached or released, and the view holds no unmanaged or disposable state.

## Theme changes

`OnThemeChanged` re-raises `PropertyChanged` for `Gen24Status.StatusCode`:

```csharp
ViewModel.Gen24System.Sensors?.InverterStatus?.NotifyOfPropertyChange(nameof(Gen24Status.StatusCode));
```

This exists because the gauge background comes from the `InverterBackgroundColor` multi-converter, which resolves
theme brushes **imperatively at convert time** (`Application.Current.GetSolidColorBrush(...)`) rather than through
`DynamicResource`. Without the nudge the gauges keep the brushes of the previous theme. Consequences:

- If `Sensors` or `InverterStatus` is null when the theme changes, nothing repaints until the next status update.
  Acceptable — there is no inverter data to colour yet.
- If `InverterBackgroundColor` is ever changed to use `DynamicResource`, this handler and its `Loaded`/`Unloaded`
  subscriptions become dead code and should be removed.

## Threading

Per `Rules/PortingFroniusMonitor.md` the client also runs on WebAssembly, where `async`/`await` is cooperative
multitasking. Therefore:

- Property change notifications for `Gen24System` and everything below it **must reach the view on the UI thread**.
  Marshalling is the publisher's job (see `DashboardView`, which hops with `Dispatcher.UIThread.InvokeAsync` before
  touching its view model); this view does no marshalling of its own.
- Never block in this view or its view model: no `Thread.Sleep`, no `Task.Wait()`, no `.Result`.

## View structure invariants

- **One switch per group, and one view model property per group.** Each `HeaderedContentControl Classes="GroupBox"`
  binds `IsVisible="{Binding <Group>}"` and its switch binds `IsChecked="{Binding <Group>, Mode=TwoWay}"`. Adding a
  gauge group therefore means three things: an `[ObservableProperty]` **carrying
  `[NotifyPropertyChangedFor(nameof(ShowAll))]`**, an entry in the view model's `GroupSwitches` array (otherwise
  `ShowAll` and the reset ignore the new group), and the switch plus group box in the XAML. The three Δ groups use
  `Delta…` as the property name while the caption still comes from the `Δ…` resource.
- **`ShowAll` is calculated, not stored** — its getter is "every group is on", so it can never disagree with the
  groups, and there is no default of its own to keep in step. Its setter writes all groups inside
  `IsNotifying = false` … `Refresh(true)` (`BindableBase`), so the batch raises one notification instead of 19 and
  cannot feed back into itself. Note the `true`: `Refresh()` alone would restore the *previous* `IsNotifying`, which
  is `false` inside the batch, and would leave the view model silent for good. The master has no indeterminate
  state, because the `OnOff` switch template has no visual for one — a partial selection shows it as off, which is
  how the WPF menu behaved too.
- **`ResetToDefaultCommand`** puts the groups back to the values declared in their property initializers, batched
  the same way. The view model's constructor snapshots those values (it runs after all property initializers), so
  the defaults exist only once — do not repeat them in the reset.
- **`DetailsGauge` control template** lives in `Styles/Gauges.axaml` and is shared by all four detail views
  (inverter, battery, smart meter, WattPilot). The dial is the gauge's `Content`, so the template must keep a
  `ContentPresenter` named `PART_ContentPresenter`; the value line is produced by the `Gauge2Text` multi-converter
  from `(gauge, Value, ValueStringFormat, UnitName)`. The template paints `{TemplateBinding Background}`, and each
  view decides the running state by setting the gauge's `Background` in its own `WrapPanel` style — that setter is
  the only part that differs between the four views, so do not paint the border in the template.
- **`UseRunningBackground`** is an attached property owned by this view and read by `InverterBackgroundColor` as
  its third value. It only has meaning inside this view.
- **Format split:** `Gauge.ValueStringFormat` formats the value read-out, `Gauge.StringFormat` the minimum/maximum
  labels. The WPF original used `StringFormat` plus the attached `MinimumMaximumStringFormat`.

## Known gaps

- `InverterDetailsViewModel.ColorAllTicks` forwards to `DashboardViewModel` and carries a `//BUG:` note — it should
  move to `MainViewModel` or to settings. Until then this view depends on `DashboardViewModel` being resolvable.
- The WPF view's `CheckAtLeastOneView` hint bound to `IsNoneSelected`, and the `Inverter` menu
  (Settings / EnergyFlow / Modbus / EventLog) are not ported yet.
