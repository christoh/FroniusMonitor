---
paths:
  - Fronius/Fronius.csproj
  - Fronius/GlobalUsings.cs
  - FroniusMonitor/Contracts/**
  - FroniusMonitor/Services/**
  - FroniusMonitor/Validators/**
  - FroniusUnitTests/FroniusUnitTests.csproj
  - HomeAutomationServerTests/HomeAutomationServerTests.csproj
---

# What belongs in `Fronius` and what does not

`Fronius` is the library the two production heads share: `HomeAutomationServer` and `HomeAutomationClient`. On
2026-09-15 everything only `FroniusMonitor` and the tests used was moved out of it, so the rule below is the state
of the code and not an aspiration.

## The rule

**`HomeAutomationClient` or `HomeAutomationServer` use it, or it does not belong in `Fronius`.** A test project
using something is not a reason to keep it there: both test projects target `net10.0-windows7.0` and reference
`FroniusMonitor`, so a type they share with the WPF app lives in the WPF app. That is the developer's decision of
2026-09-15 and the reason the test projects are Windows only.

What moved, and where it went:

| From | To |
|---|---|
| `Contracts/IScoped.cs`, `Contracts/ISmartMeterImportService.cs` | `FroniusMonitor/Contracts` |
| `Models/EnergyDirection.cs`, `Models/Settings/SettingsBase.cs`, `SettingsEnums.cs`, `ElectricityPriceSettings.cs`, `AwattarParameters.cs` | `FroniusMonitor/Models` |
| `Services/BayernWerkImportService.cs`, `AwattarService.cs`, `DataCollectionService.cs` | `FroniusMonitor/Services` |
| `Validators/RegexRuleAttribute.cs` | `FroniusMonitor/Validators` |

Two things came with them. **`ClosedXML`** was in `Fronius` for `BayernWerkImportService` and nothing else, so its
package reference left too - and its transitive `DocumentFormat.OpenXml` had meanwhile been picked up by two stray
`using` directives in `HomeAutomationClient` that the IDE had auto-imported and nothing used, which is what a
package in the wrong project buys you. **`CommunityToolkit.Mvvm`** had to be added to `FroniusMonitor`, because
the moved settings classes are `[ObservableProperty]` partial properties and a package's analyzers do not flow
over a project reference (`PrivateAssets` defaults to `contentfiles;analyzers;build`). Its generator now runs in
the WPF app too; the hand-written `Set(ref field, …)` properties there are untouched.

**`Newtonsoft.Json` went the same way on the same day, and left the shipping code altogether.** It was in
`Fronius` for 28 `[JsonProperty]` attributes on the WattPilot models that nothing had read since the conversion to
`System.Text.Json`, a `using` in `WebConnection` that named nothing, and one `JObject` in the WPF app's obfuscated
key provider, which is a `JsonObject` now. The package is a reference of `HomeAutomationServerTests` and of
nothing else, because `JsonExtensionsTests` measures the conversion against it - see [[DeviceJson]].

## What stays although the WPF app is its only real user

Do not move these without reading why they are here; each is pinned by something that is easy to miss.

| What | Pinned by |
|---|---|
| `IDataCollectionService`, `HomeAutomationSystem`, `SolarDataEventArgs`, `SmartMeterCalibrationHistoryItem` | `Gen24PowerFlow` resolves `IDataCollectionService` through `IoC.TryGet` for its calibration history, and `Gen24DataCollector` writes the history items. Both are the server's. |
| `CultureNotifier` | `Gen24Service.OnCultureChanged` |
| `IHaveToolTip` | `ListItemModel<T>` implements it |
| `IPowerMeter1P`, `ITemperatureSensor` | `IPowerConsumer1P`, `FritzBoxDevice`, `ModbusServerService` |
| `Gen24Sensors`, `Gen24ConnectedInverter` | `IGen24Service` and `Gen24Service` |
| `Ipv4Attribute`, `TimeOfDayAttribute` | the Avalonia client, as `[Ipv4(…)]` and `[TimeOfDay(…)]` - see below |

## How to find out, rather than guess

Whether something is shared is a question about the whole repository, and grep for the type name over
`FroniusMonitor HomeAutomationClient HomeAutomationServer FroniusUnitTests HomeAutomationServerTests Fronius`
answers it. Three things will mislead you, and two of them did:

- **An attribute is written without its `Attribute` suffix.** `Ipv4Attribute` and `TimeOfDayAttribute` looked
  unused outside the WPF app until the compiler found `[Ipv4(AllowMask = true, AllowList = true)]` in
  `Gen24ModbusSettings` and `[TimeOfDay(…)]` in two client view models. Search for the short name as well - but
  only where it reads as an attribute, or `NotEmptyAttribute` comes back as used by every `Assert.NotEmpty`.
- **Comments count for nothing.** A comment in `BatteryDetailsViewModel` mentions `HomeAutomationSystem` and
  reads exactly like a use of it.
- **A type another `Fronius` file needs stays**, however few consumers it has of its own, so the search has to
  close transitively over `Fronius` itself before anything moves.

Seventeen files in `Fronius` have no compile-time reference from anywhere as of 2026-09-15 - most of
`Extensions`, `MDnsService` (the NUnit `MDnsTests` talks to Makaretu directly, not to it),
`ToshibaHvacStatusDevice`, `Gen24SolarWebSettings`, `NotEmptyAttribute`. They are not the WPF app's and were left
where they are; they are dead, not private, and deleting them is a separate decision the developer has not been
asked for yet. Check for reflection and JSON deserialization before acting on that list.

## Referencing the WPF app costs a build workaround

**`<BuildInParallel>false</BuildInParallel>` is in three project files** - `FroniusMonitor.csproj` and both test
projects - and all three are needed. WPF compiles XAML that uses a project's own types by building a temporary
copy of the project into the *same* `obj` directory. Beside another project on a second MSBuild node the two
race: the `.g.cs` files vanish mid compile and the build fails with a page of `CS2001` blamed on
`FroniusMonitor.csproj` itself. A second build then succeeds, which is what makes it easy to write off as a
fluke.

Measured on cold builds of `HomeAutomationServerTests`, which is the worse case because it builds the server, the
client and the WPF app side by side: 3 of 3 failed with nothing set, 1 of 5 still failed with the property on
`FroniusMonitor` alone, and 15 of 15 passed once the consumer serializes its project references as well.
`FroniusUnitTests` never failed either way - it has only two references - but carries the property so that adding
a third does not quietly bring the fault back. `dotnet build -m:1` is the same cure from the command line, for a
build that goes wrong anyway.
