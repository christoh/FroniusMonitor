---
paths:
  - HomeAutomationClient/HomeAutomationClient/Misc/PlatformStartup.cs
  - HomeAutomationClient/HomeAutomationClient/FileCache.cs
  - HomeAutomationClient/HomeAutomationClient/ICache.cs
  - HomeAutomationClient/HomeAutomationClient/App.axaml.cs
  - HomeAutomationClient/HomeAutomationClient/App.axaml
  - HomeAutomationClient/HomeAutomationClient.Desktop/**
  - HomeAutomationClient/HomeAutomationClient.Browser/**
  - HomeAutomationClient/HomeAutomationClient.Android/**
  - HomeAutomationClient/HomeAutomationClient.iOS/**
  - Fronius/Localization/**
  - HomeAutomationServer/Program.cs
  - FroniusMonitor/ViewModels/SettingsViewModel.cs
---

# Lifecycle contract: the platform heads

`HomeAutomationClient` holds the whole application; the four head projects only start it and supply what a
platform can answer and the shared code cannot. This document says what a head owes the app, when, and how the
answer travels.

## Startup order

1. **Head startup code.** `Program.Main` on Desktop and Browser, `CustomizeAppBuilder` on Android
   (`AndroidApp`) and iOS (`AppDelegate`). Here the head
   - creates `App.ServiceCollection` and registers its `ICache`,
   - optionally sets `PlatformStartup.AccentColor`,
   - optionally registers an `IUriService` of its own - see `Navigation.Lifecycle.md`,
   - the browser head additionally seeds `CacheKeys.ApiUri` and `CacheKeys.HubUri` from the base address it is
     handed in `args[0]`, and downloads the satellite assemblies for the language of the user - both before
     Avalonia starts.
2. **`AppBuilder`** with the head's font and platform options.
3. **`App.Initialize`** loads `App.axaml`.
4. **`App.OnFrameworkInitializationCompleted`** calls `SetAccentColor()`, builds the service provider, hands it to
   `IoC`, and creates `MainWindow` (desktop lifetime) or `MainView` (single view lifetime).
5. `MainView`'s constructor starts `MainViewModel.Initialize`, which shows the login dialog.

**The rule for step 1: no Avalonia types.** Nothing is initialized yet, and on Android and iOS this code runs
while the platform is still building the activity. That is why a color arrives as
`De.Hochstaetter.Fronius.Models.HaColor` and not as an Avalonia `Color`.

**Not in this document: the address of a view.** Which view the app shows, how it is written into the address bar
of the browser, and what the other heads do instead, is `Navigation.Lifecycle.md`. Only two things here belong to
a head: registering the `IUriService` in step 1, and the base address the browser head is handed in `args[0]`,
which is `document.baseURI` **for a reason the navigation document gives**. Do not go back to `location.href`.

## What every head must provide

**An `ICache`.** `MainViewModel`, `UpdateService` and `LoginViewModel` read it through
`IoC.TryGetRegistered<ICache>()`. A head without one gets a client that fails shortly after startup - which is
exactly what Android and iOS did before they got theirs.

Everything with a file system shares `FileCache` in `HomeAutomationClient`: one `key=json` pair per line, so any
of these heads could read another's file. A head supplies **only the directory**:

```csharp
public class Cache() : FileCache(DataDirectory)
{
    private static string DataDirectory => …;
}
```

| Head | Directory | Why |
|---|---|---|
| Desktop | `LocalApplicationData` + `Hochstätter\HomeAutomationClient` | Same path since the first version. Do not move it, users have files there. |
| Android | `Application.Context.FilesDir` | The private data directory of the app. |
| iOS | `NSSearchPathDirectory.ApplicationSupportDirectory` | The data directory. It does not exist until created, which `FileCache` does. |
| Browser | none - `localStorage` through `[JSImport]` | No file system worth the name. |

**Never the cache directory,** although `ICache` sounds like it: Android and iOS delete it whenever they need
space, and the connection to the server has to survive that.

`FileCache` serializes with reflection, so the types of cached values (`HomeAutomationServerConnection`, not only
strings) must survive the trimmer. All heads currently build with a trim mode that leaves our own assemblies
alone - verified for iOS with `dotnet msbuild -p:RuntimeIdentifier=ios-arm64 -getProperty:TrimMode`, which
reports `partial`. Give `FileCache` a `JsonSerializerContext` before that changes; the compiler will not warn,
because the trim analyzer does not look into a referenced project.

## What a head may provide: the accent color

`PlatformStartup.AccentColor` is a nullable `HaColor`. Where a head detected one, `App.SetAccentColor()` writes
it into the accent palette of the Fluent theme: `SystemAccentColor` plus the six shades `SystemAccentColorLight1`
to `Light3` and `Dark1` to `Dark3`. It is the palette and not a brush of our own, because the theme paints far
more with it than the dialog title bar - the thumb of a slider, a check box, a focus rectangle. Verified: an
app level override of those keys re-resolves `SystemControlBackgroundAccentBrush` and
`SystemControlHighlightAccentBrush` immediately.

Windows delivers the six shades with the accent color and Avalonia passes them on; where we bring our own color
we derive them in HSV, with factors read off the Windows palette (see `accentShades` in `App.axaml.cs`). They
reproduce it to about one step of 255, and they scale rather than subtract, so a dark accent color does not run
into black.

**Where a head detected nothing, the app uses its own color**: `AppAccentColor`, one `Color` per theme in the
`Light` and `Dark` dictionaries of `App.axaml` (`#FFDA3B01` and `#FFFF6D3D`). Those two are the only place the
color is written down, and they reach iOS, the browser, macOS and Linux at once - none of which has an accent
color to read. Do not add a third copy to a head, a manifest or an asset catalog.

**The palette is built again on every theme change.** `SetAccentColor` writes plain colors into
`Application.Resources`, which are not theme scoped and would otherwise shadow both variants with whichever one
was current at startup - so `App.Initialize` subscribes to `ActualThemeVariantChanged` and rebuilds. Anything
else added to the palette later has to go through `SetAccentColor` for the same reason.

**On Windows nothing is touched at all,** which is what `PlatformStartup.AccentColorFollowsOs` says: Avalonia
fills the palette from the OS there and keeps it up to date while the app runs, so any override - the head's
color or `AppAccentColor` - would freeze the accent at what it was when the app started. Only the desktop head
sets the flag, and only on Windows.

| Head | Accent color |
|---|---|
| Desktop on Windows | the OS, live, through Avalonia - `AccentColorFollowsOs` is true and we write nothing |
| Desktop on macOS and Linux | `AppAccentColor` |
| Android | the wallpaper (Material You) or the app theme |
| iOS, Browser | `AppAccentColor` |

- **Desktop** detects nothing anywhere. macOS keeps its accent color in `NSColor.controlAccentColor`, out of
  reach for a plain .NET app, and Linux has none that all desktop environments agree on - hence the app color for
  both. Windows needs no detection at all, see the flag above.
- **Browser** supplies nothing, and there is no point in trying. **Do not add the detection back.** A browser
  never hands the accent color of the OS to a page. Chromium answers the CSS system color `AccentColor` with a
  built-in `rgb(0, 117, 255)` and paints even its own form controls with `accent-color: auto` in that blue,
  whatever the browser itself is themed with - measured in Chrome 151 against a Windows accent of `#DA3B01`,
  with the native check box as the counter-check. Firefox and Safari do answer truthfully, but an accent color
  that only some browsers follow was judged not worth the moving parts. An earlier version read the keyword
  through `[JSImport]` from a `wwwroot/accent.js`; both are gone.
- **Android** takes the Material You color `system_accent1_500` on API 31 and later, and the `colorAccent` of
  the app theme before that. Compile verified only.
- **iOS** supplies nothing, and there is nothing to supply: iOS has no accent color of the operating system, what
  looks like one is the tint color of the app itself. Set `PlatformStartup.AccentColor` in `AppDelegate` only for
  a color iOS itself decides; a color of ours belongs in `AppAccentColor`, where every platform without one
  already reads it.

## What the browser head must do alone: the translations

Every other head carries its satellite assemblies in the app package. The browser downloads them, so it decides
which ones a visitor pays for. `Program.LoadSatelliteAssembliesForBrowserLanguageAsync` asks `wwwroot/culture.js`
for `navigator.languages` and hands the answer to `Program.GetSatelliteCultures`, which walks that list **in the
order the user put it in** and stops at the first language we can serve:

| The user asks for | We load | Because |
|---|---|---|
| a culture we have (`de-CH`) | that one **and its neutral** (`de-CH`, `de`) | .NET falls back `de-CH` → `de` → neutral, so a string only `de` translates would turn English |
| a culture we only have neutral (`it-IT`) | the neutral one (`it`) | |
| English in any form (`en-GB`) | nothing | English **is** the neutral culture, so the user is already served |
| nothing we have (`xx`) | nothing, and the walk continues | the next language of the list gets its turn |
| nothing at all | nothing | the user sees the neutral culture |

**The cultures we can serve come from the build, not from a list in the code**, through
`culture.js/getSupportedCultures`, which reads the keys of `resources.satelliteResources` from the runtime
config. Two reasons, and the second one bites:

- A hand-kept list drifts from the `.resx` files in `Fronius/Localization`.
- **The loader of the runtime matches the cultures we pass with `===`**, and the build spells them exactly like
  the `.resx` files - `Resources.de-ch.resx` becomes the culture `de-ch`, in lower case. A list of our own said
  `de-CH`, so the loader dropped it without a word and the Swiss German translation was never downloaded, from
  the first version of this code until it was measured. Passing the keys of the build back to the loader cannot
  get that wrong. **Do not "tidy" the casing of a culture anywhere in this path.**

Matching a browser language against those keys stays case-insensitive - the browser says `de-CH`, the build says
`de-ch`, and what we hand to the loader is always the spelling of the build.

**`CurrentUICulture` is set to the culture we loaded.** The runtime derives it from the *first* language of the
browser, so for `["pt-BR", "de-DE"]` it would be `pt-BR`, the resource lookup would land on the neutral culture
and the German we just downloaded would never be read. `CurrentCulture` stays untouched: numbers and dates follow
the region of the user, not our choice of translation.

The module is imported as `JSHost.ImportAsync("culture", "../culture.js")`. **The path is relative to the .NET
runtime in `_framework`, not to the document** - `./culture.js` does not resolve. Failure is caught and logged to
the browser console; the user then gets the neutral culture instead of a broken start.

The same inventory serves the other projects, through `SupportedCultures` in `Fronius/Localization`: it reports
the satellite assemblies next to `Fronius.dll` and their neutral culture. `HomeAutomationServer` builds its
request localization from it and `FroniusMonitor` its language chooser, so a new `.resx` reaches all of them
without an edit. Only the browser cannot use it - a satellite in WebAssembly is a download, not a file next to an
assembly - which is why it asks the runtime config instead. Both answer the same question from what the build
produced, neither from a list somebody has to remember.

### Not every German word comes from our .resx files

Some of what the user reads is the wording of the **inverter**, not ours. `Gen24Service` downloads the language
files of the Gen24 (`<inverter>/…/<language>.json`) and localizes channel, status and configuration names with
them, deliberately: a setting has to read the same in our app as in the web interface of the inverter, and the
inverter is the authority on its own vocabulary. Consequences to keep in mind:

- The inverter decides which languages exist there, not us. `Gen24Service` maps our culture onto one of them -
  `gsw` asks for `de`, and `en` means "take the invariant file" rather than a localized one.
- A language we add to `Fronius/Localization` does **not** localize those names. If the inverter has no file for
  it, the mapping in `Gen24Service` needs a decision: which of the inverter's languages comes closest.
- Wording of a channel that looks wrong is a question for the inverter's language file first, not for our `.resx`.

## Verified, so you do not have to measure again

- Windows fills the palette with the real accent color (`#ffda3b01` on the machine where this was written) and
  Avalonia derives the six shades from it, so leaving Windows alone is right, not lazy.
- Overriding `SystemAccentColor` at application level re-resolves the accent brushes of the theme at once.
- `AppAccentColor` is found where `App.SetAccentColor` looks for it, per theme, and the rebuild on a theme change
  works. Measured with the desktop head, `AccentColorFollowsOs` forced to false and the variant driven from the
  code: started in `Dark` with `#ffff6d3d`, switched to `Light` and got `#ffda3b01`, switched back and got
  `#ffff6d3d` again.
- Chromium gives a page a constant instead of the accent color of the OS; see the browser entry above. The
  `[JSImport]` route itself worked - it delivered `#ff0075ff` from the browser into a parsed `HaColor` - so if a
  head ever needs a color from JavaScript, that path is proven.
- The culture selection, measured in a browser: `de-CH, de-DE, en-US, en` gives `de-ch` and `de`; `it-IT` gives
  `it`; `de-DE, de` gives `de`; `pt-BR, de-DE` gives `de`; `gsw-CH` gives `gsw`; `DE-ch` gives `de-ch` and `de`;
  `en-US, en` and `xx` give nothing.
- The loader takes what it is given case-sensitively: asked for `["de-CH", "de-li", "fr"]` it loaded `de-li` and
  `fr` and ignored `de-CH`. Asked with the spelling of the build it loads `de-ch` and `de` together.
- The satellite the app downloads is the right one: with the browser asking for `it-CH`, exactly one
  `Fronius.resources.*.wasm` was fetched, and `resources.satelliteResources` maps that file to the culture `it`.
  The user confirmed the app then displays Italian.
- Measuring which files were fetched with `performance.getEntriesByType("resource")` is unreliable here: the
  buffer holds 250 entries and this app loads more. Read the state of the entries in
  `resources.satelliteResources` instead - the loader stamps `behavior: "resource"` and the culture on the ones
  it took.
- Desktop, Browser, Android and iOS all compile in Debug and Release. iOS compiles on Windows, but has never been
  linked for a device and never been run.

## Known gaps

- **Android and iOS have no server address.** Unlike the browser head they do not seed `CacheKeys.ApiUri`, so
  they fall back to the `https://home-automation.example.com` placeholder in `MainViewModel` and the login fails.
  Either seed them like the browser head does under `#if DEBUG`, or build the settings dialog.
- Neither mobile head has ever run on a device or emulator. Everything about them here is compile time knowledge.
- `dotnet run` on the browser head serves an app whose SkiaSharp fails to initialize, because neither
  `WasmBuildNative` nor `RunAOTCompilation` is on. The page loads and the .NET side runs - useful for checking
  interop in the console - but nothing is rendered.
- The browser head carries `FileCache` without using it. Accepted: one place for the file format is worth more
  than the few kilobytes.
