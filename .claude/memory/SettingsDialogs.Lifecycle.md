---
paths:
  - HomeAutomationClient/HomeAutomationClient/ViewModels/Dialogs/Gen24SettingsDialogViewModel.cs
  - HomeAutomationClient/HomeAutomationClient/ViewModels/Dialogs/Gen24ModbusViewModel.cs
  - HomeAutomationClient/HomeAutomationClient/ViewModels/Dialogs/Gen24EventLogViewModel.cs
  - HomeAutomationClient/HomeAutomationClient/Views/Dialogs/Gen24SettingsDialogView.axaml
  - HomeAutomationClient/HomeAutomationClient/Views/Dialogs/Gen24ModbusView.axaml
  - HomeAutomationClient/HomeAutomationClient/Views/Dialogs/Gen24EventLogView.axaml
  - HomeAutomationClient/HomeAutomationClient/Controls/CompactTextColumn.cs
  - HomeAutomationClient/HomeAutomationClient/Controls/Toast.axaml
  - HomeAutomationClient/HomeAutomationClient/Styles/CompactForms.axaml
  - HomeAutomationClient/HomeAutomationClient/App.axaml
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
out the delta with the `GetToken` / `GetUpdateToken` the models already carry, and posts only that. How
that JSON is read and written is [[DeviceJson]].

```
client  PUT api/Gen24System/{id}/settings/modbus   { typed Gen24ModbusSettings }
server  read api/config/ -> current settings
        delta = wanted.GetToken(current)
        post api/config/modbus <- delta only
        200 true  = written        200 false = inverter already held it
        IGen24ConfigRefresher.ReadConfigNow(id)
```

**A write that succeeded ends with `ReadConfigNow`.** `Gen24DataCollector` polls the configuration every five
minutes, and everything the clients know about an inverter comes from that poll. Without the call, a setting just
written would not reach any client until the next one, and a dialog reopened in the meantime would show what the
inverter held *before* the change. How that interrupts the poll is [[Gen24Polling]].

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

`GET {id}/events` is separate; the event log is not part of the snapshot, and not read when the dialog opens
either - see the event log tab below.

## Roles

Reading a setting needs `User`, writing one needs `Operator` - the role `requestStandBy` already asked for.

**`Roles` is a `[Flags]` enum with no hierarchy.** A user who holds only `Operator` can write settings and gets 403
on the read that has to happen first, so they cannot open the dialog they are allowed to change. Whether the reads
should accept `User,Operator` is still open; see the note at the end.

## It is a dense form, not a touch surface

`Gen24SettingsDialogView` sets `FontSize="12"` and includes `Styles/CompactForms.axaml`, and that is where the
density of every tab comes from - `FontSize` is inherited and the styles reach the whole subtree, so a tab added
later needs nothing. Fluent sizes its controls for a finger: a 32 pixel combo box, a 48 pixel tab header with 24
point type. The settings views of FroniusMonitor are forms of thirty or forty fields, and the three still to be
ported are the big ones. Measured on a Modbus shaped form, the two together take it from 702 pixels tall to 499.

- **`CompactForms.axaml` is included by the control that wants it, never in `App.axaml`.** The rest of the app is
  meant to keep the Fluent sizes.
- Fields are a `MinHeight` and a `Padding`, so one that needs more room than its text - a wrapping caption, a
  bigger font - still gets it. **Buttons are the exception and keep the 30 pixels WPF gives them**: they are the
  one thing in a dialog that is aimed at rather than read.
- **A button gets its height from its `Padding`, never from a `MinHeight`.** A `MinHeight` leaves the content
  presenter stretched over the spare room and the caption then draws at the top of it, which looks like a button
  whose text is not centred - and is. Measured at 12 point: `MinHeight="30"` with `Padding="10,2"` gives a 24 high
  text block 3 from the top; `Padding="10,7"` alone gives a 14 high one with 8 above and 8 below, and the same 30
  pixels. Padding also lets the button follow the type, so a bigger font or a caption that wraps still centres.
- **A check box goes in a `Viewbox Classes="CheckBox"`.** It cannot be told to be shorter - the Fluent template
  puts `Height="32"` on a grid inside itself, bound to nothing the control exposes, and a style setter loses to a
  value set in a template - so it is scaled instead, from 32 to 22. Two things follow, and both are easy to get
  wrong:
  - **`IsVisible` and `Margin` go on the `Viewbox`.** On the check box, `IsVisible` leaves the Viewbox holding the
    empty row, and a margin is scaled with everything else *and* changes the natural height the scale is worked
    out from.
  - **The check box inside gets `FontSize` 17.5**, which is 12 back again once scaled by 22/32. The two numbers in
    `CompactForms.axaml` belong together; change one and the caption stops matching the rest of the dialog.

  Clicking works on the box and on the caption after scaling - that was checked, not assumed.
- A tab's own margins are its own: 8 around the view, 8 inside a group box, 2 above and below a field. The numbers
  come from the WPF views.

## The shell and its tabs

`Gen24SettingsDialogViewModel` is the `DialogBase`. Each tab is a plain `ViewModelBase` that the shell creates from
the snapshot and exposes as a property, and the view puts one `UserControl` per tab into a `TabControl`.

Three things a tab has to do:

- **Proxy `BusyText` to the shell.** A tab has no busy indicator of its own; the dialog does, through
  `MainViewModel.DialogBusyText`. Overriding `BusyText` to forward to the shell also means the guard in
  `TaskExceptionHandler` clears the indicator the user is actually looking at rather than a property nothing binds.
- **Proxy `ToastText` to the shell** as well, for the same reason: the toast is in the button row of the dialog,
  next to Cancel, because that is the only place with room for a sentence. A tab that keeps its own would have to
  squeeze it into the gap between its switches and its buttons, where the text wraps to four lines and drags the
  row open - measured at 124 px against 334 px.
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

- **Nothing changed:** the tab compares locally, `Settings.GetToken(loadedSettings).HasValues()`, and where nothing
  differs it puts up a **message box** (`Loc.NoSettingsChanged`, `Loc.Warning`, `WarningIcon`) and sends nothing.
  This is why the client registers `IGen24JsonService`: the models reach for it through `IoC` when they build a
  token. The check has to come **after** any derived value - the Modbus `Mode` and `InverterAddress` follow from
  what the user enabled, and they are part of what is compared.
- **Busy while saving:** `string.Format(Loc.SavingSettings, <group>)` - "Saving Modbus settings". A format, because
  every tab wants its own name in it.
- **Saved:** `Loc.SettingsSavedToInverter` in a `Toast` (`Controls/Toast.axaml`), five seconds then a one second
  fade, in the button row of `Gen24SettingsDialogView` rather than in the tab. `Toast.Text` binds two way and the
  control clears it when it has faded, so the same message shown twice appears twice - which is also what clears
  the shell's `ToastText` again. A `DispatcherTimer` rather than the `async void` + `Task.Delay` of the WPF
  original. Its colours are `ToastBackground` and `ToastForeground` from both theme dictionaries, not literals -
  the app is used in light and dark.

The switch for the dangerous group is `ToggleButton Classes="OnOff"`, the switch of this app, not a `ToggleSwitch`.
`Classes="OnOff Labeled"` is the same switch with its `Content` as a caption in front of it. The plain check boxes
of the WPF dialog stay check boxes.

A group box in a dialog has to set `Background="{DynamicResource DialogBackground}"`. The header of
`HeaderedContentControl.GroupBox` covers the piece of border line behind its caption with that brush, and the
default is the surface of the app, which is the wrong colour inside a dialog.

## Validation

The mechanism is [[Validation.Lifecycle]] - rules are attributes on the edited property, the settings report what
is wrong with them through `INotifyDataErrorInfo`, and `Styles/Validation.axaml` paints the frame and the tooltip.
`DisableAvaloniaDataAnnotationValidation` in `App.axaml.cs` must stay commented out for any of it to arrive.

What a settings tab adds:

- **A number is edited as text.** The tab keeps a `string` property per numeric field, because a text box never
  binds to a number - the rule and the reasons are in [[Validation.Lifecycle]]. `CopyFromSettings` fills them,
  `CopyToSettings` writes them back at the top of Apply, once the rules have passed.
- **An empty address is not an error.** Neither Modbus address is required: an inverter that is not a Tauro has no
  string controllers, and one can report no meter address either. An empty field ends up as null, and
  `GetUpdateToken` leaves a null out of the delta altogether - so emptying a box never clears the value on the
  inverter, it only stops the dialog having an opinion about it. A field that really were required would need
  `AllowEmpty = false` **and** the `ValidateAllProperties()` that ends the reset; see [[Validation.Lifecycle]] for
  why the setter alone is not enough.
- **Apply asks the settings and the tab.** `Settings.GetErrors()` covers what the check boxes and combo boxes
  wrote, `GetErrors()` on the tab itself covers the text of the boxes; both validate through the same rules, and
  together they fill the `ItemList` of a `PleaseCorrectErrors` message box. Nothing is sent. No visual tree walk,
  nothing handed over from the code behind.
- **A refused field the user cannot see must not stop them saving.** `RestoreRefusedFieldsThatAreHidden` puts such
  a field back to what the dialog read from the inverter, using the same visibility properties the view binds to -
  `ShowAllowedIp` for the IP address, `ShowCommonSlaveSettings` for the two addresses. Add to it when a tab gains a
  field that can both be refused and be hidden. The WPF dialog reached the same end by walking the visual tree and
  reflecting over the binding to find the property behind the box.
- **Undo is `Reset`**: a fresh clone of the settings the dialog started from, and every text property re-filled
  from it. Unconditional, whatever the boxes hold. A successful Apply runs the same `Reset` after moving
  `loadedSettings` on, so Undo afterwards goes back to what was written - and a `0200` the user typed reads `200`
  once it has been saved.

## While the settings are still being read

The tabs are up from the start - the dialog is **not** gated on `IsLoaded`, or it would open as a bare title bar
with nothing for the busy animation of the frame to sit over. The `TabControl` carries a `MinHeight` for the same
reason. A tab whose content depends on the snapshot stays in place while it is read and is dropped afterwards only
where the inverter has nothing for it: that is what `ShowModbus` is, `!IsLoaded || Modbus is not null`. Gating such
a tab on the payload alone makes it appear a second or two after the dialog opened.

## The event log tab reads itself, when it is looked at

The event log is the one tab that does not come out of the snapshot. `Gen24EventLogViewModel` fetches it through
`GET {id}/events` the first time the tab is actually on screen - `TabItem.IsSelected` is two way bound to
`IsSelected`, and the read fires from its setter - and keeps it from then on. A log of a few hundred entries is its
own request to the inverter, and most visits to this dialog are to change a setting rather than to read it. The
fetch runs through `TaskExceptionHandler`, because nothing awaits it: an escaping exception would take the app
down.

**What an event code means is localized on the client, never by the server.** The description lives in a
translation file on the inverter, so the server would answer in whatever language it happens to run in. The client
has already downloaded those files in its own language, and `IGen24LocalizationService.GetEventDisplayName` is the
lookup; the tab writes the result into `Gen24Event.Message`, which is `[JsonIgnore]`d for exactly this reason.
FroniusMonitor fills the same property from its own `IGen24Service.GetEventDescription`. See [[DeviceJson]] for
what that property used to do and why `GET {id}/events` could never answer while it did it.

`Code` and `Description` are captioned by the inverter's own `EVENTLOG.*` strings through the `{l:Ui '...'}`
markup extension, the way FroniusMonitor captions them; the other four column headings are in the resx.

This is the only view in the client that uses `Avalonia.Controls.DataGrid`. It is a separate package, and its
theme is not part of `FluentTheme`, so `App.axaml` includes
`avares://Avalonia.Controls.DataGrid/Themes/Fluent.xaml` as well. Four things about it are worth keeping, all of
them measured in a headless probe rather than guessed:

- **No `ScrollViewer` around it.** It scrolls and virtualizes itself; wrapped in one it gets unlimited height and
  lays out every row it has. It shows its own scrollbars instead, `Auto` in both directions.
- **`FontSize` has to be said, not inherited.** The DataGrid theme sets it on `DataGridCell`, and a theme value
  beats what the dialog hands down: the cells came out at 15 while the headers, which do inherit, were at 12. The
  `DataGridCell` rule in `Styles/CompactForms.axaml` is where that is fixed, together with the row height - 18
  pixels against the 32 Fluent draws. The grid's density lives there and not in the view, because it is the
  density of the dialog and nothing about it is specific to the event log.
- **The padding of the cell is the whole inset, and `CompactTextColumn` is what makes that true.**
  `DataGridTextColumn` puts `Margin="12,0"` on the `TextBlock` it generates, *inside* the cell padding: measured,
  the text began 20 pixels in while the severity column, whose content the column does not generate, began at 8.
  A style cannot undo it - the margin is a local value on the instance and beats a setter, the same reason the
  check boxes are scaled in a `Viewbox` - so the column type clears it in `GenerateElement`. The severity icon is
  14 wide for the same reason: it is the height of a line of 12 point type, so a row with an icon is no taller
  than one without.
- **There is no `AlternatingRowBackground`** the way the WPF grid had. A `DataGridRow:nth-child(2n)` style does it,
  against a faint `AlternatingRowBackground` brush that is in both theme dictionaries.
- **Every column sizes to its content, and not one of them is a star.** `ColumnWidth="Auto"`, no `Width` on any
  column. A star column absorbs whatever width is left over, so the row always comes to exactly the width of the
  grid and the horizontal scrollbar can never appear however long the descriptions are - which is why the
  description column is not one. With all of them on `Auto` a wide log is wider than the dialog and scrolls, at
  `MaxWidth` (1024) as much as below it. The cost is a ragged right edge on a log of short entries, which is what
  the WPF grid did too.

## Never a BindableCollection on the server

`BindableCollection` demands a `SynchronizationContext` and throws `ThreadStateException` without one, and a
request thread has none. Anything the server parses returns plain lists - `Gen24ChargingRule.ParseList` next to the
`Parse` that the WPF app still needs. There is a test for it, because the difference is invisible until it runs off
a UI thread.

## Still open

- **Two tabs are not ported.** Self consumption and inverter settings carry their heading in the `TabControl`
  and say so; adding one is a matter of dropping its view in.
- **Three write endpoints are missing** for the same reason: `api/config/common`, `api/config/powerunit` and
  `api/config/limit_settings/powerLimits`. Their token building is entangled with state of the WPF inverter
  settings dialog - `needsConnectedInverterUpdate`, the `staticControlledDevices` and
  `autodetectedControlledDevices` lists built from `ConnectedInverters` - so they belong with that tab rather than
  being ported blind. Note that the WPF `Apply` declares `hasCommonUpdates` and never sets it true; check that
  before copying the logic across.
- **`ConverterCulture`** has no counterpart in Avalonia's `Binding`, and `ValidationBinding` of the WPF app sets
  it. It does not matter for the fields ported so far - whole numbers and an IP string - but it will for anything
  with a decimal separator.
- **The role lockout** above.
- The busy texts of this dialog are translated into every language the app has; `rm` and `gsw` are worth a native
  reading. See [[Localization]] for how the resource files are kept.
