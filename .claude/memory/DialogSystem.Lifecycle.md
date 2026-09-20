---
paths:
  - HomeAutomationClient/HomeAutomationClient/ViewModels/Adapters/DialogBase.cs
  - HomeAutomationClient/HomeAutomationClient/ViewModels/Dialogs/**
  - HomeAutomationClient/HomeAutomationClient/Views/Dialogs/**
  - HomeAutomationClient/HomeAutomationClient/Models/Dialogs/**
  - HomeAutomationClient/HomeAutomationClient/Contracts/IDialogBase.cs
  - HomeAutomationClient/HomeAutomationClient/Contracts/IDialogControl.cs
  - HomeAutomationClient/HomeAutomationClient/Controls/DragMove.cs
  - HomeAutomationClient/HomeAutomationClient/Controls/DragResize.cs
  - HomeAutomationClient/HomeAutomationClient/Controls/InitialWindowSize.cs
  - HomeAutomationClient/HomeAutomationClient/MessageBoxes/**
  - HomeAutomationClient/HomeAutomationClient/Views/MainView.axaml
  - HomeAutomationClient/HomeAutomationClient/Views/ChildWindow.axaml
  - HomeAutomationClient/HomeAutomationClient/Views/ChildWindow.axaml.cs
  - HomeAutomationClient/HomeAutomationClient/Contracts/IDialogPresenter.cs
  - HomeAutomationClient/HomeAutomationClient/Contracts/IPagePresenter.cs
  - HomeAutomationClient/HomeAutomationClient/Services/Presentation/**
  - HomeAutomationClient/HomeAutomationClient/ViewModels/MainViewModel.cs
  - HomeAutomationClient/HomeAutomationClient/Styles/DetailViews.axaml
---

# Lifecycle contract: the dialog system (Avalonia)

The client also runs in the browser, where a second window does not exist, so a dialog **cannot depend on being**
a window. It is a control, and where that control appears is decided by a presenter the head chooses: the dialog
frame of `MainView`, which builds a title bar, a close box, moving and modality itself, or - since 2026-09-15, on
the desktop - a window, where the operating system provides all four.

**A dialog view model knows nothing about either.** It asks to be shown and is told when it is closed; everything
below the line `presenter.Create(...)` is the presenter's.

## The parts

| Part | Role |
|---|---|
| `DialogParameters` | What the caller asks for: `Title`, `ShowCloseBox`, `IsMoveable`, `IsModal`, `IsResizeable`, `StaysInMainView`, `WindowKey`, `IsModalWindow`. Derive for more (`MessageBox`). |
| `DialogBase<TParameters, TResult, TBody>` | The base of every dialog view model. Owns showing, waiting, closing. |
| `TBody` | The view: a `ContentControl` implementing `IDialogControl`, with a public parameterless constructor (`new()` constraint). |
| `IDialogPresenter` / `IDialogPresentation` | Where a dialog appears, and the one that is on screen. |
| `IPagePresenter` | The same for a detail page, which has the same two answers. |
| `MainViewPresenter` | Both, inside `MainView`: the dialog frame and the one content host. Every head but the desktop. |
| `WindowPresenter` | Both, as windows. The desktop only. |
| `ChildWindow` | The window a dialog or a page goes in: chrome, a content host and a busy animation. |
| `InitialWindowSize` | Two optional attached properties a detail page - or, since 2026-09-16, a dialog body - sets on its own root to say how big its window opens. |
| `DialogQueueItem` | One shown dialog - its body and its live parameters - held by `MainViewModel.CurrentDialog`. Only `MainViewPresenter` uses it. |
| `MainView.axaml` | The host of that presenter: dimming layer, dialog frame, title bar, body, busy animation. |

## Where a dialog appears

`App.OnFrameworkInitializationCompleted` registers one presenter under both contracts, chosen by the lifetime:
`IClassicDesktopStyleApplicationLifetime` gets `WindowPresenter`, everything else `MainViewPresenter`. One
instance for both contracts on purpose - a logout closes dialogs and pages together, and the two must not hand out
two windows for one key.

What the desktop does with each dialog:

| | Window | Why |
|---|---|---|
| Message box, error box | **modal**, never reused | It answers a question the user has just been asked, and one over another is normal |
| Everything else | non-modal, one per `WindowKey` | A window that may be left standing: the settings of one inverter beside those of another |
| The login dialog | none - it stays in `MainView` | `StaysInMainView`: there is nothing to put a window beside before anyone is logged in |

- **`IsModalWindow` is a virtual property, not a flag a caller sets.** `MessageBox` overrides it to true and
  nothing else does, which is exactly "only message boxes and error boxes are modal". It is not `IsModal`: that
  one drives the dimming layer of the dialog frame, which a window has no use for.
- **`WindowKey` is the identity, not the device.** `MainViewModel.Settings` passes `device.Key`, so each inverter
  gets a window of its own and asking for the same one again brings its window to the front - `Create` returns
  null and `ShowDialogAsync` returns its default result at once, without building a body. Bringing it to the front
  is `ChildWindow.Reactivate`, which restores a minimized window first: activating one the user cannot see would
  look exactly like a click that did nothing.
- **The window follows the live parameters** for the one thing it can: `IsResizeable`, which the event log tab of
  the settings dialog turns on and off while the dialog is up. Turning it on also lifts the `MaxWidth` the body
  declares, the way `DragResize` does in the frame, and turning it off puts it back.
- **The close box of the chrome goes through `AbortAsync`.** `ChildWindow` cancels the close and lets the view
  model do it, because a window closed from under a dialog would leave whoever awaits `ShowDialogAsync` waiting
  for ever. A dialog with `ShowCloseBox = false` cannot be closed by its chrome either.
- **A logout closes all of it** (`CloseAll` on both contracts, from `MainViewModel.Logout`), through the view
  models, so every awaiting caller is released.
- **The menu bar asks where pages go.** `IPagePresenter.ShowsPagesInMainView` is what hides the Dashboard entry on
  the desktop (`MainViewModel.ShowDashboardMenu`): that entry is there to bring the dashboard back once a page has
  taken its place, and where every page opens in a window the dashboard is never covered.

**Nothing above a page window is the app.** A page inside `MainView` can reach the singleton `MainViewModel`
through the tree - `$parent[v:MainView]` and `$parent[Window]` both arrive - while a page in a window of its own
has neither above it: the window is a root, and its tree ends at the page. A binding that walks upwards for
application state therefore works on the browser and the phones and silently produces **nothing** on the desktop,
where it leaves the property at its default and looks like a switch that does not work. That is exactly what
happened to "Always fully color gauges" between 2026-09-15, when the detail pages were given windows, and
2026-09-19.

Application state that the detail pages read travels as an **application resource** instead, which every window
shares whoever opened it: `MainViewModel.PublishColorAllTicks` writes `ColorAllTicks` under
`MainViewModel.ColorAllTicksResourceKey` from the constructor and on every change, and the
`WrapPanel.GaugeGroups c|HalfCircleGauge` style in `Styles/DetailViews.axaml` reads it with `DynamicResource`,
which follows each later write. Do not put a `$parent[v:MainView]` binding back; the dashboard may use one,
because the dashboard is `MainView` content on every head.

**A `[RelayCommand]` that opens a dialog needs `AllowConcurrentExecutions = true` and a `CanExecute` of its own.**
This is the trap of the whole change and it is invisible in the dialog code. The generated `AsyncRelayCommand` says `CanExecute` is false while
its previous run is still pending, and a run that awaits `ShowDialogAsync` is pending for as long as the dialog is
on screen - which used to be a modal moment and is now a window the user leaves standing. Without the flag the
menu entry or button is **disabled** for exactly that time, so a second settings dialog could never be opened and
clicking Electricity price while its window was up did nothing at all, not even bring it to the front. It carries
Seven commands need the flag today: `MainViewModel.Settings`, `ChangePassword`, `ShowEnergyChart` and `ShowSolarWebChart`,
`EnergyChartViewModel.ShowPriceComponents`, `UserManagementViewModel.Add` and `Edit`. (`ShowPowerFlow` opens a
page, not a dialog, and needs neither the flag nor the gate.)

**The flag on its own is too much, though.** The menu bar is the one thing a modal dialog does not disable - the
dimming layer sits in the content row, not over the bar - so on a head with one dialog frame the disabled command
was the only thing keeping a second settings dialog off the first. The four commands of the menu bar are therefore
gated on `MainViewModel.CanOpenDialog`, which asks `IDialogPresenter.ShowsDialogsInMainView`: where one frame shows
one dialog at a time nothing may be opened over it, and where every dialog gets a window everything may. `Logout`
is gated the same way and is deliberately **not** concurrent - it shows a modal confirmation and then tears the
session down, and logging out with a settings dialog on the frame would leave that dialog queued under the login.

**Switching pages stays allowed while a dialog is up.** That is what the menu bar is left usable for, and only the
commands that would open a second dialog are held back. The commands *inside* a dialog need no gate: `MainView`
puts only the top dialog's body in the tree, so the buttons of the one underneath cannot be clicked at all.

## Showing and waiting

`ShowDialogAsync` is the whole lifecycle:

1. On the UI thread: a fresh `CancellationTokenSource`, then `presenter.Create(this, Parameters)`. **Before the
   body**, because creating the body assigns its `DataContext`, which starts `Initialize`, and a dialog that sets
   a busy text there needs somewhere for it to go. A null presentation means an equivalent dialog was already up
   and has been activated; the call returns its default result there and then.
2. A new `TBody` whose `DataContext` is the view model goes to `SetBody`, then `Open` puts it on screen.
3. `await Task.Delay(-1, token)` parks the call until `Close()` cancels the token. **This is how a dialog waits.**
   Never block the UI thread instead: on WebAssembly async is cooperative multitasking, and a blocking wait
   deadlocks the whole application.
4. `Close()` takes the dialog off screen through the presentation and cancels the token. The
   `OperationCanceledException` is swallowed and `Result` is returned to the caller.

Inside `MainViewPresenter` steps 1, 2 and 4 are what `DialogBase` did itself before the presenters were split
out: push the shown dialog on `MainViewModel.DialogQueue`, set `CurrentDialog` to a `DialogQueueItem` built from
the parameters - which makes the overlay visible through `IsDialogVisible` - and pop it back on close.

So the caller writes one line and gets the answer:

```csharp
var result = await new MessageBox { Text = "…", Icon = new ErrorIcon() }.Show();
```

`MessageBox.Show()`, `Exception.Show()` and `ProblemDetails.Show()` (all in `MessageBoxes/ErrorBoxes.cs`) are the
front door for message boxes; `Exception.Show()` marshals to the UI thread itself.

`Exception.Show()` is the **unhandled fallback**, not a place to classify hub failures. ViewModel methods that
invoke the hub catch `HubException` by type and call `ex.ShowHubError()`, an extension in `ErrorBoxes`.
It logs the exception at guarded Warning level and shows `Resources.Error` as the caption and `ex.Message`
as the text, without a stack trace or an invented HTTP status. The message is displayed, never inspected to
decide how to handle the error.

## The queue item holds the live parameters

`DialogQueueItem` is a positional record of `Title`, `Body`, `Parameters` and `BusyText`. The parameters are the
**live** `DialogParameters` object of the dialog, not a copy of its fields, so a dialog may change how its frame
behaves while it is up: that is how the settings dialog turns resizing on for its event log tab and off again for
the others. `Title` is the exception - it is read once, when the dialog is put up, because the frame needs a title
before the body has run its `Initialize`.

**When you add a parameter,** add it to `DialogParameters` and bind `CurrentDialog.Parameters.<Name>` in
`MainView.axaml` with a `FallbackValue`, because `CurrentDialog` is null while no dialog is shown. Then decide
what a window does with it: `WindowDialogPresentation` follows `IsResizeable` and `Title` through
`DialogParameters.PropertyChanged` and reads the rest once, when the dialog goes up.

## Nesting

This is the dialog frame only. In windows nothing nests: every dialog is a top level window of its own, and a
modal message box over one is the operating system blocking the rest.

`DialogQueue` is a `ConcurrentStack`, so dialogs nest: a message box on top of the login dialog is normal. Only
the top one is visible; closing it brings the one underneath back, in its own state, because the body control
instance lives on in the popped item. The first `Push` stores `null` - that is what makes the last `Close` clear
the overlay.

## Busy text

`DialogBase.BusyText` is not a property of its own, it proxies the presentation, because the animation belongs to
wherever the dialog is. **The override announces `BusyText` and `IsBusy` itself** (since 2026-09-20): the
generated setter it replaces would have, and without it a binding to `IsBusy` - the row of controls a dialog
disables while it loads - kept whatever it read first. The Solar.web chart, whose `Initialize` sets the busy text
before the first binding is read, came up with every button disabled for good. `MainViewDialogPresentation` proxies `MainViewModel.DialogBusyText`, which the animation
over the dialog frame binds to; `WindowDialogPresentation` proxies `ChildWindow.BusyText`, which is the animation
in that dialog's own window. A dialog that writes a busy text needs neither to know which.

In the frame, the presentation takes the busy text that was on screen and clears it when it is created, and puts
it back when it closes, so the busy overlay of a nested dialog does not leak into the dialog below it.

**Both happen before the body is created, and the order matters.** Creating the body assigns its `DataContext`,
which starts `Initialize` on the UI thread, and a dialog that sets a busy text there gets that far synchronously -
`await` on an already completed task does not yield. So a dialog **can** set its busy text in `Initialize` and have
it show. It could not while the copy and the clear came after the body: the argument list picked up the new busy
text and the clear then wiped it, so such a dialog opened with no busy animation at all and the queue item held the
wrong text to restore later. This is why the presentation is created **before** the body and not with it.

**What is put back is the presentation's own `busyTextBelow`, not `CurrentDialog.BusyText`.** Reading it off the
item that comes back - which is what `DialogBase.Close` did until 2026-09-15 - restores the busy text of the dialog
*below* the one being uncovered, which is one dialog too far down: a dialog that was busy when a message box opened
over it came back idle. Fixed in `MainViewDialogPresentation.Close` and pinned by
`MainViewPresenterTests.A_nested_dialog_covers_the_one_below_and_gives_it_back_with_its_busy_text`.

## Modality

In the dialog frame. A window is modal or not by `IsModalWindow` instead - see "Where a dialog appears" - and
`IsModal` is not read there at all.

`IsModal` (default `true`) becomes `MainViewModel.IsModalDialogVisible`, and that drives three things in
`MainView.axaml`:

- the dimming layer (`DisableBrush`) is only visible for a modal dialog, and it swallows pointer input,
- the main content and the tick-color switch are disabled through `BoolInverter`,
- the busy animation over the dialog body is only visible for a modal dialog.

A non-modal dialog leaves the views live. The overlay grid itself has **no background** on purpose: a panel
without a background does not take part in hit testing, so clicks fall through to the view behind it while the
dialog frame, which has a background, still gets its own. Do not give that grid a background again.

**Consequence to keep in mind:** a non-modal dialog has no busy indicator. `LoginViewModel` sets
`BusyText = Resources.BusyLoggingIn` while it logs in, and since the login dialog is non-modal, nothing shows it
and the Login button stays clickable. Fix it in the dialog body (disable it while busy) rather than by bringing
the blocking overlay back.

## Moving

The dialog frame has to build this; a window is moved by its own title bar and none of it applies there.

`Controls/DragMove.cs` is an attached behavior, not a control. The title bar grid carries
`DragMove.IsEnabled` (bound to `CurrentDialog.Parameters.IsMoveable`), `DragMove.Target` (the `DialogFrame`) and
`DragMove.ResetTrigger` (bound to `CurrentDialog`, so every new dialog starts centered).

It moves the target with a `TranslateTransform`, never with layout properties, and clamps the offset so that the
target cannot leave its container - also when the container is resized, which it re-checks on every `Bounds`
change. Pointer capture makes touch and pen work like the mouse. The close button keeps its own clicks because a
`Button` marks `PointerPressed` as handled before the drag handler sees it.

**The handle carries a `SizeAll` cursor, and `Cursor` is inherited.** So anything inside the title bar that is not
there to be dragged has to set a cursor of its own, or it offers to move the dialog: the close box sets
`Cursor="Arrow"` in its style for exactly that reason. The icon and the title text inherit it on purpose - the
dialog can be dragged by both.

## Resizing

In the dialog frame. A window gets `CanResize` from the same parameter and the rest of this section does not
apply to it; what the two have in common is that the maximum the body declares is lifted while it is resizable.
**A window also lifts an explicit `Width` and `Height` off the body** (since 2026-09-20): the two chart dialogs
state their size that way, because that is what the frame's `DragResize` needs, and in a window an explicit size
stays what it is - the body sat at 1160 in the middle of a window dragged to 1600. `ChildWindow.ApplyContentLimits`
sets them to `NaN`, hands the body's `MinWidth`/`MinHeight` to the window, and the lifted size is what the window
opens at where `InitialWindowSize` says nothing (`ApplyInitialSize`); switching resizing off puts it all back.
`A_resizable_window_lifts_the_explicit_size_off_its_body_and_opens_at_it` pins it.

Off unless a dialog asks for it (`IsResizeable`), because a dialog is as big as what it has to show and a form
dragged wider only grows its whitespace. `Controls/DragResize.cs` is the counterpart of `DragMove`, an attached
behavior on the grip in the bottom right corner of the frame, wired the same way and dropped by the same trigger.
Two dialogs ask for it today: the settings dialog, only while its event log tab is on screen - that tab is a table
of a few hundred rows and the user is the one who knows how much room to give it - and the price chart
(`EnergyChartViewModel`, see [[EnergyData]]), from the start, because a chart gets better with every pixel.

**What it promises: a dialog can be dragged to fill the whole overlay, from wherever it happens to sit, and
nothing is ever clipped.** Getting there took five attempts and every one of them failed in the same two ways -
either the dialog stopped growing well short of the screen, or its tab headers, buttons and first column were cut
off. Both have the same cause and both are counter-intuitive, so they are worth stating plainly.

**A dialog is moved by a render transform, which the layout cannot see.** `DragMove` translates the frame, so a
dialog dragged to the left edge still occupies its old, centred layout rectangle - and the room to grow into is
measured from that rectangle, not from where the dialog is. Measured: a dialog centred 188 from the left of a
1400 wide overlay stopped at 1212 wide, and moving it out of the way first made no difference whatsoever, which
is exactly what it looks like when the fix is to fold the transform into the margin and clear it. `Pin` does that
now; nothing shifts on screen, and the room to the right becomes the room the user can see is there.

**An element with an explicit size that does not fit is centred in the space it did get.** So it hangs over on
both sides at once, and what goes missing is the top and bottom of the dialog and the left and right of the table
inside it - the failure that kept coming back. The size has to be explicit (a maximum only binds content that
wants more than it, and a tab wants what it wants, so raising a maximum moves nothing), which means the clamp has
to be right. Belt and braces: the target is also aligned to the top left while it is sized, so a mistake in the
arithmetic shows as empty space at the bottom right instead.

The rest, in the order it matters:

- **It sizes the body of the dialog, not the frame.** The body is what decides how big a dialog is:
  `Gen24SettingsDialogView` asks for a `MaxWidth` of 1024, and that is why the dialog is 1024 wide - its event log
  wants far more. Sizing the frame alone would leave the body at its cap with empty space beside it.
- **The frame is pinned while the drag lasts** (`DragResize.Anchor`, bound to the `DialogFrame`), or growing the
  dialog moves its top and left edges outwards by half of what it grew and the whole thing creeps up and to the
  left. Pinning is left and top alignment with the position it is *seen* at as a margin. Never a negative one: a
  dialog already bigger than the overlay would be held clipped at the moment the user is trying to make it fit.
- **The pinned corner gives way once there is nothing left to grow into, and only then.** That is what makes the
  full overlay reachable without the user having to move the dialog first, while keeping the position for as long
  as keeping it costs nothing. Measured from four starting positions - unmoved, moved left, moved right, moved
  hard left - each reaching 1400x836 of a 1400x836 overlay, short by nothing.
- **The dragged size replaces the maximum the dialog declares**, which is what lets a drag pass the 1024 the
  settings dialog asks for. It declares no maximum height at all: the dialog is as tall as what is on the tab,
  and the energy flow tab grows with every rule added to its schedule.
- **The size is given up when resizing is switched off; the place is not.** For the settings dialog that is the
  moment the user leaves the event log tab, and it has to happen: a size is a size whichever tab is showing, so
  one left behind by a table of a few hundred rows would stop the forms on the other tabs sizing themselves to
  what is on them. Where the user put the dialog is not like that and is kept until the dialog is gone - so the
  pin outlives the `Resizer`, which is thrown away and rebuilt on every such switch, and lives in the
  `PlaceBeforePin` attached property on the frame instead. Measured: a dialog moved to 68, resized to 1264 wide,
  then handed a 420 by 260 form - it came back to 420 wide, with an unset `Width`, still at 68; and the dialog
  after it was centred again.
- **Reset puts the size, the maxima, the alignments and the margin back** rather than clearing them. A dialog
  states its own size in XAML, which is a local value like the one a drag writes, so clearing does not reveal the
  dialog's number but the framework's - no maximum at all, and the dialog reopens as wide as its widest tab wants.
- **It re-clamps when the room changes**, by following the container's bounds: a dialog dragged large and then met
  with a smaller window is an explicit size that nothing re-examines. Measured: 1212 by 810 kept in a window
  shrunk to 700 by 500, buttons 160 pixels below the bottom edge; it now comes back on its own.
- **What the frame has around the body - its title bar - is measured once, at drag start.** Taken live it is the
  difference between two bounds that different layout passes have written, and mid-drag it goes negative, which
  inflates the room and produces exactly the clipping above.
- **The grip takes its drag on a `Panel` with a transparent background, not on the lines drawn inside it.** A
  stroked `Path` is hit only along its stroke, so it would have to be grabbed by a one pixel diagonal; a
  transparent background takes part in hit testing where no background at all does not. The probe found this by
  failing to drag anything.

The probe that established all of it drives the real `MainView` with simulated pointer input and asserts two
things: that a long drag fills the overlay from any starting position, and that at a dozen window sizes from
3000 by 2000 down to 420 by 300 nothing overflows and the buttons stay visible. **If this is touched, rebuild
that probe first.** Reasoning about which of `Width`, `MaxWidth`, the alignment, the render transform and the
arrange pass wins was wrong five times in a row; measuring was right every time.

## How big a page window opens

Sized by its content, unless the page says otherwise. `Controls/InitialWindowSize.cs` is how it says so - two
attached properties on the page's own root, set in its XAML:

```xml
<ContentPage c:InitialWindowSize.Width="1400" c:InitialWindowSize.Height="900" …>
```

- **Optional and per view.** The default of both is `NaN`, which means "the content decides", so a page that says
  nothing behaves exactly as every page window did before this existed. All four detail views set both today -
  inverter 1055x952, smart meter 1280x775, WattPilot 980x780, battery 680x780 - measured on the real app rather
  than derived from anything, so change them by looking, not by reasoning.
- **The two are independent.** Fixing only the width gives a window that wide and as tall as what is on it, which
  is the useful combination for a wall of gauges: the width is what decides how many fit in a row, the height is
  however many rows that makes.
- **Initial, not fixed.** The window is resizable from the moment it is up and nothing is written back or
  remembered, so the next window for that page opens at the declared size again.
- **A dimension left to the content keeps following it** after the window is up (since 2026-09-17; before,
  `OnOpened` switched a resizable window to `SizeToContent.Manual`). The power flow page has no cards until its
  view model has heard from the devices, which is after `Loaded`, so measured once on opening its window was the
  height of its title and legend. Handing a dimension over to the user is Avalonia's own doing:
  `Window.HandleResized` drops the auto-sizing of the dimension the user drags, that one only, so a window the
  user made narrower still grows in height as the consumers wrap into more rows.
  `A_content_sized_page_window_follows_content_that_arrives_after_opening` pins it.
- **It is read by the presenter, not by the window.** `ChildWindow` has no idea that what is on it is a page;
  `WindowPresenter.Show` reads the properties off the page and calls `ChildWindow.SetInitialSize`. Inside
  `MainView` the properties are simply not read - a page fills the view it is put in - and that is not an error:
  the same view runs on every head.
- **The screen cap still binds it.** `SetInitialSize` is called after `LimitToScreen` and clamps to the same
  maximum, so a view may ask for more room than the screen has and its window still opens on the screen. A zero,
  a negative number or an infinity counts as "not asked for": it is one number in a view's XAML, and there is
  nobody for the window to report it to.
- **Unless the page lifts the cap on its height.** `c:InitialWindowSize.LimitHeightToScreen="False"` (since
  2026-09-17) makes the presenter call `LimitToScreen` for the width only, so a content-sized height may take the
  whole screen instead of the presenter's 90 % of it. The power flow page does this: it fixes its width at 1500
  and is as tall as its cards, however many rows of consumers there are. **The screen itself still bounds it**,
  and not by our doing: `Window.MeasureOverride` measures a content-sized window against the platform's
  `MaxAutoSizeHint`, the working area, and Windows refuses a resizable window taller than the virtual screen
  (`WM_GETMINMAXINFO`, which Avalonia only widens for a finite `MaxHeight`). A headless window asked for 100 000
  came out exactly the screen's height. The width is capped either way.
- **A fixed dimension is not sized to its content at all.** `ChildWindow.ApplySizeToContent` picks
  `SizeToContent.Width`, `.Height`, `.WidthAndHeight` or `.Manual` from which of the two were given. Setting both
  a size and `SizeToContent` for the same dimension has the content win, and the number the view asked for would
  silently do nothing.

**A dialog body may say the same** (since 2026-09-16): `WindowDialogPresentation.Open` reads the two properties
off `HostedContent` after `LimitToScreen(1)`, so a body with no size of its own opens at the size it declares and
is then the user's to resize. No dialog uses it today - the power flow page was one for an afternoon, see
[[PowerFlowPage.Lifecycle]], and is a page now - but `SizedTestDialog` pins that it works. A body that says
nothing, which is every form, stays as big as what is on it, as before. It is on the body and not in
`DialogParameters` because it is the view's knowledge, not the caller's: the same view says the same thing on
every head, and inside the dialog frame it is simply not read. A resizable dialog body with a plain `Width` and
`Height` - the chart dialogs - gets the same treatment without saying anything: see "Resizing" above.

## Closing

- **A dialog window may refuse its own close box and nothing else.** `WindowDialogPresentation.OnClosing` cancels
  the close only for `WindowCloseReason.WindowClosing` (and `Undefined`, which is a platform that did not say and
  so is treated as the user). `ApplicationShutdown`, `OSShutdown` and `OwnerWindowClosing` take the dialog down
  through its view model instead and let the window go. **Cancelling those is cancelling the shutdown itself**: a
  close that any window vetoes is a close that does not happen, so with `ShutdownMode.OnMainWindowClose` a single
  open dialog closed the main window, then closed itself, and left the process running with nothing on screen and
  no way back to it. Pages never had this - they cancel nothing.
- The close box is visible when `ShowCloseBox` is true and runs `MainViewModel.DialogClosedCommand`, which calls
  `AbortAsync` on the view model behind `CurrentDialog.Body`. In a window the close box of the chrome does the
  same thing: `ChildWindow` cancels the close and calls `AbortAsync`, so there is one way out and not two. Every dialog view model must implement it and decide
  what "aborted" means for its result (`MessageBoxViewModel` returns an empty `MessageBoxResult`).
- The view model itself closes by calling `Close()` after setting `Result`.
- **`AbortAsync` has to call `Close()` as well.** Nothing else does it for you: setting only `Result` and returning
  leaves the dialog on screen and `ShowDialogAsync` waiting on its token, so the close box appears to do nothing at
  all. Where a dialog has a Cancel button of its own, let both go through one method rather than writing the two
  paths separately - they are the same thing and drift apart otherwise.

## What a dialog view model looks like

```csharp
public class MessageBoxViewModel(MessageBox parameters)
    : DialogBase<MessageBox, MessageBoxResult, MessageBoxView>(parameters)
```

The body's `OnDataContextChanged` starts `ViewModel.Initialize()` (fire and forget) - that is where a dialog loads
what it needs, as `LoginViewModel` does with the cached connection. Since `DataContext` is assigned in the object
initializer inside `ShowDialogAsync`, `Initialize` starts before the dialog is on screen.

**`Initialize` fires again whenever the body is re-attached.** In a window of its own the body is never taken out
of the tree, so there it runs once - but the guard is still needed, because the same dialog runs in the frame on
every other head. `MainView` presents `CurrentDialog.Body` through one
host, so a nested dialog opening and closing over a dialog takes its body out of the tree and puts it back, and
`OnDataContextChanged` comes round a second time. A message box is shown once and closed and never notices. Any
dialog that **stays open** while it shows a message box does, and has to guard `Initialize` against running twice,
with the flag set before the first `await`. Otherwise it loads everything again and puts its busy overlay back up
over a dialog the user is working in. `Gen24SettingsDialogViewModel` is the worked example; see
[[SettingsDialogs.Lifecycle]]. `LoginViewModel` belongs in that group too: a refused login, an unreachable server
or a mistyped address all put a message box over it, and re-running `Initialize` would throw away what the user
had typed.

## Where this is tested

`WindowPresenterTests` and `MainViewPresenterTests` drive both presenters with real windows on the headless
platform - see [[Testing.HeadlessAvalonia]]. Between them they cover a window per dialog and per device page,
reuse and activation, the close box through `AbortAsync`, a dialog with no close box, resizing switched while the
dialog is up, the modal message box, a logout, and on the other side the dialog frame, nesting, the busy text
handover, one page per view type and the menu bar gate.

`Closing_the_owner_window_takes_a_dialog_with_it_and_releases_its_caller` and `…_without_a_close_box_too` pin the
veto rule. They close the test's main window, which is the owner of every dialog window, and assert that the
dialog window goes with it and its caller is released - the shutdown itself cannot be driven from a test, because
the headless session is one lifetime for the whole run.

`GaugeColoringTests` pins the paragraph above: a gauge in a `GaugeGroups` panel in a window with no `MainView`
anywhere follows the switch, a window opened later starts the way the switch stands, and the resource carries the
value. The first of the three fails on the old `$parent[v:MainView]` binding, which is what it is for.

The four `A_page_window_…` facts in `WindowPresenterTests` cover the initial size: both dimensions asked for, one
of the two, neither, and a page asking for more than the screen has; `A_dialog_body_may_declare_the_size_its_window_opens_at`
covers the dialog side with `SizedTestDialog`. The last one reads the screen back off the
window and so fails rather than passing vacuously if the cap ever stops being applied.

## Known gaps

- `IDialogBase` is `IDisposable` and nobody disposes it. The `CancellationTokenSource` of every dialog is left to
  the finalizer, and `ShowDialogAsync` creates a fresh one in its `finally` without disposing the old one.
- No keyboard handling in the host: no Escape to abort - in the dialog frame. A window gets Escape, Alt+F4 and the
  rest from the operating system, so this gap is the frame's alone. `LoginView` handles `Enter` in its own code behind and is
  the only dialog that reacts to a key at all. It gets away with knowing nothing about what the dialog is
  currently showing, because the two modes of that dialog share one `OkCommand`.
- Nothing takes focus when a dialog opens; a window at least takes the focus of the window manager.
- The title bar always uses `SystemControlBackgroundAccentBrush` and the dialog `DialogBackground`; a dialog cannot theme itself.
- Neither a dialog window nor a page window remembers its size or its place, unlike the main window
  ([[PlatformHeads.Lifecycle]]). A dialog opens centred on its owner, sized to its content, every time; a page
  opens centred on the screen, at whatever `InitialWindowSize` its view declares, every time. What the user
  dragged either to is lost when it closes.
