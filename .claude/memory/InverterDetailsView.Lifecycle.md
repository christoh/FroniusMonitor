---
paths:
  - HomeAutomationClient/HomeAutomationClient/Views/InverterDetailsView.axaml
  - HomeAutomationClient/HomeAutomationClient/Views/InverterDetailsView.axaml.cs
  - HomeAutomationClient/HomeAutomationClient/ViewModels/InverterDetailsViewModel.cs
---

# Lifecycle contract: InverterDetailsView (Avalonia)

This is the Avalonia port of the WPF `FroniusMonitor/Views/InverterDetailsView.xaml`, which was a separate
`ScalableWindow`. Where it appears is the head's business and the view knows nothing about it: a page swapped into
the main content where there is one window - the browser, the phones - and since 2026-09-15 a window of its own per
inverter on the desktop, which is what the WPF app did. See [[DialogSystem.Lifecycle]] for who decides.

## Ownership and lifetimes

| Participant | Lifetime | Registered in |
| --- | --- | --- |
| `InverterDetailsView` | **Transient** | `App.axaml.cs` → `AddTransient<InverterDetailsView>()` |
| `InverterDetailsViewModel` | **Transient** | `App.axaml.cs` → `AddTransient<InverterDetailsViewModel>()` |
| `Gen24System` (the inverter) | Owned by `IUpdateService`, **not** by the view | assigned per navigation |

How many instances there are is the presenter's business, not the container's: `MainViewPresenter` builds one page
of this type and shows it again for every inverter, which is the single instance this view had until 2026-09-15;
`WindowPresenter` builds one per inverter and gives each a window. Either way a page is not disposed and is shown
again, so every rule below still holds.

## Construction (once per application run)

1. The container hands out the view; its parameterless constructor resolves the view model itself, as the code
   behind injection rule asks (`IoC.TryGetRegistered`, so the designer gets a view with no view model instead of
   an exception). Both are transient, so a page and its view model belong to each other.
2. `InitializeComponent()` runs, then `DataContext` is assigned.
3. `DataContext` is assigned exactly once and never changed. `ViewModel` is a cast of `DataContext` and relies on
   this; do not re-assign `DataContext` elsewhere.
The constructor does nothing else: the gauge group switches and the `ShowAll` logic live in the view model
(`Rules/ViewModelsForInteractionLogic.md`), so there is no wiring to do here.

The constructor is parameterless so that the XAML loader and the previewer can create the view (`AVLN3001`); the
view model comes from the container inside it. See [[HomeAutomationClient.CodeBehindInjection]].

## Navigation (once per shown inverter)

`MainViewModel.ShowDetails` is the only entry point:

```csharp
pagePresenter.Show<InverterDetailsView>(device.Key, title, view => view.ViewModel.Gen24System = gen24System);
```

The presenter runs that action **before** the page is shown, and runs it again on a page it is reusing. The device
key is what decides whether this inverter has a page already: on the desktop each one gets a window of its own, and
asking for the same inverter twice brings its window to the front.

Contract for callers:

- **`Gen24System` must be assigned before the view is shown,** which is what the presenter's action is for. The property is declared
  `Gen24System { get; set; } = null!` and every binding in the view starts at `Gen24System.…`; a null would throw
  during the first render pass, not at assignment.
- `Gen24System.Sensors`, `.Config` and everything below them **are** allowed to be null. All XAML paths use `?.`
  and the gauges display `---` for a null `Value`. Do not add non-null-safe paths.
- Where one page is reused - the browser and the phones - showing another inverter re-assigns `Gen24System` on the
  same view model, so **only one inverter is on screen at a time** and the 19 group switches keep their state across
  inverters. On the desktop each inverter has a page of its own, so each window keeps its own switches.

## Attach and detach (once per navigation)

`Loaded` subscribes and `Unloaded` unsubscribes `Application.Current.ActualThemeVariantChanged`.

**This pairing is mandatory, not stylistic.** A page is kept and shown again, so a subscription that is not removed
in `Unloaded` is re-added on every navigation and the handler runs N times per theme change, forever. Any future
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
- **`ColorAllTicks` comes from neither this view nor its view model.** The `WrapPanel.GaugeGroups` style in
  `Styles/DetailViews.axaml` sets it from the application resource `MainViewModel.ColorAllTicksResourceKey`, which
  the main view's switch writes. A resource and not a binding up the tree, because on the desktop this page stands
  in a window that has no `MainView` above it - see [[DialogSystem.Lifecycle]].
- **The cos(phi) gauges read the amount, not the value.** `DialShowsAbsoluteValue` (on `Gauge`, set in that
  group's style) puts the needle at `|cos phi|` while the read-out under it keeps the sign, asked for by the
  developer on 2026-09-19: the sign says which way the reactive power flows, the dial is about how good the power
  factor is, and a needle crossing the whole scale when the sign flips reports a change that did not happen. It is
  read in `Gauge.SetValue`, where the fraction of the scale is worked out, so both kinds of gauge get it from one
  place; `Gauge2Text`, which builds the read-out, never sees it. The group's scale went from -1 to 1 with
  `MidIsBad` to **0 to 1 with `LowIsBad`** in the same breath, and its `Origin` setter went with it: with the sign
  gone there is no lower half to show, and 1 is the good end of the dial rather than both ends being good. The
  dashboard's `InverterControl` and `SmartMeterControl` carry the same setters on their cos(phi) linear gauges,
  where the bar is the amount and the number printed beside it is the value; in `SmartMeterControl` the `Origin`
  of the base `ControlTheme` has to be set back to 0, because that theme is a bipolar power gauge. **The WPF app
  has all five of those places too, and they were changed with it** - [[Gauges]] holds what the two apps share.
- **The ΔFrequency gauge hides itself below 10 Hz** (`IsVisible` through `co:IsInRange Minimum=10` on
  `Sensors?.Inverter?.InverterFrequency`, asked for by the developer on 2026-09-19). An inverter that is off,
  starting up or in standby is not synchronized to the grid and reports next to no frequency, and the difference
  to the grid's 50 Hz is then tens of thousands of mHz - a reading that says nothing and pins the needle. The two
  frequency gauges beside it stay: that the inverter reports 0 Hz is worth seeing. A frequency that was never
  reported at all counts as not synchronized too - the call sites pass `Unknown=False`, against that converter's
  own default - because nothing then says the inverter *is* synchronized. The developer chose that on
  2026-09-19 over the other reading, which was to keep a difference until one is proved meaningless.
  The WPF original does the same since 2026-09-19, through `co:Range2Visibility` on the `ContentControl` around
  that gauge - see [[Gauges]].
  The rule sits in a converter and not in the view model on purpose: the live readings reach the page through
  `Gen24System.Sensors.…` bindings, and the view model is handed the `Gen24System` once per navigation and hears
  nothing afterwards, so a view model property would need `PropertyChanged` subscriptions on two model levels -
  exactly what "Attach and detach" above warns against. The same pattern is used for value driven visibility on the
  dashboard (`IsVisible="{Binding Device.Sensors.Storage, Converter={co:Null2Bool}}"`).
- **The two Δ voltage groups go the same way, whole** (added 2026-09-19, same limit, same converter): every gauge
  in `ΔAcPhaseVoltageFeedIn` and `ΔAcLineVoltageFeedIn` is a difference between the inverter and the grid, so
  below 10 Hz, and with no frequency reported at all, there is nothing left in them worth a frame. They therefore
  have **two** reasons to be away - the user's switch and the reading - and their `IsVisible` is a `MultiBinding`
  over both through `co:AllTrue`. That converter counts anything that is not `false` as true, so a binding which
  has not produced its first value cannot take a group away; the frequency is a `false` of its own through
  `IsInRange`'s `Unknown=False`, which is a decision about the reading and not about the binding. The ΔFrequency gauge is gated alone rather than by its group, because the two
  gauges beside it in that group are readings and not differences.
  **This is the Avalonia client only.** The WPF window keeps both groups on screen whatever the frequency, and
  that is the developer's decision of 2026-09-19, not an oversight: its menu has a switch per group and the user
  turns them off there. Do not carry the rule over - the ΔFrequency gauge is the only one of the three that the
  WPF app hides by itself, because it has no switch of its own to be turned off with.

## Known gaps

- The WPF view's `CheckAtLeastOneView` hint bound to `IsNoneSelected`, and the `Inverter` menu
  (Settings / EnergyFlow / Modbus / EventLog) are not ported yet.
