---
paths:
  - HomeAutomationClient/HomeAutomationClient/Misc/PlatformStartup.cs
  - HomeAutomationClient/HomeAutomationClient/Misc/ServerUris.cs
  - HomeAutomationClient/HomeAutomationClient/ViewModels/Dialogs/LoginViewModel.cs
  - HomeAutomationClient/HomeAutomationClient/FileCache.cs
  - HomeAutomationClient/HomeAutomationClient/CacheJson.cs
  - HomeAutomationClient/HomeAutomationClient/ICache.cs
  - HomeAutomationClient/HomeAutomationClient/App.axaml.cs
  - HomeAutomationClient/HomeAutomationClient/App.axaml
  - HomeAutomationClient/HomeAutomationClient/Views/MainWindow.axaml.cs
  - HomeAutomationClient/HomeAutomationClient/Misc/StoredWindowSize.cs
  - HomeAutomationClient/HomeAutomationClient/Models/WindowSize.cs
  - HomeAutomationClient/HomeAutomationClient/Assets/Images/**
  - HomeAutomationClient/HomeAutomationClient.Desktop/**
  - HomeAutomationClient/HomeAutomationClient.Browser/**
  - HomeAutomationClient/HomeAutomationClient.Android/**
  - HomeAutomationClient/HomeAutomationClient.iOS/**
  - Fronius/Localization/**
  - Fronius/Models/Settings/WebConnection.cs
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
     handed in `args[0]` - through `ServerUris.From`, see below - and downloads the satellite assemblies for the
     language of the user, both before Avalonia starts.
2. **`AppBuilder`** with the head's font and platform options.
3. **`App.Initialize`** loads `App.axaml`.
4. **`App.OnFrameworkInitializationCompleted`** calls `SetAccentColor()`, builds the service provider, hands it to
   `IoC`, and creates `MainWindow` (desktop lifetime) or `MainView` (single view lifetime). `MainWindow`'s
   constructor opens the window as big as it was last closed - see "The desktop window's size" below.
5. `MainView`'s constructor starts `MainViewModel.Initialize`, which shows the login dialog.

**The rule for step 1: no Avalonia types.** Nothing is initialized yet, and on Android and iOS this code runs
while the platform is still building the activity. That is why a color arrives as
`De.Hochstaetter.Fronius.Models.HaColor` and not as an Avalonia `Color`.

**Not in this document: the address of a view.** Which view the app shows, how it is written into the address bar
of the browser, and what the other heads do instead, is `Navigation.Lifecycle.md`. Only two things here belong to
a head: registering the `IUriService` in step 1, and the base address the browser head is handed in `args[0]`,
which is `document.baseURI` **for a reason the navigation document gives**. Do not go back to `location.href`.

## The desktop window's size

Since 2026-09-14 the desktop window remembers its **size, not its position**, under `CacheKeys.WindowSize`
(a `Models/WindowSize`: `Width`, `Height` in device independent pixels, `IsMaximized`). The developer asked for
exactly that: the position is deliberately not kept, so the OS places the window and it never comes back on a
monitor that is gone.

- **Who does what.** `Misc/StoredWindowSize` is the decision part and has no Avalonia types, like
  `StoredConnection`: `Load` throws away anything below 320 x 240, NaN or infinite (a hand edited cache file) and
  cuts the size down to the working area it is handed; `Save` writes the client size, and for a window closed
  while **maximized** keeps the size already stored and only sets `IsMaximized`, so the window comes back
  maximized and leaving that state gives the old size rather than a screen sized window. Tested in
  `StoredWindowSizeTests` with the test project's `Fakes/TempFileCache`.
- `Views/MainWindow.axaml.cs` only touches the window's own properties: `RestoreSize` in the constructor (before
  the window is shown) sets `Width`/`Height` and `WindowState`, with `Screens.Primary`'s `WorkingArea / Scaling`
  as the limit; `Closing` calls `SaveSize` with `ClientSize`, skipping a minimized window. This is the "work that
  genuinely needs the UI framework" the interaction logic rule allows in code behind; the decisions live in the
  testable helper, not in a view model, because a view model may not know about windows at all.
- Desktop only by construction: `MainWindow` exists only in the `IClassicDesktopStyleApplicationLifetime` branch
  of `App.OnFrameworkInitializationCompleted`. The single view heads fill their screen and never touch the key.

## The desktop is the head with more than one window

Since 2026-09-15 the desktop shows every detail page and every dialog in a window of its own, while the dashboard
and the login stay in `MainView`. The same branch of `App.OnFrameworkInitializationCompleted` decides it, by
registering `WindowPresenter` where every other head gets `MainViewPresenter`; nothing in the head projects knows
about it. See [[DialogSystem.Lifecycle]] for what that presenter does.

Two consequences for this document:

- **`ShutdownMode` is `OnMainWindowClose`, set in the same branch.** Avalonia's default keeps the application alive
  while any window is open, so closing the main window with a detail page still up would leave the app running
  with nothing the user connects it to. Closing the main window now ends it, and the pages go with it.
- **Only `MainWindow` remembers a size.** A dialog window is as big as what is on it, and a page window opens
  sized to its content, capped at 90% of the working area and offset from the last one. Nothing is stored for
  them, so `CacheKeys.WindowSize` stays what it says: the main window.

## What every head must provide

**An `ICache`.** `MainViewModel`, `UpdateService`, `LoginViewModel` and `Misc/StoredConnection` read it through
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

**Every head serializes through `CacheJson.Options`**, the file based ones and the browser alike. They hold the
same values under the same keys, so an implementation that words them differently is a bug waiting to be found by
someone moving between heads - and one such bug was worse than that. The browser cache used Newtonsoft, which
honors neither `System.Text.Json.Serialization.JsonIgnore` nor `XmlIgnore`, and so wrote `WebConnection.Password`
into `localStorage` **in clear text**, beside the encrypted copy that is the only one meant to be stored.
`IsNotifying` and `IsNotifyingBeforeChanging` escaped only because they also carry `[IgnoreDataMember]`, which
Newtonsoft does respect. If a model must be invisible to both serializers, `[IgnoreDataMember]` is the attribute
that says so - but the cache is System.Text.Json throughout now, and should stay that way.

That leak had been holding something else up, which is worth knowing before the next bug report reads like a
regression. On the browser there **was** no encrypted copy - see the next section - so the clear text one was the
only reason a remembered password ever came back there. Closing the leak was right and is not to be undone; it
merely stopped hiding a fault that had been there all along.

Two things about those options that are easy to get wrong:

- **The same object has to be given to the reader and to the writer.** Half of what is set there only applies on
  the way in. For a while only `Serialize` was given them, so `AllowTrailingCommas`, `ReadCommentHandling` and
  `PropertyNameCaseInsensitive` did nothing at all, and `AllowNamedFloatingPointLiterals` was actively dangerous:
  a `NaN` was written happily and threw on the next read. `CacheSerializationTests` holds that symmetry.
- **`IgnoreReadOnlyProperties` is on to keep `ObservableValidator.HasErrors` out**, which every cached
  `BindableBase` carries. It drops every get-only property to do it, so a cached type that keeps state in one
  loses it silently.

The caches serialize with reflection, so the types of cached values (`HomeAutomationServerConnection`, not only
strings) must survive the trimmer. All heads currently build with a trim mode that leaves our own assemblies
alone - verified for iOS with `dotnet msbuild -p:RuntimeIdentifier=ios-arm64 -getProperty:TrimMode`, which
reports `partial`. Give `CacheJson.Options` a `JsonSerializerContext` before that changes; the compiler will not
warn, because the trim analyzer does not look into a referenced project.

## What a browser cannot do at all: encrypt

**There is no symmetric cipher in a browser.** The Web Crypto API is asynchronous throughout, so .NET offers none
of it to synchronous code, and `Aes.CreateEncryptor` / `TransformFinalBlock` throw `PlatformNotSupportedException`
there. **No cipher mode escapes this** - ECB and CBC are equally unavailable, so reaching for another mode is not
a fix, and changing the mode the other heads use only endangers the passwords already written in their settings
files.

What *is* available is everything hash based - SHA, HMAC, PBKDF2 - because those are compiled into the runtime
rather than borrowed from the browser. That asymmetry is the signature of this fault, and it is visible from the
outside: a stored connection with `PasswordChecksum` filled in and `EncryptedPassword` empty beside it has hit
exactly this.

`WebConnection` therefore decides **by capability, not by platform**: `ProbeAes` tries the cipher once and
`HasAes` remembers the answer. Where AES works nothing changed, so every settings file keeps the ECB written
password it always had. Where it does not, `XorWithKeystream` puts an HMAC-SHA256 keystream over a block counter,
keyed with the same key, in its place, with PKCS7 style padding so that a wrong key fails to unpad and forgets
the password instead of putting noise into the box - the same graceful degradation as everywhere else.

Two things to keep:

- **Probe, do not ask `OperatingSystem.IsBrowser()`.** What matters is whether the cipher works, and a capability
  test keeps saying the right thing if a future runtime gains one.
- **The fallback is not interchangeable with AES.** A password written by one cannot be read by the other, which
  costs nothing while no head switches between them - but it does mean the browser and the desktop cannot read
  each other's stored password, unlike every other value in the cache.

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
`Light` and `Dark` dictionaries of `App.axaml` (`#FFDA3B01` and `#C4480C`). Those two are the only place the
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

## The one font, and what a shared view may put on the screen

All four heads build with `WithInterFont()`, and that is the whole font stock of the application. What separates
them is what happens to a character Inter does not have: desktop, Android and iOS ask the operating system and
find something that does, and **the browser head has nothing to fall back to** - a WebAssembly app sees no system
fonts - so it draws an empty box.

**Nothing sets `FontFamily`, anywhere.** `WithInterFont()` only registers the font; the default family stays the
platform's. So the heads do not render in the same font as each other: measured on Windows, `FontFamily.Default`
resolves to Segoe UI (fingerprint `M 898, 0 539` per 1000 em, where the packaged Inter is `M 889, 0 625`), while
the browser head has only the registered collection and therefore renders in Inter. That is worth knowing before
reading any measurement in these documents as gospel: a text width measured on the desktop was measured in Segoe
UI. Setting one family in `App.axaml` would make the four heads agree and cost the desktop its native font -
open, and the developer's to decide.

**Tabular figures are on where the numbers are, not application wide.** Inter gives every digit a width of its
own, which is what the desktop never showed: measured through the real shaper at 12 point, ten ones came to 55.7
pixels against 77.1 for ten fours, so a column of numbers did not line up and a value that changed shifted the
text beside it. Inter offers `tnum` and Avalonia 12.1.2 honours it: `FontFeatures="+tnum"` puts every digit at
77.7, verified end to end against the real `App.axaml` with Skia - 78.00 for ones, zeros and fours alike, and
inside a `TextBox`'s template as well.

It sits on the readouts themselves, put there by the developer after an application wide setter was tried and
rejected: the gauge styles of `InverterControl`, `SmartMeterControl` and `WattPilotControl`, a `TextBlock.Fixed`
class for the numbers of the LCD panel in `PowerConsumer`, and the two timestamp columns of the event log through
`CompactTextColumn.FontFeatures` (see [[SettingsDialogs.Lifecycle]]). **The setter belongs where a number
is, and not on words.** Tabular figures are what a reading wants - a live value that changes must not move the
text beside it - and proportional figures are what a sentence wants. An application wide setter cannot tell the
two apart, and the app is mostly words with numbers in known places.

**The half circle gauges of the detail views are deliberately left out**, decided when they were offered the
setter: each of those shows one reading under a dial of its own, so there is no column for it to line up with.
Not an oversight - do not add it to `WrapPanel.GaugeGroups c|HalfCircleGauge` in `Styles/DetailViews.axaml`.

Worth knowing when adding one: `FontFeatures` is an **inherited** property, so it can be set on a container
rather than on each `TextBlock` inside it, and a `ControlTheme` for a gauge carries it to the numbers the gauge
draws. Measured for reach: a setter on `:is(TopLevel)` arrives at a plain `TextBlock`, at the presenter inside a
`TextBox` and at a tool tip - a native popup root is a `TopLevel` of its own, and a managed popup, which is what
the browser uses, lives in the overlay layer of the window. `:is(TemplatedControl)` reaches nothing further and
puts a setter on every control in the application to do it.

Fonts that were measured while choosing, in case the family is ever set: digits are one width in Noto Sans (572),
Source Sans 3 (472), Lato (580), Open Sans (572), IBM Plex Sans (600) and Segoe UI (539), and a width each in
Inter, Fira Sans and Public Sans. Open Sans, IBM Plex Sans and Public Sans are out whatever their digits do -
none of them has `ϕ` (U+03D5), which three detail views write as `cos(ϕ)` while three others write `cos(φ)`
with U+03C6. **That inconsistency is worth fixing on its own**: on one of the two characters it is invisible, on
the other it would be an empty box.

So a shared view may not depend on a glyph. The rule is: text that is words is fine, and anything that is really
a picture - a cross, an arrow, a chevron - is drawn as a shape and lives in `Assets/Images`. `CrossIcon` is that,
and it exists because the delete button of a charging rule was `Content="✕"` (U+2715), which is not in Inter:
correct on the desktop and an empty box in the browser, reported from a screenshot of the two side by side.

An icon of that kind takes its colour from `Foreground`, which is inherited, so the shape follows the button it
sits in through pointer-over and disabled without being told. That is why `CrossIcon` is a `ContentControl` and
not a `Viewbox` like `GridIcon`: only a templated control has a `Foreground` to inherit into. Measured: the path
resolves to the button's own foreground brush rather than to `null`, which is what a mistyped `RelativeSource`
gives and which draws nothing at all - a worse failure than the box it replaces, because nobody reports a button
that looks empty on purpose.

The other non-ASCII characters in the shared views - `°`, `•`, `Δ`, `cos(φ)` - are Latin, Greek and punctuation
that Inter does carry, and every one of them is on the dashboard or in a message box that the browser head has
been showing all along. They are proven by use; the cross was the one that had never been looked at in a browser.

## Verified, so you do not have to measure again

- Windows fills the palette with the real accent color (`#ffda3b01` on the machine where this was written) and
  Avalonia derives the six shades from it, so leaving Windows alone is right, not lazy.
- Overriding `SystemAccentColor` at application level re-resolves the accent brushes of the theme at once.
- `AppAccentColor` is found where `App.SetAccentColor` looks for it, per theme, and the rebuild on a theme change
  works. This resolution behavior was measured with the desktop head, `AccentColorFollowsOs` forced to false and
  the variant driven from code, using the colors current at the time: started in `Dark` with `#ffff6d3d`, switched
  to `Light` and got `#ffda3b01`, switched back and got `#ffff6d3d` again.
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

## Where the server address comes from

The client talks to two addresses - `CacheKeys.ApiUri` and `CacheKeys.HubUri` - and both are derived from the one
address a user knows: the root the server answers at. `Misc/ServerUris` is the only place that derives them
(`api/` and `hub` below the root, `RootOf` back the other way), so no head does that arithmetic itself.

Only the **browser** head can know the address without asking: it is served by the server it talks to, so
`Program.Main` seeds both cache keys from `args[0]` before Avalonia starts. Everything else asks the user, and the
login dialog is where it asks.

**The login dialog has two modes and one Ok button.** It either asks for credentials or for the server address,
never for both at once, and `LoginViewModel.IsChoosingServer` says which - so `Ok` means "log in" or "use this
server" depending on it. A **"Choose server"** button sits at the left of the Ok row and switches into the second
mode; it is hidden where `CanChangeConnection` is false, which is exactly the browser. Confirming an address
validates it with `AbsoluteUriAttribute`, writes both cache keys, points `IWebClientService` at the new server
through `MainViewModel.SetApiUri`, and fetches that server's AES key.

Four consequences worth knowing:

- **An `HttpClient` refuses a new `BaseAddress` once it has sent a request**, so `WebClientService.Initialize`
  throws away its client and makes a new one when it is called a second time. That drops the authorization header
  of the previous server with it, which is what we want.
- **The cached password does not survive a change of server.** It is encrypted with the key the *server* hands
  out for the user name, so after the switch it cannot be decrypted and comes back empty - the documented
  graceful degradation of `WebConnection.EncryptedPassword`. The user types it once and it is cached again.
- **What the user has typed is never taken away from them.** `LoginViewModel.LoadCachedCredentialsAsync` runs
  again after a change of server, and only fills a box that is *empty*. By then the user may well have typed the
  credentials for the new server already, and replacing those with a cached password of the old one would be the
  last thing they asked for.
- **Asking a server for a key is the first thing the client ever does with an address**, so that is where a wrong
  or unreachable one shows up. `IServerBasedAesKeyProvider.SetKeyFromUserName` therefore answers with a
  `ProblemDetails` instead of throwing, and the dialog shows it with `ErrorBoxes.ShowServerProblem` - the server's
  own message where it answered, and a "cannot reach the server" sentence where nothing did. A socket exception
  is not something an end user can act on.

Where the cached addresses are unusable - a phone that has never been told, or a leftover from a server that has
moved - `LoginViewModel.Initialize` starts in the address mode without contacting anything, and shows no error:
not knowing the address yet is not the user having got something wrong.

## Known gaps

- Neither mobile head has ever run on a device or emulator. Everything about them here is compile time knowledge.
- `dotnet run` on the browser head serves an app whose SkiaSharp fails to initialize, because neither
  `WasmBuildNative` nor `RunAOTCompilation` is on. The page loads and the .NET side runs - useful for checking
  interop in the console - but nothing is rendered.
- The browser head carries `FileCache` without using it. Accepted: one place for the file format is worth more
  than the few kilobytes.
