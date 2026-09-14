---
paths:
  - Fronius/Models/EnergyData/**
  - Fronius/Models/Settings/EnergyDataSettings.cs
  - Fronius/Models/Settings/EnergyDataCollectorParameters.cs
  - Fronius/Contracts/EnergyData/**
  - Fronius/Services/EnergyData/**
  - Fronius/Services/DataCollectors/EnergyDataCollector.cs
  - Fronius/Contracts/HomeAutomationClient/IWebClientService.cs
  - Fronius/Services/HomeAutomationClient/WebClientService.cs
  - HomeAutomationServer/Services/EnergyHistoryStore.cs
  - HomeAutomationServer/Controllers/EnergyDataController.cs
  - HomeAutomationServer/Models/Settings/Settings.cs
  - HomeAutomationServer/Settings.xml.example
  - HomeAutomationServer/Dockerfile
  - HomeAutomationServer/Program.cs
  - HomeAutomationClient/HomeAutomationClient/Models/EnergyChartModel.cs
  - HomeAutomationClient/HomeAutomationClient/ViewModels/Dialogs/EnergyChartViewModel.cs
  - HomeAutomationClient/HomeAutomationClient/ViewModels/Dialogs/PriceComponentsViewModel.cs
  - HomeAutomationClient/HomeAutomationClient/Views/Dialogs/EnergyChartView.axaml
  - HomeAutomationClient/HomeAutomationClient/Views/Dialogs/EnergyChartView.axaml.cs
  - HomeAutomationClient/HomeAutomationClient/Views/Dialogs/PriceComponentsView.axaml
  - HomeAutomationClient/HomeAutomationClient/Controls/EnergyChartRenderer.cs
  - HomeAutomationClient/HomeAutomationClient/Services/UpdateService.cs
  - HomeAutomationClient/HomeAutomationClient/Contracts/IUpdateService.cs
  - HomeAutomationServerTests/UnitTests/EnergyDataCollectorTests.cs
  - HomeAutomationServerTests/UnitTests/EnergyDataParserTests.cs
  - HomeAutomationServerTests/UnitTests/EnergyHistoryStoreTests.cs
  - HomeAutomationServerTests/UnitTests/EnergyChartModelTests.cs
  - HomeAutomationServerTests/UnitTests/Fakes/FakeEnergyDataSources.cs
  - FroniusMonitor/ViewModels/PriceViewModel.cs
  - FroniusMonitor/Views/PriceView.xaml
  - FroniusMonitor/Views/PriceComponentsView.xaml
---

# Energy prices, productions and weather (the price chart)

The port of FroniusMonitor's `PriceView` to the client-server architecture. The WPF app asks Awattar itself from
`AwattarService` / `WattPilotElectricityService`; **the new code uses neither** - the server collects, keeps a
history and pushes, the client only draws. The WPF app is untouched and still uses the old services.

## Sources, and which one wins

| Source | What | How |
|---|---|---|
| Awattar (tado° Energy) | market prices, tariff components, sun and wind production of the price zone | HTTPS, `AwattarClient` |
| Wattpilot | the same market prices as its `awpl` forecast | not asked: taken off the `WattPilot` the `WattPilotDataCollector` publishes to `IDataControlService` |
| DWD (German weather service) | hourly forecast and hourly measurements of one station | plain files on `opendata.dwd.de`, `DwdWeatherClient` |

**Awattar first, the Wattpilot fills in.** `EnergyDataCollector.MergePrices` takes every Awattar slot and adds a
Wattpilot slot only where no Awattar slot overlaps it. The two are the same auction, so a mixed day is not a
contradiction; every `EnergyPricePoint` says its `Source` and `EnergyChartData.HasWattPilotPrices` tells the client
to say so in the title.

### Awattar

- `GET api.awattar.{de|at}/v1/marketdata?start=&end=` (ms since the epoch, end exclusive) - no token. Answers
  `marketprice` in Eur/MWh; `EnergyPricePoint.CentsPerKiloWattHour` is that over ten, **net, market only**.
- `GET /v1/power/productions?start=&end=` - no token, `solar` and `wind` in MW per slot (`GridProductionPoint`).
- `GET /v1/power/productions?start=&end=` - no token. **Answers every hour of the span, and the hours beyond its
  forecast horizon (one day ahead, checked 2026-09-15 against the live service) carry `"solar": null, "wind":
  null`.** `AwattarEnergy` is therefore nullable and `AwattarClient.ToProductions` drops such hours
  (`HasValues`); a plain `double` rejected the whole two-day answer and the chart had no production bars at all.
  The WPF `PriceViewModel` filters the same way. `AwattarProductionsTests` reads a recorded answer with the gap.
- `GET /v1/prices?zipcode=&consumption=1800&gridoperator=&tariff=hourly-2&domain=awattar&date=yyyy-MM-dd` with
  `Authorization: Bearer` - the tariff components (`EnergyPriceComponent`): name, description, net price, tax
  rate, unit. Not all are per kWh (`Grundpreis Netz` is Euro/Jahr), so the unit travels and only
  `IsPerKiloWattHour` components go into a price per kWh. **Awattar's own fee is not in the answer**: the collector
  appends it from `EnergyDataSettings.SurchargeCentsPerKiloWattHour` (1.5 ct/kWh net, VAT on top) as a component
  named `aWATTar`, and only when the other components are there - a "buying price" of market plus fee alone would
  be no buying price.
- **About a hundred requests a day are allowed.** That is why the history exists and why the collector asks only
  in windows: tomorrow's prices every quarter hour from 12:00 until 15:00 local time, and only while tomorrow is
  not yet complete (`EnergyPriceCalculator.CoversSpan`); the production forecast again every quarter hour from
  18:00 to 20:00; the tariff once per day; today and tomorrow once at start. A day that is incomplete outside the
  window is retried hourly. `FakeAwattarClient` counts requests and the collector tests pin the cadence.
- The bearer, postal code and grid operator id are in `Settings.xml` **in clear text**, by decision: the token is
  Awattar's fixed one for years and reads a public tariff. It is **not** in the repo - `Settings.xml.example`
  carries a placeholder. Without the three the market prices and productions are still collected; only the
  components stay empty.

### DWD

- Forecast: `MOSMIX_L/single_stations/{id}/kml/MOSMIX_L_LATEST_{id}.kmz` - a zip with one KML, issued every six
  hours, ~10 days hourly. `DwdMosmixParser` reads `Rad1h` (kJ/m² over the hour **before** the time step, filed
  under the slot an hour earlier, converted to W/m² by ×1000/3600), `FF` (m/s), `TTT` (K → °C), `N` (%) at the
  step. A missing value is `-`.
- Measurements: `weather_reports/poi/{id}-BEOB.csv` - semicolon separated, decimal comma, `---` missing, first
  row names, second row units, `dd.MM.yy;HH:mm` UTC, newest first, about a day. `DwdObservationParser` finds the
  columns **by name** and converts the wind from km/h. Not every MOSMIX station measures; a 404 is an empty list,
  not an error. The station id is the MOSMIX catalogue id (`10863` = Weihenstephan, which measures radiation; `10870` = München-Flughafen does not) and is used for both
  files.
- The netCDF grid files of `opendata.dwd.de/weather/satellite/radiation/` (what the `dwd_global_rad_hass`
  integration the task named uses) are 16-27 MB every ten minutes and need an HDF5 reader - deliberately not
  used. **No DWD history is fetched**: the climate archive is zipped decades per station; what is polled is kept
  (`Weather` table), which is what the task asked for where no history API fits.
- Polled every 30 minutes (`EnergyDataCollectorParameters.WeatherRefreshRate`), forecast and measurements in one
  go. A forecast issued again replaces the old rows (same key); a measurement of an hour stands beside the
  forecast of that hour, and `MergeWeather` prefers the measurement when the chart data is assembled.

### The Wattpilot

`WattPilot.ElectricityPrices` (`awpl`: `start` epoch seconds, `interval` seconds, `marketprice[]` ct/kWh) arrives
with the charger several times a second. `OnDeviceUpdate` compares a snapshot (start, interval, the array) and only
a changed forecast is expanded (`ExpandWattPilotPrices`), stored under `Source = WattPilot` and published.

## The history: `history/PriceAndWeatherHistory.db`

`EnergyHistoryStore` in the server, SQLite via `Microsoft.Data.Sqlite`, the contract `IEnergyHistoryStore` in
Fronius so the collector and the tests do not know SQLite. **The file is next to the executable in `history/`**,
which the Dockerfile creates and `chown`s to `app`; `docker-compose.yml` shows the mount. Yesterday and older is
final: `GetDayAsync` serves it from the store and asks Awattar only for what the store lacks, once.

Tables (`user_version` 1), every key carrying the source so a second provider fits without a migration:

| Table | Key | Notes |
|---|---|---|
| `MarketPrice` | `Source, StartUtc` | `CentsPerKwh` as TEXT so a decimal comes back exact |
| `PriceComponent` | `Day, Name` | `Ordinal` keeps Awattar's order (a WITHOUT ROWID table has none); a day is deleted and rewritten as a whole |
| `GridProduction` | `Region, StartUtc` | |
| `Weather` | `Source, StationId, TimeUtc, IsForecast` | nullable columns; an upsert never overwrites a value with NULL (`COALESCE`) |

Times are Unix seconds UTC. Schema changes go into `InitializeAsync` as `if (version < n)` steps.

## Publishing: the chart data is a "device"

`EnergyChartData : IHaveUniqueId` (`Manufacturer` "Home Automation Server", `Model` `EnergyChartData`,
`SerialNumber` "current") is published to `IDataControlService` under `EnergyChartData.DeviceId`, **so nothing new
was built for the push**: `SignalRDispatcher` broadcasts it as the hub message `EnergyChartData` on every
`AddOrUpdate`, `HomeAutomationHub.OnConnectedAsync` replays it to a connecting client, `StopAsync` removes it. It
covers **today and tomorrow of the server's local day** (`EnergyDataSettings.TimeZoneId`, or the machine's zone -
a container is UTC unless `TZ` is set; the image has `tzdata`). `From`/`To` are UTC and a day is 23 or 25 hours
when the clocks change (`DayBounds` converts local midnight, it does not add 24 h).

`GET api/EnergyData` answers `IEnergyDataService.Current`, `GET api/EnergyData/{yyyy-MM-dd}` one local day of the
server; 404 with a `ProblemDetails` where the server has no `EnergyData` element. The collector implements
`IEnergyDataService` and is registered under both (same instance), like the Gen24 and Wattpilot collectors.

`EnergyChartData.MarketVatRate` travels with the data so the client taxes the market price with the server's
rate; each component carries its own `TaxRate`. **`EnergyPriceCalculator.Display` is the one place that adds a
price up** - market or buy, net or gross - and both ends use it. Gross is not one factor over the sum: a component
exempt from VAT stays as it is, and a negative market price gets negative VAT, which is what the invoice does.

## The client

- `UpdateService.EnergyChartData` (+ `HasEnergyData`, `EnergyChartDataChanged`) - read once with
  `GetEnergyData()` at start (404 is "no chart"), replaced whole on every hub message. `MainView` shows the
  **Electricity Price** menu button only while `HasEnergyData`.
- `EnergyChartViewModel` (`DialogBase`, resizeable, `IsResizeable` on from the start) owns every choice - buy or
  market, net or gross, today, tomorrow or a historic day, the two switches for productions and weather - and
  works an `EnergyChartModel` out of the data: **plain numbers and captions, local times, no charting type**.
  Today and tomorrow are the *client's* local days cut out of the live span; a historic day is the server's day
  as answered (`From`/`To`). The historic date binds a `DatePicker` directly (`DateTimeOffset?`): it is spun, not
  typed, which is what the interaction rule allows, and the changed handler clamps it to 2013-12-22 .. yesterday
  because the control has no bounds. `Initialize` is guarded against the second run the components dialog causes.
- `EnergyChartView.axaml.cs` is the only place that knows ScottPlot: it draws `ChartModel` with
  `EnergyChartRenderer` into the `AvaPlot`, redraws on `ActualThemeVariantChanged`, and marshals a model set from
  the hub's thread with `Dispatcher.UIThread.Post`. It calls `Plot.Reset()` before every drawing because the
  weather adds right axes that `Clear()` would leave standing. Interaction is off (`UserInputProcessor.IsEnabled`),
  as zoom and pan were off in WPF. Theme colors reach the renderer as `HaColor`. **Color the axes after
  `DateTimeTicksBottom()`**: that call replaces the bottom axis, and one colored before it came up black on the
  dark theme.
- **Value labels sit at a fixed offset in a fixed 11 point font, like OxyPlot's.** They overlap when the dialog is
  narrow, and the developer wants that rather than what was tried on 2026-09-13: staggering by bar index (bars
  of different heights put neighbours at the same height anyway) and collision detection in pixel space with a
  smaller font and dropped labels ("a mess"). Do not bring either back; make the dialog wider instead.
- The settings row is one `Grid` of `Auto` columns and never wraps, by decision: the switches and the components
  button stand right of the date picker. The `DatePicker` is given `MinWidth="0" Width="230"` because Fluent makes
  it 300 wide, which squeezed a star column to nothing; the dialog's `MinWidth` is what the row needs in German.
  The picker binds `MinYear`/`MaxYear` - the only bounds it has, a year not a day - and the view model clamps the
  day. Nothing is written over the chart when weather, productions or components are missing.
- **The chart's font is Inter, registered by hand** (`InterFontResolver`). ScottPlot resolves font names through
  Skia's system font manager, which the browser does not have, so the chart's face changed from one container
  start to the next (monospace one time, proportional the next). The resolver reads `Inter-Regular.ttf` and
  `Inter-Bold.ttf` out of the `Avalonia.Fonts.Inter` assets, is put first in `ScottPlot.Fonts.FontResolvers`,
  and makes `Fonts.Default` "Inter" - **before the `AvaPlot` is built**, in the view's constructor, because a
  plot takes its font at construction. `plot.Font.Set(...)` is deliberately not called: it pins one typeface on
  every label and the title stops being bold.
- **Weather lines run from edge to edge without a break** (`EnergyChartModel.WeatherLines`). Radiation is a mean
  and drawn at the middle of its hour, wind is a state and drawn at the start, so both lines used to stop an hour
  short of midnight and the dashed forecast began an hour after the solid measurement ended. Now the forecast
  line starts at the last measured point, and a point is added on each edge of the day: interpolated between the
  neighbouring hours where the hour beyond the edge is known (the live span has tomorrow's first hour), the edge
  value repeated where it is not. `Build` therefore looks at the whole span's weather, not only the day's.
- The shape is the WPF chart's: price bars (LightSeaGreen, negatives Coral) with their value written on them,
  productions in GW hanging from the top of a right axis whose range is minus three times the largest total
  (so they take the top third; the price axis is stretched 60 % at the top for them), legend. New: DWD global
  radiation (W/m²) and wind speed (m/s) as lines on two more right axes, measured solid, forecast dashed.
- `PriceComponentsViewModel` / `PriceComponentsView`: the WPF components table (per kWh components only, net,
  VAT, gross, sums) as a dialog over the chart. Its columns sort on a header click (`CanUserSortColumns`, since
  2026-09-14 at the developer's request); the grid sorts the plain `IReadOnlyList` through its own collection
  view, like the event log, and a bound column sorts by the bound value, so no `SortMemberPath` is needed.
- **ScottPlot.Avalonia 5.1.59** needs Avalonia ≥ 12; the browser head is expected to work through Avalonia's Skia
  but has not been run with it yet.

## Tests (`HomeAutomationServerTests/UnitTests`)

`EnergyDataParserTests` (both DWD files cut down, the calculator, `CoversSpan`), `EnergyHistoryStoreTests` (a
real SQLite file in the temp folder), `EnergyDataCollectorTests` (fakes, a settable clock, `TickAsync` by hand:
start, the window cadence, the Wattpilot fallback and hand-back, the store serving a past day, a refusing Awattar,
stop), `EnergyChartModelTests` (bars, axes, weather, and the JSON round trip with the server's options).

## Known gaps, as of 2026-09-13

- Nothing shows the Wattpilot's price zone (`awc`); the zone is `EnergyDataSettings.PriceRegion` throughout.
- `WeatherStationName` is known only after the first DWD poll of a process; the store does not keep it.
- Historic days show the weather only where the server was running that day.
- No Android/iOS/browser run of the chart yet; ScottPlot's Skia rendering in WebAssembly is unverified.
- The WPF app still has its own `AwattarService`; the two implementations share only the JSON models.
