---
paths:
  - HomeAutomationClient/HomeAutomationClient/App.axaml
  - HomeAutomationClient/HomeAutomationClient/Styles/Controls.axaml
  - HomeAutomationClient/HomeAutomationClient/Styles/Gauges.axaml
  - HomeAutomationClient/HomeAutomationClient/Styles/HeaderedContentControls.axaml
  - HomeAutomationClient/HomeAutomationClient/Controls/DeviceControlBase.cs
  - HomeAutomationClient/HomeAutomationClient/Controls/InverterControl.axaml
  - HomeAutomationClient/HomeAutomationClient/Controls/SmartMeterControl.axaml
  - HomeAutomationClient/HomeAutomationClient/Controls/SmartMeterControl.axaml.cs
  - HomeAutomationClient/HomeAutomationClient/Controls/WattPilotControl.axaml
  - HomeAutomationClient/HomeAutomationClient/Controls/PowerConsumer.axaml
  - HomeAutomationClient/HomeAutomationClient/Controls/ToshibaHvacControl.axaml
  - HomeAutomationClient/HomeAutomationClient/Converters/Gen24Converters.cs
  - HomeAutomationServerTests/UnitTests/Avalonia/DevicePaletteTests.cs
---

# The palette of the dashboard and the detail views (Avalonia client)

## One place for every colour

The colours of the device cards, the gauge tiles, the power arrows and the page ground are in the two
`ThemeDictionaries` of `App.axaml`, and nowhere else. A control or converter asks for a key; it never carries a
colour of its own. That is not tidiness: **a second copy of the palette is a copy that wins.** Until 2026-10-31
`Models/Gen24/StatusColor.cs` held the WPF colours a second time, and `SmartMeterControl.axaml` bound its card to
them through `Gen24Status2Brush` while the code behind set the same `BackgroundProvider.Background` from the
palette. The binding won. Nobody saw it for as long as both copies held the same colours; it showed the day the
light palette changed, as the one card that stayed beige. `StatusColor`, `Gen24Status2Brush`,
`Gen24Status2PanelBrush` and the unused `PowerStatus2Brush` were deleted then. Their WPF originals in
`FroniusMonitor` are the WPF app's and stay.

A device card (`DeviceControlBase`) takes its state colours from `Styles/Controls.axaml`, which sets `Outer*` and
`Inner*` from `DeviceOuterBackground*` and `DeviceInnerBackground*`; `ChangeOuter`/`ChangeInner` run whenever one
of them or the device's state changes. Outer is the card, Inner the panel inside it. The gauges of the detail views
get theirs from `InverterBackgroundColor` - the outer keys for running, fault, warning and startup, the inner one
for any other state, as it always was.

**The keys are spelled `…Startup`, both of them.** The outer one was `DeviceOuterBackgroundStartUp` while
`InverterBackgroundColor` asked for `…Startup`; `ApplicationExtensions.GetResource` returns null for a key that is
not there, so every gauge of a detail view lost its background while the device started up, with no error
anywhere. `DevicePaletteTests` holds both defects.

## The light variant (2026-10-31)

Redone on the developer's request; the WPF colours (`AntiqueWhite`, `Cornsilk`, `BurlyWood`, `LightSalmon`, `Blue`
units, `DimGray` frames) had come over unchanged and the dark variant had been designed since. The light one now
speaks the language of the power flow page:

- **A grey ground, white cards.** `BackgroundBrush` (`#EEF1F5`) is the ground of every page - the style
  `:is(ContentPage)` in `Styles/Controls.axaml`; `:is`, because a page is a subclass - and of the group box
  captions, which have to match what they sit on (`HeaderedContentControls.axaml`). Fluent paints a page white or
  black by itself. `PowerFlowView` sets its own `FlowPageBackground` on itself and wins.
- **Running is the neutral card.** White outside, `#F6F8FB` inside. Only what needs attention is tinted: warning
  amber, fault red, startup blue, each a light card with a lighter panel. Any other state is a light grey.
- **The frame** of a device card and of a gauge tile is one style, `Border.DeviceFrame`, with `DeviceBorder`,
  `DeviceCornerRadius` and - in the gauge template only - `GaugeTileMargin` from the palette: rounded, with a gap,
  so that the tiles are cards and not a table. `ClipToBounds` is what makes a card's square panels follow the
  rounded frame. Do not set `BorderBrush` or `CornerRadius` on the Border in `DetailsGauge`: a value set in a
  template outranks a style, and the frame would stop following the theme.
- The tabs of a card are grey, the selected one a light blue; the units are the slate of the labels, the needle
  the orange of the dark variant; the battery and the on/off switch have colours of the same family.

**The dark variant did not change**, on purpose: `DeviceBorder` is `DimGray`, radius and gap are 0, and
`BackgroundBrush` is the black Fluent painted a page with. Rendered before and after, the dark pages differed in a
few hundred pixels of anti-aliasing at the gauge ticks, by at most 28 of 255 - `ClipToBounds` changes the path
Skia draws them on.

## The kinds of power

Solar yellow, battery green, grid grey mean the same on the dashboard (`PowerFlow*`, the arrows and the colours
`DashboardView.UpdatePowerFlowColors` mixes) and on the power flow page (`Flow*`). **In the light variant they are
written down once**, as `SolarColor`, `BatteryColor` and `GridColor`, and both sets point to them with
`StaticResource` - which resolves inside a theme dictionary. They are the colours the power flow page already had,
because it uses them for lines and figures on white, where a lighter yellow would not read; the dashboard's arrows
became darker by it. **In the dark variant the two sets stay apart**: `Flow*` is the design the power flow page was
drawn from, `PowerFlow*` the dashboard's own look, and the developer has chosen neither over the other.

## Looking at it

There is no designer in the cloud, but the real pages can be rendered. A throwaway console project in the
scratchpad, never in the repo:

- References `HomeAutomationClient.csproj` plus `Avalonia.Headless` and `Avalonia.Skia` at the Avalonia version,
  and sets `AssemblyName` to `HomeAutomationServerTests`: `IUpdateService` has an internal event, and that is the
  name the client grants its internals to.
- `ProbeApp : App` with an empty `OnFrameworkInitializationCompleted`, so no container and no main window are
  built; `App.Initialize` still loads `App.axaml` with all styles and palettes. Call the private
  `App.SetAccentColor` through reflection for the browser's accent colour.
- `AppBuilder.Configure<ProbeApp>().UseSkia().UseHeadless(new() { UseHeadlessDrawing = false }).WithInterFont()
  .SetupWithoutStarting()`, set `RequestedThemeVariant` **before** anything is built, one process per variant.
- A container as in `App`, with a hand-written `IUpdateService` that holds a `Gen24System`, a meter and a few
  FRITZ!Box devices; the page in a `Window`, `Show`, a few rounds of `Dispatcher.UIThread.RunJobs()` and
  `AvaloniaHeadlessPlatform.ForceRenderTimerTick()`, then `window.CaptureRenderedFrame()!.Save(path)`.

What such a picture shows that the real app does not: a red firmware warning on the inverter card and raw keys
like `INVERTER.DEVICESTATE.STATE_RUNNING`, because the fake system has no firmware version and no Gen24
localization was loaded.
