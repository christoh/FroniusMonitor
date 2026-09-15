---
paths:
  - Fronius/Fronius.csproj
  - Fronius/GlobalUsings.cs
  - FroniusMonitor/Contracts/**
  - FroniusMonitor/Services/**
  - FroniusMonitor/Validators/**
  - HomeAutomationClient/HomeAutomationClient/Contracts/**
  - HomeAutomationClient/HomeAutomationClient/Validators/**
  - FroniusUnitTests/FroniusUnitTests.csproj
  - HomeAutomationServerTests/HomeAutomationServerTests.csproj
---

# What belongs in `Fronius` and what does not

`Fronius` is the library the two production heads share: `HomeAutomationServer` and `HomeAutomationClient`. It
was cleared out three times, once per head - of what only `FroniusMonitor` used and of what only the server used
on 2026-09-15, of what only the client used on 2026-09-16 - so the rule below is the state of the code and not an
aspiration. What is left is what **both** heads need: the device models and their JSON, the localization, the
validation rules both use, and the web API contract.

## The rule

**`HomeAutomationClient` *and* `HomeAutomationServer` use it, or it does not belong in `Fronius`.** One head on
its own is not enough: what only the client uses would belong to the client, what only the server uses belongs to
the server, and what only the WPF app uses belongs to the WPF app. A test project using something is no reason to
keep it either - both test projects reference `FroniusMonitor` and `HomeAutomationServer` and target
`net10.0-windows7.0`, so they can reach a type wherever it lives. That is the developer's decision of 2026-09-15
and the reason the test projects are Windows only.

What moved, and where it went:

| From | To |
|---|---|
| `Contracts/IScoped.cs`, `Contracts/ISmartMeterImportService.cs` | `FroniusMonitor/Contracts` |
| `Models/EnergyDirection.cs`, `Models/Settings/SettingsBase.cs`, `SettingsEnums.cs`, `ElectricityPriceSettings.cs`, `AwattarParameters.cs` | `FroniusMonitor/Models` |
| `Services/BayernWerkImportService.cs`, `AwattarService.cs`, `DataCollectionService.cs` | `FroniusMonitor/Services` |
| `Validators/RegexRuleAttribute.cs` | `FroniusMonitor/Validators` |
| the whole SunSpec and Modbus stack: `Models/Modbus/**`, `Contracts/Modbus/**`, `Services/Modbus/**`, `Attributes/ModbusAttribute.cs`, `Extensions/ModbusExtensions.cs`, `Extensions/SunSpecExtensions.cs`, `Models/ModbusMapping.cs`, `Models/Settings/Modbus*.cs`, `SunSpecClientParameters.cs` | the same folders under `HomeAutomationServer` |
| `Services/DataCollectors/**`, `Services/DataControlService.cs`, the DWD half of `Services/EnergyData` and `Models/EnergyData/DwdForecast.cs` | `HomeAutomationServer/Services`, `HomeAutomationServer/Models/EnergyData` |
| the collector parameter classes of `Models/Settings`, `SettingsChangeTracker`, `PolledWebConnectionParameterBase`, `Models/WebApi/WebApiInfo.cs` | `HomeAutomationServer/Models` |
| `Contracts/IDataControlService.cs`, `IGen24ConfigRefresher.cs`, `IHomeAutomationRunner.cs`, `IWattPilotServices.cs`, `IDwdWeatherClient.cs`, `IEnergyDataService.cs`, `IEnergyHistoryStore.cs` | `HomeAutomationServer/Contracts` |
| `Contracts/HomeAutomationClient/**`, `Services/HomeAutomationClient/**`, `Models/HaColor.cs`, `Models/HomeAutomationClient/ApiResult.cs`, `Validators/AbsoluteUriAttribute.cs` | `HomeAutomationClient/HomeAutomationClient` under `Contracts`, `Services`, `Models` and a new `Validators` |

Two things came with them. **`ClosedXML`** was in `Fronius` for `BayernWerkImportService` and nothing else, so its
package reference left too - and its transitive `DocumentFormat.OpenXml` had meanwhile been picked up by two stray
`using` directives in `HomeAutomationClient` that the IDE had auto-imported and nothing used, which is what a
package in the wrong project buys you. **`CommunityToolkit.Mvvm`** had to be added to `FroniusMonitor`, because
the moved settings classes are `[ObservableProperty]` partial properties and a package's analyzers do not flow
over a project reference (`PrivateAssets` defaults to `contentfiles;analyzers;build`). Its generator now runs in
the WPF app too; the hand-written `Set(ref field, …)` properties there are untouched.

**`FluentModbus` followed the Modbus stack into `HomeAutomationServer`, and `CommunityToolkit.Mvvm` had to be
added there** for the same reason as in the WPF app. `DeviceId` stays: `Crypto/AesKeyProvider` uses it.

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
| `ProblemDetails` | it sits in `Models/HomeAutomationClient` and looks like the client's, but `ValidationRuleAttribute` and the server's controllers use it. The folder name is the misleading part, not the type. |

## How to find out, rather than guess

Whether something is shared is a question about the whole repository, and grep for the type name over
`FroniusMonitor HomeAutomationClient HomeAutomationServer FroniusUnitTests HomeAutomationServerTests Fronius`
answers it. Four things will mislead you, and three of them did:

- **An attribute is written without its `Attribute` suffix.** `Ipv4Attribute` and `TimeOfDayAttribute` looked
  unused outside the WPF app until the compiler found `[Ipv4(AllowMask = true, AllowList = true)]` in
  `Gen24ModbusSettings` and `[TimeOfDay(…)]` in two client view models. Search for the short name as well - but
  only where it reads as an attribute, or `NotEmptyAttribute` comes back as used by every `Assert.NotEmpty`.
- **Comments count for nothing.** A comment in `BatteryDetailsViewModel` mentions `HomeAutomationSystem` and
  reads exactly like a use of it.
- **An extension method is called by its own name, never by its class's.** `SunSpecExtensions` and
  `JsonExtensions` look like nothing refers to them, because nothing writes `JsonExtensions` anywhere - it is
  `node.AsString()` at 9 call sites. Only the compiler found this one: `SunSpecModelBase` stopped building the
  moment its extensions stayed behind. For a static class, search the **member** names, not the type.
- **A type another `Fronius` file needs stays**, however few consumers it has of its own, so the search has to
  close transitively over `Fronius` itself before anything moves.

**On unreferenced files, trust nothing that was not checked member by member.** An earlier version of this
document called seventeen files dead, and most of `Fronius/Extensions` was on that list purely because the search
had been for type names. The ones with no member referenced anywhere are `ColorControlModes`,
`ToshibaHvacPowerSetting`, `ToshibaHvacStatusDevice`, `MDnsService` (the NUnit `MDnsTests` talks to Makaretu
directly, not to it) and `NotEmptyAttribute`; `Gen24SolarWebSettings` and `ToshibaDateTimeConverter` are reached
only through `Clone`, `Read` and `Write`, which are too common a name for a search to settle. Nothing was
deleted, because deleting is a separate decision the developer has not been asked for - and reflection and JSON
deserialization would have to be checked first in any case.

## Where `Fronius/Validators` ended up

The family is split three ways, and each piece went to its only user. `Fronius/Validators` keeps
`ValidationRuleAttribute`, `MinMaxRules`, `NumericText`, `Ipv4Attribute`, `TimeOfDayAttribute`,
`WattPilotFallbackCurrentAttribute` and the unreferenced `NotEmptyAttribute`; `RegexRuleAttribute` is in
`FroniusMonitor/Validators` and `AbsoluteUriAttribute` in `HomeAutomationClient/.../Validators`. The mechanism
itself is unchanged and is described in [[Validation.Lifecycle]]: a rule is declared on the property that is
edited and every head picks it up from there, wherever the attribute class happens to live.

`HaColor` went with them, into `De.Hochstaetter.HomeAutomationClient.Models`. The developer changed
`.claude/rules/ViewModelsForInteractionLogic.md` to name the new namespace and to say the paragraph does not
apply to `FroniusMonitor`, which has no `HaColor` of its own.

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
