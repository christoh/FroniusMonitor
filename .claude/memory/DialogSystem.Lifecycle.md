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
  - HomeAutomationClient/HomeAutomationClient/MessageBoxes/**
  - HomeAutomationClient/HomeAutomationClient/Views/MainView.axaml
  - HomeAutomationClient/HomeAutomationClient/ViewModels/MainViewModel.cs
---

# Lifecycle contract: the dialog system (Avalonia)

The client also runs in the browser, where a second window does not exist, so a dialog is **not** a window. It is
a control that `MainView` shows on top of the main content. Everything a window manager would normally provide -
a title bar, a close box, moving, modality - is built here.

## The parts

| Part | Role |
|---|---|
| `DialogParameters` | What the caller asks for: `Title`, `ShowCloseBox`, `IsMoveable`, `IsModal`, `IsResizeable`. Derive for more (`MessageBox`). |
| `DialogBase<TParameters, TResult, TBody>` | The base of every dialog view model. Owns showing, waiting, closing. |
| `TBody` | The view: a `ContentControl` implementing `IDialogControl`, with a public parameterless constructor (`new()` constraint). |
| `DialogQueueItem` | One shown dialog - its body and its live parameters - held by `MainViewModel.CurrentDialog`. |
| `MainView.axaml` | The host: dimming layer, dialog frame, title bar, body, busy animation. |

## Showing and waiting

`ShowDialogAsync` is the whole lifecycle:

1. The dialog that is currently shown (possibly `null`) is pushed on `MainViewModel.DialogQueue`.
2. On the UI thread: a fresh `CancellationTokenSource`, a new `TBody` whose `DataContext` is the view model, and a
   `DialogQueueItem` built from the parameters. Then `MainViewModel.CurrentDialog` is set, which makes the overlay
   visible through `IsDialogVisible`.
3. `await Task.Delay(-1, token)` parks the call until `Close()` cancels the token. **This is how a dialog waits.**
   Never block the UI thread instead: on WebAssembly async is cooperative multitasking, and a blocking wait
   deadlocks the whole application.
4. `Close()` pops the previous item back into `CurrentDialog`, restores its busy text and cancels the token. The
   `OperationCanceledException` is swallowed and `Result` is returned to the caller.

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
`MainView.axaml` with a `FallbackValue`, because `CurrentDialog` is null while no dialog is shown. Nothing else
needs touching.

## Nesting

`DialogQueue` is a `ConcurrentStack`, so dialogs nest: a message box on top of the login dialog is normal. Only
the top one is visible; closing it brings the one underneath back, in its own state, because the body control
instance lives on in the popped item. The first `Push` stores `null` - that is what makes the last `Close` clear
the overlay.

## Busy text

`DialogBase.BusyText` is not a property of its own, it proxies `MainViewModel.DialogBusyText`, which the busy
animation over the dialog body binds to. `ShowDialogAsync` copies the current busy text into the queue item and
then clears it, and `Close()` restores the busy text of the item underneath. That is why the busy overlay of a
nested dialog does not leak into the dialog below it.

**Both happen before the body is created, and the order matters.** Creating the body assigns its `DataContext`,
which starts `Initialize` on the UI thread, and a dialog that sets a busy text there gets that far synchronously -
`await` on an already completed task does not yield. So a dialog **can** set its busy text in `Initialize` and have
it show. It could not while the copy and the clear came after the body: the argument list picked up the new busy
text and the clear then wiped it, so such a dialog opened with no busy animation at all and the queue item held the
wrong text to restore later.

## Modality

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

Off unless a dialog asks for it (`IsResizeable`), because a dialog is as big as what it has to show and a form
dragged wider only grows its whitespace. `Controls/DragResize.cs` is the counterpart of `DragMove`, an attached
behavior on the grip in the bottom right corner of the frame, wired the same way and dropped by the same trigger.
The only dialog that asks for it today is the settings dialog, and only while its event log tab is on screen -
that tab is a table of a few hundred rows and the user is the one who knows how much room to give it.

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

## Closing

- The close box is visible when `ShowCloseBox` is true and runs `MainViewModel.DialogClosedCommand`, which calls
  `AbortAsync` on the view model behind `CurrentDialog.Body`. Every dialog view model must implement it and decide
  what "aborted" means for its result (`MessageBoxViewModel` returns an empty `MessageBoxResult`).
- The view model itself closes by calling `Close()` after setting `Result`.
- **`AbortAsync` has to call `Close()` as well.** Nothing else does it for you: setting only `Result` and returning
  leaves the dialog on screen and `ShowDialogAsync` waiting on its token, so the close box appears to do nothing at
  all. Where a dialog has a Cancel button of its own, let both go through one method rather than writing the two
  paths separately - they are the same thing and drift apart otherwise.
- `Result` is one value. A dialog that has more to report exposes it as a property the caller reads after
  `ShowDialogAsync` returns: `LoginViewModel.User` is the `UserInfo` the server answered the login with, and
  `MainViewModel.Initialize` copies it to `MainViewModel.User` for the menu bar, which shows it as
  `name (Role, Role)` through `UserText`. The roles are the enum names on purpose and are not localized.

## What a dialog view model looks like

```csharp
public class MessageBoxViewModel(MessageBox parameters)
    : DialogBase<MessageBox, MessageBoxResult, MessageBoxView>(parameters)
```

The body's `OnDataContextChanged` starts `ViewModel.Initialize()` (fire and forget) - that is where a dialog loads
what it needs, as `LoginViewModel` does with the cached connection. Since `DataContext` is assigned in the object
initializer inside `ShowDialogAsync`, `Initialize` starts before the dialog is on screen.

**`Initialize` fires again whenever the body is re-attached.** `MainView` presents `CurrentDialog.Body` through one
host, so a nested dialog opening and closing over a dialog takes its body out of the tree and puts it back, and
`OnDataContextChanged` comes round a second time. A dialog that is shown once and closed never notices - the login
and the message boxes do not. One that **stays open** while it shows a message box does, and has to guard
`Initialize` against running twice, with the flag set before the first `await`. Otherwise it loads everything again
and puts its busy overlay back up over a dialog the user is working in.
`Gen24SettingsDialogViewModel` is the worked example; see [[SettingsDialogs.Lifecycle]].

## Known gaps

- `IDialogBase` is `IDisposable` and nobody disposes it. The `CancellationTokenSource` of every dialog is left to
  the finalizer, and `ShowDialogAsync` creates a fresh one in its `finally` without disposing the old one.
- No keyboard handling in the host: no Escape to abort. `LoginView` handles `Enter` in its own code behind and is
  the only dialog that reacts to a key at all.
- Nothing takes focus when a dialog opens.
- The title bar always uses `SystemControlBackgroundAccentBrush` and the dialog `DialogBackground`; a dialog cannot theme itself.
