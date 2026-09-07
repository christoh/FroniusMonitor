---
paths:
  - HomeAutomationClient/HomeAutomationClient/ViewModels/Dialogs/Gen24SettingsDialogViewModel.cs
  - HomeAutomationClient/HomeAutomationClient/ViewModels/Dialogs/Gen24ModbusViewModel.cs
  - HomeAutomationClient/HomeAutomationClient/Views/Dialogs/Gen24SettingsDialogView.axaml
  - HomeAutomationClient/HomeAutomationClient/Views/Dialogs/Gen24ModbusView.axaml
  - HomeAutomationClient/HomeAutomationClient/Controls/Toast.axaml
  - HomeAutomationServer/Controllers/Gen24Controller.cs
  - Fronius/Models/Gen24/Settings/**
  - Fronius/Contracts/HomeAutomationClient/IWebClientService.cs
---

# Lifecycle contract: the settings dialogs

How a device's settings are read, edited and written. The dialog machinery itself - the queue, nesting, modality,
`DialogBase` - is [[DialogSystem.Lifecycle]]; this is only what settings add on top. The direction rules of the
hub are [[SignalR.MessageDirection]].

These dialogs are ports of the settings views of `FroniusMonitor` (WPF). That app talks to the inverter directly;
here the inverter is only reachable from the server, which changes where the work happens.

## Settings never go over the hub

The hub carries the live device stream, server to client, and a client may not send over it at all. A setting is a
request that wants an answer, so it goes over https through `IWebClientService`. Nothing about settings belongs in
`UpdateService` or in a hub method.

## The server does the read-modify-write

The client PUTs the **whole typed settings object**. The server then reads `api/config/` from the inverter, works
out the delta with the `GetToken` / `GetUpdateToken` the models already carry, and posts only that.

```
client  PUT api/Gen24System/{id}/settings/modbus   { typed Gen24ModbusSettings }
server  read api/config/ -> current settings
        delta = wanted.GetToken(current)
        post api/config/modbus <- delta only
        200 true  = written        200 false = inverter already held it
```

Why this way round:

- The delta is against what the inverter **holds right now**, not against what a client saw when its dialog
  opened, so a dialog left open for ten minutes cannot quietly undo somebody else's change.
- No endpoint takes a config path plus a payload of the caller's choosing. Raw JSON never crosses the API.
- `WriteSettings<T>` in `Gen24SystemController` is the one place that does this. A new settings group is a call to
  it with a parse function and a delta function, not a new copy of the procedure.

## Reading: one snapshot for the whole dialog

`GET {id}/settings` returns a `Gen24SettingsSnapshot` - inverter settings (with `Mppt`, `PowerLimitSettings` and
`AcSystemSettings` inside), battery settings, charging rules, Modbus settings, firmware versions, nominal AC power.
The dialog reads it **once** and hands it to the tabs, so the inverter is asked once rather than four times and all
tabs show the same moment in time. It going stale costs nothing, because of the read-modify-write above.

`GET {id}/events` is separate; the event log is not part of the snapshot.

## Roles

Reading a setting needs `User`, writing one needs `Operator` - the role `requestStandBy` already asked for.

**`Roles` is a `[Flags]` enum with no hierarchy.** A user who holds only `Operator` can write settings and gets 403
on the read that has to happen first, so they cannot open the dialog they are allowed to change. Whether the reads
should accept `User,Operator` is still open; see the note at the end.

## The shell and its tabs

`Gen24SettingsDialogViewModel` is the `DialogBase`. Each tab is a plain `ViewModelBase` that the shell creates from
the snapshot and exposes as a property, and the view puts one `UserControl` per tab into a `TabControl`.

Two things a tab has to do:

- **Proxy `BusyText` to the shell.** A tab has no busy indicator of its own; the dialog does, through
  `MainViewModel.DialogBusyText`. Overriding `BusyText` to forward to the shell also means the guard in
  `TaskExceptionHandler` clears the indicator the user is actually looking at rather than a property nothing binds.
- **Own its visibility rules.** Every `MultiBinding` to `Visibility` in the WPF views becomes a bindable property
  here - `IsRtuSlave`, `ShowAllowControl`, `ShowRestrictControl`, `ShowAllowedIp`, `ShowCommonSlaveSettings`.
  Avalonia has no `Visibility`, and [[ViewModelsForInteractionLogic]] wants the rule in the view model anyway.
  Those rules read properties of the settings model, so the tab subscribes to its `PropertyChanged` and re-raises.

## Initialize runs once

The body's `OnDataContextChanged` starts `Initialize`, and **it fires again every time the dialog is re-attached** -
which happens whenever a message box has opened and closed over it, an error from a tab for instance. A settings
dialog stays open across that, unlike the login and message boxes that came before it, so `Initialize` guards
itself with a flag set before its first `await`. Without the guard the inverter is read again and the busy overlay
comes back up over a dialog the user is working in.

## The title comes from the caller

`DialogQueueItem` copies `Parameters.Title` at show time, so a title worked out after the snapshot arrives never
reaches the screen. `MainViewModel.Settings` builds it from the device's `DisplayName`, which is known at click
time. Do not try to refine it from `Initialize`.

## What the user sees

Ported from the WPF dialogs deliberately, so the two apps behave alike:

- **Nothing changed:** the tab compares locally, `Settings.GetToken(loadedSettings).HasValues`, and where nothing
  differs it puts up a **message box** (`Loc.NoSettingsChanged`, `Loc.Warning`, `WarningIcon`) and sends nothing.
  This is why the client registers `IGen24JsonService`: the models reach for it through `IoC` when they build a
  token. The check has to come **after** any derived value - the Modbus `Mode` and `InverterAddress` follow from
  what the user enabled, and they are part of what is compared.
- **Busy while saving:** `string.Format(Loc.SavingSettings, <group>)` - "Saving Modbus settings". A format, because
  every tab wants its own name in it.
- **Saved:** `Loc.SettingsSavedToInverter` in a `Toast` (`Controls/Toast.axaml`), five seconds then a one second
  fade. `Toast.Text` binds two way and the control clears it when it has faded, so the same message shown twice
  appears twice. A `DispatcherTimer` rather than the `async void` + `Task.Delay` of the WPF original. Its colours
  are `ToastBackground` and `ToastForeground` from both theme dictionaries, not literals - the app is used in
  light and dark.

The switch for the dangerous group is `ToggleButton Classes="OnOff"`, the switch of this app, not a `ToggleSwitch`.
`Classes="OnOff Labeled"` is the same switch with its `Content` as a caption in front of it. The plain check boxes
of the WPF dialog stay check boxes.

A group box in a dialog has to set `Background="{DynamicResource DialogBackground}"`. The header of
`HeaderedContentControl.GroupBox` covers the piece of border line behind its caption with that brush, and the
default is the surface of the app, which is the wrong colour inside a dialog.

## Validation

The settings models validate by **throwing from their setters** - `MeterAddress` outside 1 to 247,
`SunSpecAddress` of 0, an `IpAddress` that is not IPv4 with an optional mask. Avalonia's
`ExceptionValidationPlugin` turns that into a validation error on the binding, which is why
`DisableAvaloniaDataAnnotationValidation` in `App.axaml.cs` must stay commented out.

Three things are needed to make that work, and each of them was silently missing at first:

- **`UpdateSourceTrigger=PropertyChanged` on every validated field.** Avalonia writes `TextBox.Text` back on
  `LostFocus` by default, so without it the setter is never called while the user types and nothing appears to
  validate at all. This is what the `ValidationBinding` of the WPF app did, along with `ValidatesOnExceptions`,
  which Avalonia does not need because the plugin is unconditional.
- **A `DataValidationErrors` `ControlTheme` that renders only its content.** The default writes the message beside
  the field and reflows the group on every keystroke. Setting `DataValidationErrors.ErrorTemplate` does *not* do
  this: the template that draws the message belongs to the control, not to the attached property.
- **Colours of their own for focus and error.** Fluent paints a focused border with the accent brush, and the
  accent of this app is a red-orange, so a field being edited read as a field that had been refused.
  `TextBoxFocusedBorder` and `ValidationErrorBrush` live in both theme dictionaries and `Styles/TextBoxes.axaml`
  applies them. **The `:error` rule comes after the focus rules on purpose** - a value is refused while it is being
  typed, so the field is focused and in error at once, and equal-specificity styles apply in order.

**The model is not what is invalid.** A throwing setter never wrote, so the model still holds the value the dialog
read and looks perfectly valid at Apply. Only the controls know, so the view collects them: the code behind walks
its own `TextBox`es, keeps the `IsEffectivelyVisible` ones, and hands the view model plain strings through
`Gen24ModbusViewModel.GetInvalidFields`, the way `DashboardView` hands plain colors over. `Apply` puts them up as
the `ItemList` of a `PleaseCorrectErrors` message box and sends nothing.

Invisible fields are left out deliberately. Their setter threw, so the model holds the value it was read with, and
an error the user cannot see must not stop them saving. The WPF dialog reached the same end by writing the old
value back by hand.

## While the settings are still being read

The tabs are up from the start - the dialog is **not** gated on `IsLoaded`, or it would open as a bare title bar
with nothing for the busy animation of the frame to sit over. The `TabControl` carries a `MinHeight` for the same
reason. A tab whose content depends on the snapshot stays in place while it is read and is dropped afterwards only
where the inverter has nothing for it: that is what `ShowModbus` is, `!IsLoaded || Modbus is not null`. Gating such
a tab on the payload alone makes it appear a second or two after the dialog opened.

## Never a BindableCollection on the server

`BindableCollection` demands a `SynchronizationContext` and throws `ThreadStateException` without one, and a
request thread has none. Anything the server parses returns plain lists - `Gen24ChargingRule.ParseList` next to the
`Parse` that the WPF app still needs. There is a test for it, because the difference is invisible until it runs off
a UI thread.

## Still open

- **Three tabs are not ported.** Self consumption, inverter settings and the event log carry their heading in the
  `TabControl` and say so; adding one is a matter of dropping its view in.
- **Three write endpoints are missing** for the same reason: `api/config/common`, `api/config/powerunit` and
  `api/config/limit_settings/powerLimits`. Their token building is entangled with state of the WPF inverter
  settings dialog - `needsConnectedInverterUpdate`, the `staticControlledDevices` and
  `autodetectedControlledDevices` lists built from `ConnectedInverters` - so they belong with that tab rather than
  being ported blind. Note that the WPF `Apply` declares `hasCommonUpdates` and never sets it true; check that
  before copying the logic across.
- **A value that does not even convert** is reported by Avalonia, not by us: `MeterAddress` and `SunSpecAddress`
  are `byte?`, so 300 fails the conversion rather than the setter and the message is Avalonia's wording instead of
  `MeterAddressError`. `ValidationBinding` also set `ConverterCulture`, which Avalonia's `Binding` has no
  counterpart for; it does not matter for these fields, which hold plain integers and an IP string, but it would
  for anything with a decimal separator.
- **A bad request body** still becomes a plain 400 on the server. The throwing setters run during model binding
  there too, and nothing turns that into a useful `ProblemDetails`.
- **The role lockout** above.
- New resource strings (`ReadingInverterSettings`, `SavingSettings`) exist in the neutral and German resx only;
  `fr`, `it`, `rm` and `gsw` fall back to English until somebody translates them.
