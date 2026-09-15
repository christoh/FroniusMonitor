---
paths:
  - HomeAutomationClient/HomeAutomationClient/Controls/ZoomBox.cs
  - HomeAutomationClient/HomeAutomationClient/Views/DashboardView.axaml
  - HomeAutomationClient/HomeAutomationClient/Views/InverterDetailsView.axaml
  - HomeAutomationClient/HomeAutomationClient/Views/BatteryDetailsView.axaml
  - HomeAutomationClient/HomeAutomationClient/Views/SmartMeterDetailsView.axaml
  - HomeAutomationClient/HomeAutomationClient/Views/WattPilotDetailsView.axaml
  - HomeAutomationClient/HomeAutomationClient/Views/MainView.axaml
  - HomeAutomationClient/HomeAutomationClient/Views/ChildWindow.axaml
---

# Zooming a view (Avalonia)

Since 2026-09-15 the user can make the gauges and the power consumers bigger or smaller: **Ctrl and the wheel**,
**Ctrl with plus or minus**, **Ctrl with zero** to go back to normal, and **two fingers** on a touch screen.
`Controls/ZoomBox.cs` is all of it.

## Two halves, and why they are separate

| Part | Where it goes | What it does |
|---|---|---|
| `ZoomBox` | around the part of a view that should zoom | scales it, and takes the pinch done on it |
| `ZoomBox.IsScope` | on a view's root, or on a window | marks the window whose gestures zoom; the handlers go on its top level |

**The pointer is hardly ever over what should zoom.** It is over a scroll viewer, a group box, or the empty space
beside one, and the developer asked for the gesture to work in the whole window. So the input is taken on a scope
and the scale is applied somewhere else entirely; a `ZoomBox` handles no input at all.

## Plugging it into a new view

```xml
<ContentPage … >          <!-- the scope is already on MainView and on ChildWindow, so a page needs none -->
    <ScrollViewer>
        <c:ZoomBox>
            <WrapPanel … />
        </c:ZoomBox>
    </ScrollViewer>
</ContentPage>
```

That is the whole job. Where the scopes are today:

- **`MainView`**, which covers the menu bar, the content and the switches at the bottom - the whole main window on
  every head, and on a head without windows it covers the detail pages too, because they are shown inside it.
- **`ChildWindow`**, so each detail page the desktop opens in a window of its own is covered by that window. It
  carries dialogs as well; they contain no `ZoomBox`, and a scope that finds none leaves the event alone, so their
  scroll viewers still scroll.

Several boxes in one scope move **together** for the wheel and the keyboard, which is what "everything in the wrap
panel" means: the four detail views each have one around the wrap panel of gauge group boxes, the dashboard one
around the power consumers.

## Pinch is the exception: it zooms only what the fingers are on

The wheel and the keyboard are window-wide because the pointer is hardly ever over the gauges. A pinch is the
opposite: it is done **to** something, with the fingers on it. So the `PinchGestureRecognizer` and its two handlers
sit on the `ZoomBox` itself, not on the top level, and a pinch

- zooms the box the fingers are on and **no other box in the window**,
- does nothing at all when the fingers are somewhere else, where it may well be meant as a scroll gesture.

A pinch reports how far apart the fingers are **relative to where they started**, not a step. The box therefore
remembers the scale the gesture began at and multiplies that by what it is told, so bringing the fingers back to
where they started undoes the zoom exactly; `PinchEnded` drops the baseline, so the next gesture starts from where
this one left off. Multiplying event by event instead would square the gesture.

In Avalonia 12 the gesture events are on `InputElement` (`PinchEvent`, `PinchEndedEvent`) - `Avalonia.Input.Gestures`,
where they lived in 11, is **internal** now and will not compile.

## Why a layout transform and not a render transform

`ZoomBox` derives from `LayoutTransformControl`. What is inside is **laid out** at the scaled size, so a wrap
panel re-wraps into fewer columns, the scroll viewer around it knows how much there is to scroll, and nothing is
clipped. A `RenderTransform` would paint the gauges bigger and leave the layout believing they were the old size -
the gauges would overlap their neighbours and the bottom of the group would be unreachable.

## The details that are easy to get wrong

- **The handlers go on the top level, not on the scope.** This is the one that cost a round. A routed event only
  reaches the elements on its route, and neither route goes where this has to work: a key tunnels from the top
  level to whatever has the **focus**, so a scope inside the window is skipped when the focus is elsewhere or
  nowhere at all, and the wheel goes to what is under the **pointer**, which is nothing where the pointer is over
  a panel with no background. The top level is on both routes, always. The scope property therefore marks a window
  that zooms; the boxes are looked for from that top level down.
- **The handlers tunnel** (`RoutingStrategies.Tunnel`), which is Avalonia's equivalent of WPF's
  `OnPreviewKeyDown`. Bubbling, the scroll viewer under the pointer turns the wheel into scrolling before the
  scope ever sees it, and the focused text box turns the key into text. Measured: with the focus in a text box,
  Ctrl and plus zoom and the box keeps the text it had, while plain typing still reaches it.
- **`Handled` is set only when a box was found.** That is what lets the same scope sit on a window with nothing to
  zoom without breaking its scrolling.
- **Both the number row and the number pad** are accepted (`OemPlus`/`Add`, `OemMinus`/`Subtract`, `D0`/`NumPad0`),
  because the layouts of the languages this app speaks do not agree on where plus and minus live.
- **The steps are geometric and both default to 1.1** - `WheelStep` and `KeyStep`, styled properties a view may
  set for itself - so a notch or a press back undoes one forward exactly, wherever the scale happens to be. A step
  that is not a finite positive number is refused and the default used, because a zero would be multiplied and
  divided by. The scale is clamped to 0.25..4 by a coercion, and narrowing the limits re-coerces what is set.
- **The zoom is per box and per session.** Nothing is stored; a detail window opened again starts at 1.

## Verified, so you do not have to measure again

A headless probe drives real windows with `MouseWheel` and `KeyPress`:

- Ctrl and the wheel with the pointer **over another control entirely** zoom the box, and the wheel without Ctrl
  does not.
- Both work with the scope on a view **inside** the window, which is how the app marks it, with nothing focused -
  the case that failed while the handlers sat on the scope element.
- The bounds of the box grow while its child keeps its own size - a layout change, not a painted one - and a
  scroll viewer around it gets more to scroll.
- Plus, minus and zero on both the number row and the number pad; plus without Ctrl does nothing.
- A wheel step and a key step set on the box are used, independently of each other, and a step of zero falls back.
- 60 notches in stop at the maximum, 120 out at the minimum.
- Two boxes in one scope stay in step.
- A scope with no box still scrolls.

The pinch checks synthesize the routed events, because the headless platform has no two-finger input: the
baseline, the arithmetic, the limits and the restriction are measured, the recognizer below them is not.

**Not measured:**

- How the real views look at a given scale. Where the box sits in each view is a judgement call that the developer
  checks by running the app.
- Whether real fingers reach the recognizer through the scroll viewer around the gauges, which wants them for
  panning. Only a touch screen can answer that; nothing here can.
- The trackpad. A precision touchpad pinch is usually delivered as Ctrl and the wheel, which already works, and
  Avalonia also offers `PointerTouchPadGestureMagnifyEvent` for the platforms that report it natively. Neither is
  handled explicitly.
