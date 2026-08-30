---
paths:
  - HomeAutomationClient/HomeAutomationClient/ViewModels/Adapters/DialogBase.cs
  - HomeAutomationClient/HomeAutomationClient/ViewModels/Dialogs/**
  - HomeAutomationClient/HomeAutomationClient/Views/Dialogs/**
  - HomeAutomationClient/HomeAutomationClient/Models/Dialogs/**
  - HomeAutomationClient/HomeAutomationClient/Contracts/IDialogBase.cs
  - HomeAutomationClient/HomeAutomationClient/Contracts/IDialogControl.cs
  - HomeAutomationClient/HomeAutomationClient/Controls/DragMove.cs
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
| `DialogParameters` | What the caller asks for: `Title`, `ShowCloseBox`, `IsMoveable`, `IsModal`. Derive for more (`MessageBox`). |
| `DialogBase<TParameters, TResult, TBody>` | The base of every dialog view model. Owns showing, waiting, closing. |
| `TBody` | The view: a `ContentControl` implementing `IDialogControl`, with a public parameterless constructor (`new()` constraint). |
| `DialogQueueItem` | An immutable **snapshot** of one shown dialog, held by `MainViewModel.CurrentDialog`. |
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

## The queue item is a snapshot

`DialogQueueItem` is a positional record. It copies `Title`, `ShowCloseBox`, `IsMoveable`, `IsModal` **at show
time**. `DialogParameters` is a `BindableBase`, but nothing binds to it: changing `Parameters.Title` after the
dialog is up does not move to the screen.

**When you add a parameter,** add it in three places: `DialogParameters`, `DialogQueueItem`, and the constructor
call in `DialogBase.ShowDialogAsync`. Then bind `CurrentDialog.<Name>` in `MainView.axaml` with a `FallbackValue`,
because `CurrentDialog` is null while no dialog is shown.

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
`DragMove.IsEnabled` (bound to `CurrentDialog.IsMoveable`), `DragMove.Target` (the `DialogFrame`) and
`DragMove.ResetTrigger` (bound to `CurrentDialog`, so every new dialog starts centered).

It moves the target with a `TranslateTransform`, never with layout properties, and clamps the offset so that the
target cannot leave its container - also when the container is resized, which it re-checks on every `Bounds`
change. Pointer capture makes touch and pen work like the mouse. The close button keeps its own clicks because a
`Button` marks `PointerPressed` as handled before the drag handler sees it.

## Closing

- The close box is visible when `ShowCloseBox` is true and runs `MainViewModel.DialogClosedCommand`, which calls
  `AbortAsync` on the view model behind `CurrentDialog.Body`. Every dialog view model must implement it and decide
  what "aborted" means for its result (`MessageBoxViewModel` returns an empty `MessageBoxResult`).
- The view model itself closes by calling `Close()` after setting `Result`.

## What a dialog view model looks like

```csharp
public class MessageBoxViewModel(MessageBox parameters)
    : DialogBase<MessageBox, MessageBoxResult, MessageBoxView>(parameters)
```

The body's `OnDataContextChanged` starts `ViewModel.Initialize()` (fire and forget) - that is where a dialog loads
what it needs, as `LoginViewModel` does with the cached connection. Since `DataContext` is assigned in the object
initializer inside `ShowDialogAsync`, `Initialize` starts before the dialog is on screen.

## Known gaps

- `IDialogBase` is `IDisposable` and nobody disposes it. The `CancellationTokenSource` of every dialog is left to
  the finalizer, and `ShowDialogAsync` creates a fresh one in its `finally` without disposing the old one.
- No keyboard handling in the host: no Escape to abort. `LoginView` handles `Enter` in its own code behind and is
  the only dialog that reacts to a key at all.
- Nothing takes focus when a dialog opens.
- The title bar always uses `SystemControlBackgroundAccentBrush` and the dialog `DialogBackground`; a dialog cannot theme itself.
