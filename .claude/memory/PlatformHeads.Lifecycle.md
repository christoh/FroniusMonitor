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
   - the browser head additionally seeds `CacheKeys.ApiUri` and `CacheKeys.HubUri`.
2. **`AppBuilder`** with the head's font and platform options.
3. **`App.Initialize`** loads `App.axaml`.
4. **`App.OnFrameworkInitializationCompleted`** calls `SetAccentColor()`, builds the service provider, hands it to
   `IoC`, and creates `MainWindow` (desktop lifetime) or `MainView` (single view lifetime).
5. `MainView`'s constructor starts `MainViewModel.Initialize`, which shows the login dialog.

**The rule for step 1: no Avalonia types.** Nothing is initialized yet, and on Android and iOS this code runs
while the platform is still building the activity. That is why a color arrives as
`De.Hochstaetter.Fronius.Models.HaColor` and not as an Avalonia `Color`.

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

**Where a head detected nothing, nothing is touched.** That is deliberate: on Windows Avalonia fills the palette
from the OS and keeps it up to date while the app runs, so an override would freeze the accent color at what it
was when the app started.

- **Desktop** supplies nothing. On Windows the fallback already is the accent color of the OS, because Avalonia
  reads it. macOS keeps its in `NSColor.controlAccentColor`, out of reach for a plain .NET app, and Linux has
  none that all desktop environments agree on.
- **Browser** reads the CSS system color `AccentColor` in `wwwroot/accent.js` through `[JSImport]`. Firefox and
  Safari answer with the real value. **Chrome knows the keyword but returns a constant `rgb(0, 117, 255)`**, not
  the accent of the OS - measured against `#CA5010` on a Windows machine, where every other CSS system color is
  hard coded as well.
- **Android** takes the Material You color `system_accent1_500` on API 31 and later, and the `colorAccent` of
  the app theme before that. Compile verified only.
- **iOS** supplies nothing: iOS has no accent color of the OS, what looks like one is the tint color of the app.

The JavaScript module is imported as `JSHost.ImportAsync("accent", "../accent.js")`. The path is relative to the
.NET runtime in `_framework`, not to the document - `./accent.js` does not resolve.

## Verified, so you do not have to measure again

- Windows fallback resolves to the real accent color (`#ffca5010` on the machine where this was written).
- `/accent.js` is served and imports; the .NET side logs to the browser console when it cannot.
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
