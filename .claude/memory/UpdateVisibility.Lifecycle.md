---
paths:
  - HomeAutomationClient/HomeAutomationClient/Contracts/IVisibilityService.cs
  - HomeAutomationClient/HomeAutomationClient/Services/VisibilityService.cs
  - HomeAutomationClient/HomeAutomationClient/Services/ConnectionGate.cs
  - HomeAutomationClient/HomeAutomationClient/Services/UpdateService.cs
  - HomeAutomationClient/HomeAutomationClient/Controls/ViewVisibility.cs
  - HomeAutomationClient/HomeAutomationClient/ViewModels/ViewModelBase.cs
  - HomeAutomationClient/HomeAutomationClient/ViewModels/HouseViewModel.cs
  - HomeAutomationClient/HomeAutomationClient/ViewModels/PowerFlowViewModel.cs
  - HomeAutomationClient/HomeAutomationClient/ViewModels/Dialogs/EnergyChartViewModel.cs
  - HomeAutomationClient/HomeAutomationClient/Views/PowerFlowView.axaml.cs
  - HomeAutomationClient/HomeAutomationClient/Views/DashboardView.axaml.cs
  - HomeAutomationClient/HomeAutomationClient/Views/Dialogs/EnergyChartView.axaml.cs
  - HomeAutomationClient/HomeAutomationClient/Controls/HouseControl.axaml.cs
  - HomeAutomationClient/HomeAutomationClient/App.axaml.cs
  - HomeAutomationServer/Hubs/HomeAutomationHub.cs
  - HomeAutomationServerTests/UnitTests/ConnectionGateTests.cs
  - HomeAutomationServerTests/UnitTests/ViewModelVisibilityTests.cs
  - HomeAutomationServerTests/UnitTests/Avalonia/VisibilityServiceTests.cs
  - HomeAutomationServerTests/UnitTests/Fakes/FakeVisibilityService.cs
---

# Lifecycle contract: updates only where somebody looks

Built 2026-09-17 on the developer's request: a page or dialog nobody can see should stop following the update
service, and while nothing at all can be seen the hub connection should be dropped and opened again as soon as a
window comes back - with the devices whose changes the hub does not repeat fetched over HTTP.

## The three layers

| Layer | Type | Knows about | Answers |
|---|---|---|---|
| Contract | `Contracts/IVisibilityService` | nothing of the UI | `IsAnyVisible`, `IsAnyVisibleChanged` |
| Source | `Services/VisibilityService` | Avalonia: windows and the app lifetime | the contract, plus `IsVisible(TopLevel)` and `VisibilityChanged` for the views |
| Consumers | `ConnectionGate` inside `UpdateService`; `ViewVisibility` for the views | the contract; the source | drop and reopen the hub; set `ViewModelBase.IsShown` |

`VisibilityService` is registered once in `App.axaml.cs`, under its own type for the views and under the contract
for the update service - the same instance, like the presenters.

## What "can be seen" means

A window is seen when it is shown (`IsVisible`) and not `Minimized`. The whole app is not seen while the platform
says it is in the background. **Nothing else counts:** a window behind another window is seen, and so is a window on
a virtual desktop the user is not looking at. Avalonia reports neither - there is no occlusion API and no virtual
desktop API on any of its platforms - so the developer's wish to treat those as invisible could not be met, and was
reported as such. Do not add a Windows-only COM query for the virtual desktop without asking; it would make the
desktop head behave differently per OS.

Where there is nothing to judge by - no window on the lifetime's list, a head without windows and without a
lifetime notion of background - the answer is **seen**. Fail open: a connection nobody looks at costs a little, a
dashboard that does not move costs a bug report.

### The desktop: windows report themselves

Nothing registers a window. The static constructor of `VisibilityService` puts class handlers on
`Window.WindowStateProperty` and `Visual.IsVisibleProperty` (for `Window` senders), and the desktop lifetime's
`Windows` is the list that is judged, so a message box counts as much as the main window. A window that becomes
visible gets its `Closed` followed, because the lifetime takes a closed window off the list without a property of
the window changing - and `Closed` is raised **before** the removal, so the recompute is done twice, once at once
and once posted, which is what makes "the last window closed" come out as seen (`VisibilityServiceTests`).

### Everywhere else: the platform lifetime

`Application.TryGetFeature(typeof(IActivatableLifetime))` answers where the platform has the notion of a
background, and `ActivationKind.Background` in `Deactivated` / `Activated` is what flips `isInBackground`:

| Head | Who raises Background |
|---|---|
| Browser | Avalonia's `BrowserActivatableLifetime`, from `document.visibilitychange` - verified in the `avalonia.js` of Avalonia.Browser 12.1.2. **No script of ours is needed**, and none was written. |
| Android, iOS | the platform lifetimes, when the app goes behind another |
| macOS | Avalonia.Native, when the user hides the app (not a window) |
| Windows, Linux | nobody; the windows alone decide |

The browser question the developer asked - can JavaScript tell us "tab invisible" and "window minimized" - has
one answer: the Page Visibility API, and Avalonia already listens to it. `document.visibilityState` turns
`hidden` for a background tab and for a minimized window; it does **not** turn hidden for a window merely covered
by another, which is the same limit as on the desktop. There is no separate browser event for minimizing.

## The hub connection: `ConnectionGate`

`UpdateService.StartAsync` builds the `HubConnection`, registers every handler **before** opening it (the server
greets a new connection with every device it has, and a message for an unregistered method is dropped), and hands
`StartAsync` / `StopAsync` of the connection to a `ConnectionGate` together with `CatchUpAsync`. The gate:

- connects at once if something is visible - and the first connect's exception goes to the caller, into the
  same error handling as before; started while nothing is visible, it waits;
- drops the connection once **nothing** has been visible for `DefaultDisconnectDelay` (5 s), so a window
  minimized and restored a moment later costs nothing;
- reconnects the moment something is visible again and runs the catch-up; a failed reconnect is retried every
  `DefaultRetryDelay` (10 s) while something is visible;
- is told by `UpdateService.OnClosed` when the connection died on its own (`Closed` with an error, which is
  SignalR's automatic reconnect having given up) and reopens it the same way - before this, such a client stayed
  dead until logout;
- runs everything on one worker loop woken through a one-slot channel, so a connect never runs beside a
  disconnect, and a wait is cancelled through a token rather than `WaitAsync` (an abandoned channel read would
  swallow the next wake-up).

`StopAsync` of the gate only stops following; the update service disposes the connection itself afterwards.
`ConnectionGateTests` drive all of it with counters and a `FakeVisibilityService`, with 150 ms delays.

### What the catch-up fetches, and why not everything

| Device | How the server pushes | On reconnect |
|---|---|---|
| Gen24 inverters, Fritz!Box devices | the whole device on every poll, and the whole of every device as the greeting of a new connection | nothing to do |
| Wattpilot | a `WattPilotUpdate` delta when a property changes | `GET` all Wattpilots |
| Toshiba air conditioners | the whole device, but only when something changes | `GET` all Toshiba devices |
| Price data (`EnergyChartData`) | the whole span, a few times a day when it changes | `GET` the live data |

The fetched objects go through **the same handlers a push goes through** (`OnWattPilotUpdate`,
`OnToshibaHvacUpdate`, `OnEnergyChartData`), so the instances the controls are bound to keep their identity and
take the values in place. A guest fetches nothing: the server answers 403 for all three and a guest sees the
inverters only. The same catch-up runs after SignalR's own `Reconnected`.

## The views: `ViewVisibility.Follow` and `ViewModelBase.IsShown`

A view that reacts to the update service beyond plain bindings calls `ViewVisibility.Follow(this)` once in its
constructor. From then on `IsShown` of its `DataContext` - if that is a `ViewModelBase` - says whether the view is
attached to a top level the user can see; detached is not shown, whatever the window does, which is what a page
the browser head has navigated away from is. The view model reacts through `WhenShown(work)`: the work runs now
if shown, else once when shown again, last one wins (see [[ViewModelBase.Features]]).

| View | Follows | While not shown |
|---|---|---|
| `PowerFlowView` / `PowerFlowViewModel` | `Follow`; the view also stops and restarts its animation frames on `IsShown` | no snapshot is built, no frame runs; one snapshot when back |
| `HouseControl` / `HouseViewModel` (singleton) | `Follow` on the control | the figures are not worked out; once when back |
| `DashboardView` | `Follow`; skips `OnSitePowerFlowUpdated`, recolors on `IsShown` | the colors are left alone; worked out once when back |
| `EnergyChartView` / `EnergyChartViewModel` | `Follow` | a push does not rebuild the chart; rebuilt once when back |

**The detail pages are deliberately not gated.** Their gauges are plain bindings to the devices the update
service writes into; a binding into a minimized window costs a property read, and the developer accepted "stay
subscribed" as long as the reaction is cheap. Gate a view only where it does work of its own on every report.

`ViewVisibility.Follow` does nothing without a `VisibilityService` in the container (the designer, a test that
did not register one), and `IsShown` then stays true, so nothing changes for a view model without such a view.
`PowerFlowViewTests` registers the service and minimizes the page's window to see the snapshot stand still.

## What did not change, on purpose

- The update service still writes every push into the device models, whatever is visible, as long as the
  connection is up. That is what makes the catch-up unnecessary for the pushed-whole devices and keeps every
  binding truthful the moment a window is restored.
- `MainViewModel` knows nothing of any of this; login and logout call `StartAsync` / `StopAsync` as before.
- The server is untouched. Its greeting on connect was already what a reconnecting client needs for the
  pushed-whole devices.
