---
paths:
  - HomeAutomationClient/HomeAutomationClient/Controls/TemplatedDecorator.cs
  - HomeAutomationClient/HomeAutomationClient/Controls/LazyPanel.cs
  - HomeAutomationClient/HomeAutomationClient/Controls/DeferredContent.cs
  - HomeAutomationClient/HomeAutomationClient/Controls/Gauge.cs
  - HomeAutomationClient/HomeAutomationClient/Controls/InverterControl.axaml
  - HomeAutomationClient/HomeAutomationClient/Controls/SmartMeterControl.axaml
  - HomeAutomationClient/HomeAutomationClient/Controls/WattPilotControl.axaml
  - HomeAutomationClient/HomeAutomationClient/Views/DashboardView.axaml
  - HomeAutomationClient/HomeAutomationClient/ViewModels/MainViewModel.cs
  - HomeAutomationClient/HomeAutomationClient.Browser/HomeAutomationClient.Browser.csproj
  - HomeAutomationServerTests/UnitTests/Avalonia/DeferredBuildingTests.cs
---

# Loading the dashboard

## What it cost, and why (measured 2026-10-31)

`MainViewModel.ShowDashboardView` sets the busy text, waits 100 ms and builds `IoC.Get<DashboardView>()` on the
UI thread. Until 2026-10-31 that was one piece of work: the page, and in its first layout pass every card from
the data templates. The developer's installation - 2 inverters, 1 Wattpilot, 4 Toshiba air conditioners, 17
Fritz!Box devices - came to **2943 visuals**, among them **253 gauges of which 243 were not visible**, 611
text blocks, 68 sliders, 87 buttons, some 2400 bindings. Measured in the headless probe (x64, JIT, Release):

| | before | after |
|---|---|---|
| `new DashboardView()` | 470-630 ms | 350-420 ms |
| attach to the tree | 110-160 ms | 65-70 ms |
| first layout pass, until the first frame is drawn | 1.7-2.8 s | **0.65-0.70 s** (948 visuals) |
| the consumer cards, in turns after that frame | - | 0.8 s in all |
| **UI thread held before anything is drawn** | **2.4-3.6 s** | **1.1 s** |
| gauges built with the page | 253 | 10 |
| first-chance exceptions while building | 19 | 2 |

What it was not: exceptions (19, cheap), the needle animations (switched off, nothing changed), the network
(everything is fetched before the view exists). It was mass, built synchronously - and, in the browser,
interpreted: the browser head had no `RunAOTCompilation`, so all of it ran in Mono's interpreter. WebAssembly
could not be measured in the cloud; the factor is several times the desktop's.

Three places held most of it: the three mode cards built **all** their panels and switched them with `IsVisible`
(`InverterControl` 25 panels, 248 visuals, 91 gauges; `SmartMeterControl` 14, 159, 48; `WattPilotControl` 13,
183, 29), the 22 consumer cards were built from the `ItemsControl`'s data templates in the same layout pass as the
page, and every one of the 253 gauges started a one second animation for its first value.

## What was done

### The panels of a mode card are built when they are shown: `LazyPanel`

`TemplatedDecorator` is a `Decorator` whose child is a `Template` (`ITemplate<Control>`, the `[Content]`), built
by `BuildChild()` once. `LazyPanel` builds it in `ApplyTemplate()`, which layout calls on every control it
measures - and it measures only what is visible, so a hidden panel costs nothing until its mode is first chosen.
This is the moment `TemplatedControl` applies its own template too; adding the child there is what Avalonia does
itself. In the cards every panel went from

```xml
<Grid x:Name="AcPhaseVoltage" IsVisible="{Binding Mode, Converter={co:InverterDisplayMode2Bool AcPhaseVoltage}}" …>
```

to

```xml
<c:LazyPanel IsVisible="{Binding Mode, Converter={co:InverterDisplayMode2Bool AcPhaseVoltage}}">
    <Template x:DataType="c:InverterControl">
        <Grid x:Name="AcPhaseVoltage" …>
```

- **`x:DataType` is said again on the `Template`.** A template starts a compiled binding scope of its own; the
  type the Viewbox's `DataContext` binding gives the rest of the file does not reach inside. The child inherits
  the data context at runtime as any child does, so the bindings find the card.
- **Names inside a template are in the template's own name scope.** An `ElementName` binding that stays inside
  its panel (`DcPowerGauge`, `PowerActiveGauge`, `CurrentGauge` and the like bind to their own root) works; one
  that reached across panels would not, and none did. The code behind names only `BackgroundProvider` and
  `InnerBackgroundProvider`, which are outside the panels.
- `StaticResource` inside a panel - the `ControlTheme`s of `SmartMeterControl` based on `PowerLinearGaugeStyle` -
  resolves as before; the renders are identical.
- The first click on a mode builds its panel, a few milliseconds; afterwards the panel stays, and switching back
  is as quick as it always was.

The rewrite was mechanical (a script wrapped the 52 panels) and was checked by rendering **every mode of every
card with the old code and the new**: 44 of 52 pictures identical, and the other 8 differ between two renders of
the old code by exactly the same pixels - the bar position of a zero-duration animation - so they are the old
code's noise, not a difference.

### The consumer cards are built after the page: `DeferredContent`

`DeferredContent` is the other `TemplatedDecorator`. It enqueues itself in a static queue when attached to the
visual tree and builds in **turns posted at `DispatcherPriority.Background`** - below input, layout and render,
so a frame is drawn and input is handled between two turns - for `TurnBudget` (25 ms) each, then posts the next.
Something detached before its turn (the page was left) is skipped and enqueues itself again when it comes back;
something already built does not enqueue. `DashboardView.axaml` wraps each of its three data templates in one.

What the user sees: the page with its inverters, meter and house at once, then the cards arriving below, a few
per frame, while the busy spinner keeps turning. The page's `BusyText` is cleared when `MainViewContent` is set,
as before; the cards arriving later need no busy state.

**The probe's "frame 0" overstates this.** `Dispatcher.UIThread.RunJobs()` runs the Background turns too, so a
plain pump builds everything in one call; `Timing` in the probe runs the first frames with
`RunJobs(DispatcherPriority.Input)` to see what the app draws first.

### A gauge nobody can see takes its value: `Gauge.SetValue`

`sKipAnimation |= !IsLoaded || !IsEffectivelyVisible;` - before the gauge is loaded, every binding that delivers
a value would start a one second animation of its own, and a gauge in a hidden panel shows whatever value it has
when the panel comes back. A value arriving while the gauge is on screen animates as it always did. See
[[Gauges]].

### The browser head is compiled ahead of time

`<RunAOTCompilation>true</RunAOTCompilation>` in `HomeAutomationClient.Browser.csproj`. It applies to a publish,
which is what the Dockerfile's client builder stage does; the `wasm-tools` workload it needs is installed there
already. The publish takes longer and the download is bigger. A plain build or `dotnet run` does not compile
ahead of time.

**AOT needs IL trimming**: the .NET 10 WebAssembly SDK stops the publish with "AOT is not supported without IL
trimming", and the browser head had `PublishTrimmed=false` since its first commit. It trims since 2026-10-31,
with `TrimMode=partial` like the mobile heads: only the assemblies that declare themselves trimmable are trimmed
(the framework, Avalonia, SkiaSharp, HarfBuzzSharp, the SignalR client, CommunityToolkit.Mvvm), and Fronius, the
client, ScottPlot and the DataGrid stay whole - which is what the reflection in our code counts on, see
[[PlatformHeads.Lifecycle]]. **Newtonsoft is not among them: nothing but `HomeAutomationServerTests` references
it any more** (the oracle of `JsonExtensionsTests`); the comments in `Fronius` that name it are history. With
trimming on, System.Text.Json switches its reflection-based serialization off by default, and the web client and
the hub protocol serialize our models with it, so `JsonSerializerIsReflectionEnabledByDefault` is set back to
true in the browser project. The mobile heads, which trim the same way, have never run (known gap in
[[PlatformHeads.Lifecycle]]) and will want the same switch.

### `Run.Text` binds two way by default

`Run.TextProperty`'s default binding mode is **TwoWay** (`TextBlock.Text` is OneWay), so a `<Run Text="{Binding
Number, StringFormat=N1}">` writes its formatted text back into the model: `'4,610.0' is not a valid value for
Double` as an exception per update, swallowed by Avalonia, and where the text does parse - no thousands separator -
the model property silently takes the rounded value. Every `Run` that shows a number has `Mode=OneWay` since
2026-10-31; write new ones that way. The two exceptions still thrown while the dashboard builds are
`Gen24PowerFlow`'s static constructor asking `IoC.TryGet` for an interface, once per process, in `Fronius`.

## Measuring it again

A throwaway console project in the scratchpad, never in the repo, as described in [[Theme.Palette]] ("Looking at
it"), with a `Timing` mode: build the real `DashboardView` with a fake `IUpdateService` shaped like the
installation, `Stopwatch` around the constructor, `window.Content = view`, and the first `RunJobs`, count the
visuals with `GetVisualDescendants`, count exceptions with `AppDomain.CurrentDomain.FirstChanceException`.
`dotnet-trace collect` plus `dotnet-trace convert --format speedscope` and a script over the JSON gives the call
tree; `dotnet-trace report topN` alone misleads, because the first-chance exceptions split the samples into
"Activity ExceptionHandling" nodes that have to be collapsed. Two renders of the old code differ in the gauges
when the needle animation is on, so compare renders with `AnimationDuration` set to zero by a style, and judge a
difference against two renders of the same code. A `git worktree` of the old commit gives the old code a probe of
its own.

## The trimmed AOT publish, seen through once (2026-10-31, in the cloud)

Built through the Dockerfile's client stage with `docker buildx bake … --set client.platform=linux/amd64 --load`
and the proxy override of the session: the publish step took 12.5 minutes (about 100 s to restore, build and
trim, the AOT toolchain the rest - see below), with 274 `IL2026` trim warnings - the compiled XAML's setters of the string properties of
the settings view models, the two JSON calls of `Browser/Platform/Cache.cs`, and `IL2104` for the DataGrid,
ScottPlot and `Avalonia.DesignerSupport`, none of which is trimmed. `_framework` is 105 MB on disk and 14.1 MB
as the brotli files nginx serves, of which `dotnet.native.wasm` is 52 MB raw and 10.2 MB brotli; the image's
content is 66 MB. Loaded in headless Chromium through Playwright, the app had its canvas after 3.2 s and the
login dialog on screen, with no page error.

Two things the first attempt taught: **the switch has to be checked in the published app**, with
`grep -o '"System.Text.Json.JsonSerializer.IsReflectionEnabledByDefault": [a-z]*' _framework/dotnet.js` -
the first publish carried `false`, because the edit that set it had died with the shell it ran in, and the app
died on its first JSON before Avalonia was up. And the client image's nginx listens on `[::]:42741` as well,
which the cloud sandbox cannot bind (no IPv6): to run the image there, mount a `default.conf` without that line
over `/etc/nginx/http.d/default.conf`.

### Where the 12.5 minutes go, and why the machine looks idle

The developer saw the AOT publish run long on little CPU. The timestamps of the build log (4 cores) say why:

| Phase | Time | In parallel? |
|---|---|---|
| restore, build, ILLink | 0-102 s | mostly |
| `mono-aot-cross`, IL to `.bc`, 100 assemblies | 102-184 s | yes, but 99 are done after 18 s; `aot-instances.dll` then runs alone for 62 s |
| LLVM `-O2`, `.bc` to `.o`, one per assembly | 184-419 s | yes: 634 s of CPU in 235 s, 2.7 cores on average; the last 85 s are `aot-instances` (137 s, and last in the queue) alone |
| `wasm-ld` | 420-423 s | - |
| `wasm-opt -O2` on the linked 52 MB module | 423-749 s | one process; Binaryen spreads only function-local passes over threads, the whole-module ones are serial |

About half the time is one core, which on a machine with many cores reads as an idle build. Both serial parts
are built in: with dedup (the default) every generic instantiation of every assembly goes once into
`aot-instances.dll`, which can only be compiled after all the others and is the biggest; and `wasm-opt` is
Emscripten's whole-module optimizer when linking at `-O2`. GitHub's runner does the whole workflow in about 8
minutes.

**The link stays at `-O2`** - the developer's decision of 2026-10-31. `EmccLinkOptimizationFlag=-O1` would skip
the 5.4 minutes of `wasm-opt` (Emscripten runs the Binaryen optimizer from `-O2` on), and it was offered and
declined; do not propose it again. Turning dedup off (`WasmDedup`) would spread `aot-instances` over the
assemblies but duplicate the generics into each of them; not proposed either.

What would save time without touching the code that comes out: the client builder stage starts with `COPY . .`,
and `.dockerignore` keeps out only `bin` and `obj`, so any change anywhere in the repository - the server, a test,
a memory document - runs the whole AOT publish again in a local build; and the workflow builds without a layer
cache, so on GitHub every push to master runs it regardless. Copying only what the client needs into that stage,
and a `gha` cache in the workflow, would let a server-only change reuse the published client. Not done.

## Not done, and known

- The 4 Toshiba cards are 217 visuals each (16 buttons, 10 menu flyouts) and the dearest of the consumer cards at
  35-80 ms; they are deferred, not slimmed.
- The detail views build all their gauges at once - `InverterDetailsView` 138 - but open one at a time, on
  request; `LazyPanel` would serve their gauge groups too, should it be wanted.
- `HouseControl` (296 visuals) and `SmartMeterControl` are part of the page and built with it.
- The WebAssembly numbers are unmeasured in the cloud. What the developer saw after the rollout of 2026-10-31
  (the `548eb111…` image on amazon2): the `.wasm.br` download takes about a second, the dashboard is much
  quicker, and the whole app is much more responsive - "das hat viel gebracht".
