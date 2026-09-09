---
paths:
  - HomeAutomationClient/HomeAutomationClient/ViewModels/Dialogs/Gen24SettingsDialogViewModel.cs
  - HomeAutomationClient/HomeAutomationClient/ViewModels/Dialogs/Gen24ModbusViewModel.cs
  - HomeAutomationClient/HomeAutomationClient/ViewModels/Dialogs/Gen24EventLogViewModel.cs
  - HomeAutomationClient/HomeAutomationClient/ViewModels/Dialogs/Gen24SelfConsumptionViewModel.cs
  - HomeAutomationClient/HomeAutomationClient/ViewModels/Dialogs/Gen24ChargingRuleViewModel.cs
  - HomeAutomationClient/HomeAutomationClient/ViewModels/Dialogs/Gen24InverterSettingsViewModel.cs
  - HomeAutomationClient/HomeAutomationClient/ViewModels/Dialogs/Gen24MpptViewModel.cs
  - HomeAutomationClient/HomeAutomationClient/Contracts/ITabHost.cs
  - HomeAutomationClient/HomeAutomationClient/Views/Dialogs/Gen24SettingsDialogView.axaml
  - HomeAutomationClient/HomeAutomationClient/Views/Dialogs/Gen24ModbusView.axaml
  - HomeAutomationClient/HomeAutomationClient/Views/Dialogs/Gen24EventLogView.axaml
  - HomeAutomationClient/HomeAutomationClient/Views/Dialogs/Gen24SelfConsumptionView.axaml
  - HomeAutomationClient/HomeAutomationClient/Views/Dialogs/Gen24InverterSettingsView.axaml
  - HomeAutomationClient/HomeAutomationClient/Views/Dialogs/Gen24MpptView.axaml
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

**The six write endpoints, and the path each of them writes:**

| endpoint | body | inverter path | delta |
|---|---|---|---|
| `settings/modbus` | `Gen24ModbusSettings` | `api/config/modbus` | `wanted.GetToken(current)` |
| `settings/batteries` | `Gen24BatterySettings` | `api/config/batteries` | `GetUpdateToken` |
| `settings/timeOfUse` | `List<Gen24ChargingRule>` | `api/config/timeofuse` | the whole list, or nothing |
| `settings/common` | `Gen24InverterSettings` | `api/config/common` | `GetUpdateToken` |
| `settings/mppt` | `Gen24Mppt` | `api/config/powerunit` | `wanted.GetToken(current)` |
| `settings/powerLimits` | `Gen24PowerLimitSettings` | `api/config/limit_settings/powerLimits` | `wanted.GetToken(current)` |

The last three are what the inverter settings tab writes, and it is one tab writing three endpoints because the
inverter keeps those three groups in three places. Two things about them are worth knowing:

- **`settings/common` takes the whole `Gen24InverterSettings` and writes three fields of it.** The trackers and
  the power limits hang off properties that carry no `FroniusProprietaryImport`, so `GetUpdateToken` does not
  look at them at all - measured: a delta with everything changed has exactly `systemName`, `timezone` and
  `timesync` in it. It takes the whole object rather than three arguments because that is what the client has.
- **`Gen24Mppt.GetToken` and `Gen24PowerLimitSettings.GetToken` build nested deltas** and leave out any object
  where nothing changed, which is what keeps an unchanged page from writing anything: a tree of empty objects
  would pass `HasValues` and ask the inverter to change nothing. A tracker the inverter does not report is left
  out rather than written from our defaults. The one thing in there that is not a delta is
  `displayModeSoftLimit`, which says whether the soft limit means watts or a percentage - it travels with the
  soft limit whenever the soft limit travels, because a number read in the wrong unit is not a small mistake.

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
point type. The settings views of FroniusMonitor are forms of thirty or forty fields, and the two biggest of them
- the energy flow and the inverter settings - are ported. Measured on a Modbus shaped form, the two together take
it from 702 pixels tall to 499.

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
- **The bar under the selected tab is positioned by the tab's height and vertical padding, and by nothing else.**
  `PART_SelectedPipe` is bottom anchored inside the template with its own margin and height, so a style setter
  cannot move it - and it sits *inside* the padding, so bottom padding lifts the bar twice as far as it lifts the
  caption and makes an overlap worse rather than better. That was tried, and measured going backwards. What holds
  is `clearance = (height - vertical padding - line height) / 2 - 4`: at 16 point the line height is 18, so the
  original 28 pixels with a padding of 2 gave **-1** - the caption's line box ran to 23 and the bar began at 22,
  which is what cut the descenders of "Energy flow" - and 30 pixels with no vertical padding gives 2.
- **On a `Slider`, write `Minimum` and `Maximum` before `Value`.** A slider clips whatever it is given to the
  range it has at that moment, and the range it comes with is 0 to 100 - so with `Value` first, the number is
  clipped before the real range arrives and the two way binding then writes the clipped number back over the
  real one. It is silent, and it looks like the settings were read wrongly. Measured on the inverter settings
  tab: a peak power of 19760 W and a soft limit of 19760 W both reached the view model again as **100**, and a
  fixed tracker voltage of 600 V as 100. With the range first, all three survive. This is not only about bound
  ranges - a literal `Maximum="5000"` written after `Value` is set after it too - so the order is the rule for
  every slider, and no slider in these views has `Value` as its first attribute any more.
- **Cancel lines up with Apply, and does not move from tab to tab.** Apply belongs to a tab and Cancel to the
  dialog, so the button row of the dialog has to repeat whatever insets the content of a tab. Two things do: the
  padding the tab control puts around what is in it, which the row takes from the tab control itself
  (`Padding="{Binding #SettingsTabs.Padding}"`) rather than writing 12 out a second time, and the margin of 8 a
  tab view carries, which is the 8 the row already had. Measured at 700 wide: Apply and Cancel both end 20.0 from
  the right on the energy flow, inverter settings and Modbus tabs, and Cancel stays at 20.0 on the event log,
  which has no Apply. The scroll bar of a full tab does not come into it - `AllowAutoHide` floats it
  over the content instead of giving it room, so Apply ends 8.0 from the right of its scroller at one rule and at
  forty alike.

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

## The energy flow tab writes two things, separately

`Gen24SelfConsumptionViewModel` is the port of `SelfConsumptionOptimizationViewModel`. Two things go from it to
the inverter, and they go one at a time because the inverter takes them that way: the battery settings as a delta
(`PUT {id}/settings/batteries`) and the time of use rules as a whole list (`PUT {id}/settings/timeOfUse`). Either
can be the only one that changed. The settings go first - they are a delta, so a failure there leaves the inverter
as it was, and stopping before the rules keeps the two from being half applied in the other order.

**Every box is paired with a slider, and that pair is the whole difficulty of this tab.** The box is a `string`
with the rule on it, because the user types into it; the slider is a `double`, because a slider cannot produce a
value out of range. They are two views of one number, so:

- One `Guard` stops them chasing each other. Whichever is written first does the writing, and the change it
  causes in the other is ignored.
- Text a rule refuses moves nothing. A half typed number is not a position, and a slider jumping about while a
  box is being filled in would be worse than one that waits.
- **A load writes both halves explicitly** rather than leaving it to the change handlers. That was the bug the
  tests found first: `CopyFromSettings` set the sliders, the handlers saw the guard closed, and every box came up
  empty - so the whole tab reported "must not be empty" the moment it opened.
- The state of charge pair may not cross, so moving one takes the other along; and because that happens inside
  the guard, the other one's box has to be written by hand there.

Two sign conventions come from the inverter and are worth stating once: it keeps **one signed number** for grid
power (negative means feeding in, which the tab shows as a switch and a positive number), and it keeps the
charging power from the house as a **negative** number, because to the battery that is power flowing the other
way. `CopyToSettings` is where both are put back.

The charging sources are one combo box standing for two flags, because only three of their four combinations mean
anything - the grid without the house is not something the inverter offers. A battery that may be charged from
the grid is charged from the house as well, and the inverter does not always say so, so that is fixed up on the
way in.

The tab is **two columns with the schedule across the foot of them**, which is how the WPF window is laid
out: the groups are short and wide, and stacking all six made the dialog a tall thin ribbon. Measured against
the WPF window at 1050 by 1000, this comes to 1060 by 1000 with the same groups in the same places.

Three things about the density of this tab were measured rather than guessed, and two of them are in
`Styles/CompactForms.axaml` because they are true of any dense form:

- **A text box does not centre its text**, it gives it a line box of its own inside the padding: with a padding
  of `6,1` the number in the box sat 2 pixels above the caption beside it. `Padding="6,0"` with
  `VerticalContentAlignment="Center"` puts them on the same line - measured at exactly the same offset.
- **A radio button cannot be told to be shorter**, for the same reason as a check box: the Fluent template puts a
  fixed height on a grid inside itself, and a value set in a template beats a style setter. Measured at 32
  against the 22 of a check box, so it is scaled in a `Viewbox` with the same numbers - which is what makes the
  two match.
- **A slider is sized by resources, not by styles or a Viewbox.** A Fluent slider comes out 50 pixels tall with a
  20 pixel disc for a thumb, and neither figure takes styling, because both are set inside the template where a
  local value beats a setter. A `Viewbox` is no help either, though it is what saves the check boxes: it measures
  its child unconstrained, and a slider asked for its natural width answers 20 pixels, which the box would then
  stretch twentyfold. What the template does read is a handful of resources, and those can simply be given
  different values - `SliderHorizontalHeight`, `SliderHorizontalThumbWidth`, `SliderHorizontalThumbHeight`,
  `SliderTrackThemeHeight`, `SliderPreContentMargin` and `SliderPostContentMargin`, in `Styles.Resources` of
  `CompactForms.axaml`. Measured: 18 pixels tall with a round 14 pixel thumb, against 50 with a 20 pixel one, and
  the track now starts 3 pixels below the box whose number it sets rather than 19 - a top margin of 2 plus
  the pixel of slack the template leaves. **The thumb is round because
  the two thumb resources are the same number**, and 14 is what the dot of a radio button comes to once its
  `Viewbox` has scaled it, so a slider and a radio button look like they belong to the same form.

The rest, in the order it matters:
The rules are **a row of controls each, not an editable grid**. Every time and every power is a text box with a
rule on it, and a grid cell that has to be clicked into before it becomes one hides both the value and the error
until then. `Gen24ChargingRuleViewModel` is one row. What no single field can see - a rule that ends before it
starts, two rules that contradict each other - is in `Problems()` alongside the field errors, which is what Apply
asks and what the tests ask. The header of the schedule and the rows under it are separate grids, so their
columns are tied together with `SharedSizeGroup` inside a `Grid.IsSharedSizeScope` rather than by giving both the
same numbers and hoping.

Each heading stands over its column the way the column sits under it, and all of it was measured to within 0.1 of
a pixel. Two of them were measured against the wrong thing first, and both are worth knowing:

- **A combo box shows its text 8 pixels in**, which is its padding, and the heading needs the same indent to
  start where the words do. A combo box also has an *empty* text block in its template for the placeholder, at
  the very left edge; lining the heading up against that one says everything is perfect while it plainly is not.
  The visible text is the one with something in it.
- **A check box with no content is still laid out as though it had some.** Fluent gives one a box of 20 and a
  content presenter 8 to the right of it, whatever it is showing, so centring the control leaves the box 4 to the
  left of centre - which is what made the day boxes look wrong under their letters. That 8 is not a resource, the
  way the metrics of a slider are: it is a margin inside the template and nothing styles it. The `NoContent`
  class in `CompactForms.axaml` takes it off the layout with a negative margin instead, which leaves the control
  alone and tells the layout what the box actually occupies. It also took the days column from 140 pixels to 98.

**The days are named once, in the heading**, and the boxes under them carry no letters: seven headings over seven
boxes says the same thing in a fraction of the width - the column measures 126 pixels, 18 to a day, where a box
with its own letter beside it needed a gap inside the pair and a wider one between the pairs to read correctly at
all. Both the heading and the row are a `UniformGrid` of seven, so a letter stands over its box without the two
having to agree on any measurement: measured at 0.1 of a pixel across all seven. Each box keeps the name of its
day as a tool tip, because a box on its own says nothing once the heading has scrolled out of sight - and that
name comes from `CultureInfo.CurrentCulture` through `Misc/WeekdayNames`, not from the resource files: a weekday
is not a term of this application that anybody has to translate, and .NET knows all of them. Measured with no
resource entry anywhere: Montag, glindesdi, and Määntig for `gsw-CH`.

**The culture, not the UI culture.** This one was got wrong once and corrected by the developer, so it is worth
the paragraph. The UI culture is what the resource files follow, and a day name is a word on the screen like any
other, which is the argument that lost: the method that answers it, `GetDayName`, lives on `DateTimeFormat`, next
to the date and number formats, and those follow the region. This repo is worked on a machine that reports a
culture of `gsw-CH` and a UI culture of `en-US`; taking the names from the UI culture there produced English days,
and the report was "I get the weekda in english. I expected gsw." Somebody who has set their region to
Switzerland is asking for Swiss days, whatever language their Windows menus are in. Measured through the view with
the two set apart: region `gsw-CH` with display language `en-US` gives *Määntig, Ziischtig, Mittwuch, Dunschtig,
Friitig, Samschtig, Sunntig*, and region `rm-CH` with display language `de-DE` gives *glindesdi, mardi, mesemna,
gievgia, venderdi, sonda, dumengia*.

The single letters of the heading stay in the resource files, and the framework itself is the argument: its short
names give `D` to `Ziischtig`, which begins with a Z, and `G` to both `glindesdi` and `gievgia`. Which letter to
use when two days collide is a matter of judgement, and the resource files make it deliberately.

The row itself carries **no** tool tip. It used to carry `RuleTooltip`, which says to use the right mouse button
to add and delete rules - true of the WPF grid and its context menu, and nonsense here, where there is an Add
button under the schedule and a cross at the end of every row. The resource stays, because two views of
FroniusMonitor still use it.

That cross at the end of a row is `i:CrossIcon`, a drawn shape, and not the character `✕`. The character was what
the row had first, and it is not in Inter: right on the desktop, an empty box in the browser, which is the only
head with no system font to fall back to. [[PlatformHeads.Lifecycle]] has the rule and the reasoning. Measured
after the change: the button is 28x16 in a row still 32 tall, the same height as the text boxes beside it.

Two pixels either side of every day, in the heading and in the rows alike, keeps each one centred and puts 4
between neighbours - measured at 4.2, the fifth of a pixel being what the Viewbox scaling leaves.

**`ITabHost` is why this tab can be tested.** A tab needs the dialog only for its busy text and its toast; taking
an interface for that instead of `Gen24SettingsDialogViewModel` means a test can build one without `DialogBase`
reaching into the static injector for `MainViewModel` and the whole client behind it.
`HomeAutomationServerTests` references the client project for exactly this, and starts no Avalonia application: a
view model is a plain object and nothing there creates a control. The Modbus tab still takes the dialog itself and
could move over the same way.

## The inverter settings tab writes three things, separately

`Gen24InverterSettingsViewModel`, ported from `InverterSettingsViewModel` of the WPF app. Three groups, and each
of them is its own endpoint: what the system is called and how it keeps time, the string trackers, and the export
limits. `Apply` works out the three deltas with the same functions the server will use again, and sends only the
groups that actually changed - so a changed system name does not rewrite the trackers. It stops at the first
failure, which leaves the groups before it written and nothing half applied inside a group.

**A tracker is a child view model**, `Gen24MpptViewModel`, one per tracker, with one view used twice. An inverter
has one or two of them and both are edited identically; the only difference is that every value is a channel of
its own - `PV_MODE_MPP_01_U16` against `PV_MODE_MPP_02_U16` - so the captions are built from the index and come
from the view model rather than from the localization markup extensions the rest of the tab uses. Those
extensions take a constant, and these keys are not.

- **The captions of a tracker that stand beside a box carry their own colon**, because they cannot go through
  `{l:Config 'key', ':'}`. The ones that label a combo box do not.
- `ShowDynamicPeakManager` and `ShowFixedVoltage` follow the power mode: a tracker that finds its own working
  point has a peak manager, one that is told where to sit has a voltage, and one that is off has neither.
  Measured through the view: two combo boxes and one box in `Auto`, one and two in `Fix`, one and one in `Off`.
- The tracker views are hosted in a `ContentControl` and found by the `ViewLocator`, not written into the view.
  With the view in place and a `DataContext` of null, an inverter with one string would show an empty group box;
  as a `ContentControl` the visibility is a property of the tab, which knows whether the tracker exists.

**The three export limits are one order** - the soft limit at or below the hard one, the hard one at or below the
peak power both are read against - and moving any of them takes whichever of the others is in its way with it.
Refusing the drag would be a worse way to say so. Only what is in the way moves: a limit raised to 7800 under a
peak of 8000 leaves the peak alone.

**What only a technician may change is not shown at all.** The trackers and the export limits are refused by the
inverter under its `customer` login, and the WPF app asked its own connection about that. A client here never
logs in to the inverter, so the server answers for it: `Gen24SettingsSnapshot.IsInverterTechnician`. Offering the
fields and letting the inverter refuse the write is a worse way to find out.

**Two deliberate differences from the WPF dialog**, both in `CopyToSettings`:

- A limit that is switched off is sent as a zero, and switching export limiting off switches both limits off -
  that part is the same. But **the peak power reference survives it**, where WPF replaces the whole
  `Gen24PowerLimitSettings` with a new one and takes the reference to zero with it. The reference is what the
  inverter's display reads a limit against, not a limit itself, so switching limiting off is no reason to forget
  the size of the system.
- The WPF `Apply` adds `displayModeSoftLimit` to the visualization token unconditionally, which makes its delta
  never empty and so writes the power limits on every Apply. Here it travels with the soft limit only - see the
  delta table above - so an unchanged page writes nothing and the endpoint can answer "already held it".

The time zone and the clock are behind the danger switch and are put back when it is switched off again, the same
rule as the energy flow tab: an inverter whose clock is wrong times the charging rules of that tab wrong.

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
  check boxes are scaled in a `Viewbox` - so the column type clears it in `GenerateElement`. It carries a
  `FontFeatures` property for the same reason: the base class hands a column's `FontFamily`, `FontSize`, weight
  and style to the text it generates but not the features, and **the two timestamp columns ask for `+tnum`** -
  they are the only place in the grid where digits stand one under another. Measured in Inter at 12 point:
  `11:11:11` measured 51.0 against 65.0 for `00:00:00` in the same column, and with the feature both measure
  67.0. Nothing else in the grid asks for it; see [[PlatformHeads.Lifecycle]] for why it goes on numbers rather
  than on the application. The severity icon is
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

- **All four tabs are ported.** What is *not* here from the WPF window is the multiple inverter setup - the list
  of connected inverters, its Add and Refresh, and the `staticControlledDevices` and
  `autodetectedControlledDevices` those wrote. The developer removed it from the WPF view first (commit
  `0d021a8`), so the port never had it. `Gen24ConnectedInverter`, `IGen24Service.GetConnectedDevices` and the
  cluster mode check box that turns the feature on are all still there, so it can come back as a piece of work of
  its own rather than being reconstructed from a deleted view.
- **`ConverterCulture`** has no counterpart in Avalonia's `Binding`, and `ValidationBinding` of the WPF app sets
  it. It does not matter for the fields ported so far - whole numbers and an IP string - but it will for anything
  with a decimal separator.
- **The role lockout** above.
- The busy texts of this dialog are translated into every language the app has; `rm` and `gsw` are worth a native
  reading. See [[Localization]] for how the resource files are kept.
