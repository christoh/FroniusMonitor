---
paths:
  - HomeAutomationClient/HomeAutomationClient/Controls/Gauge.cs
  - HomeAutomationClient/HomeAutomationClient/Controls/HalfCircleGauge.axaml
  - HomeAutomationClient/HomeAutomationClient/Controls/HalfCircleGauge.axaml.cs
  - HomeAutomationClient/HomeAutomationClient/Controls/LinaerGauge.axaml
  - HomeAutomationClient/HomeAutomationClient/Controls/LinaerGauge.axaml.cs
  - HomeAutomationClient/HomeAutomationClient/Styles/Gauges.axaml
  - FroniusMonitor/Controls/Gauge.cs
  - FroniusMonitor/Wpf/Resources/HalfCircleGauge.xaml
  - FroniusMonitor/Wpf/Resources/HalfCircleGauge.xaml.cs
  - FroniusMonitor/Wpf/Resources/LinearGauge.xaml
  - FroniusMonitor/Wpf/Resources/LinearGauge.xaml.cs
---

# The gauges, in both apps

Two implementations of the same idea, and they are kept alike on purpose: the Avalonia client's
`Controls/Gauge` with `HalfCircleGauge` and `LinearGauge` deriving from it, and the WPF app's
`Controls/Gauge : ProgressBar` with two control templates in `Wpf/Resources`. A property that exists in one is
called the same in the other, so that a change made in one app can be found in the other.

## The needle and the read-out are two different things

**`DialShowsAbsoluteValue`** (both apps, 2026-09-19) puts the needle - or the bar - at `|value|` while the
read-out keeps the sign. It exists for **cos(phi)**, where the sign says which way the reactive power flows and
the dial is about how good the power factor is: -0.998 and +0.998 are the same quality, and a needle crossing the
whole scale when the sign flips reports a change that did not happen.

Where a gauge is switched to it, the scale goes with it: **0 to 1 with `LowIsBad`**, and no `Origin` in the middle
- with the sign gone there is no lower half to point at, and 1 is the good end rather than both ends being good.
In the WPF app the two linear cos(phi) styles have to set `Origin` back to 0 by hand, because the styles they are
based on are bipolar power gauges.

Everything that shows cos(phi) carries it: in the Avalonia client the groups of `InverterDetailsView`,
`SmartMeterDetailsView` and `WattPilotDetailsView` and the linear gauges of `InverterControl` and
`SmartMeterControl`; in the WPF app the same five places.

**The read-out never sees it.** It is built from the value: `Gauge2Text` in both apps for the half circle, and
`SetValueTextBlock` for the WPF linear gauge, which reads `gauge.Value` unless `ShowPercent` is on.

## The two apps are alike, but not the same app

Where they differ, it is a decision and not a gap. The delta frequency gauge hides itself in both, but **the two
Δ voltage groups of the inverter window hide themselves in the Avalonia client only** (2026-09-19): the WPF menu
has a switch per group, so the user turns those off there, while the delta frequency gauge has no switch of its
own in either app. See [[InverterDetailsView.Lifecycle]].

## One place works out where the needle stands

Both apps compute the fraction of the scale **once**, and both templates ask for it:

| | Where |
|---|---|
| Avalonia | `Gauge.SetValue`, which drives `AnimatedValue`; the subclasses draw from that |
| WPF | `Gauge.RelativeValue`, read by `HalfCircleGauge.SetValue` and `LinearGauge.SetValueTextBlock` |

The WPF pair each had the formula written out until 2026-09-19, which is why `DialShowsAbsoluteValue` came with
`RelativeValue`: a switch that only half the gauges obey is worse than no switch. **Do not write the clamp out
again in a template.**

## What a gauge is told at runtime, and what only by a style

`ColorAllTicks` changes while the app runs - a menu item in WPF, a switch in the main view in Avalonia - so both
apps watch it: WPF with a `DependencyPropertyDescriptor`, Avalonia through `OnPropertyChanged` and an application
resource (see [[DialogSystem.Lifecycle]] for why a resource and not a binding). `DialShowsAbsoluteValue` is set by
a style and never changes, so the WPF templates deliberately do **not** watch it; the Avalonia one recomputes on
change anyway, because its base class can do it in one line.

## Compiling the WPF app from the cloud

`dotnet build FroniusMonitor/FroniusMonitor.csproj -c Debug -p:EnableWindowsTargeting=true` **works on Linux**,
verified on 2026-09-19, and the markup compiler runs with it: a `Setter` for a property that does not exist fails
with `MC4005`, which is how these XAML changes were checked without Windows. It only compiles - WPF cannot run
there, and `FroniusMonitorTests` still refuses with `FMT001` unless given `-p:VerifyOnLinux=true`.
