---
paths:
  - HomeAutomationServer/Models/Settings/SolarWebSettings.cs
  - HomeAutomationServer/Models/Settings/SolarWebParameters.cs
  - HomeAutomationServer/Models/SolarWeb/**
  - Fronius/Models/SolarWeb/**
  - HomeAutomationClient/HomeAutomationClient/Models/SolarWebChartModel.cs
  - HomeAutomationClient/HomeAutomationClient/Models/FirmwareUpdateNotice.cs
  - HomeAutomationClient/HomeAutomationClient/Controls/SolarWebChartRenderer.cs
  - HomeAutomationClient/HomeAutomationClient/Controls/ChartTheme.cs
  - HomeAutomationClient/HomeAutomationClient/ViewModels/Dialogs/SolarWebChartViewModel.cs
  - HomeAutomationClient/HomeAutomationClient/Views/Dialogs/SolarWebChartView.axaml
  - HomeAutomationClient/HomeAutomationClient/Views/Dialogs/SolarWebChartView.axaml.cs
  - HomeAutomationClient/HomeAutomationClient/ViewModels/MainViewModel.cs
  - HomeAutomationClient/HomeAutomationClient/Views/MainView.axaml
  - HomeAutomationClient/HomeAutomationClient/Contracts/IUpdateService.cs
  - HomeAutomationClient/HomeAutomationClient/Services/UpdateService.cs
  - HomeAutomationClient/HomeAutomationClient/Contracts/IWebClientService.cs
  - HomeAutomationClient/HomeAutomationClient/Services/WebClientService.cs
  - HomeAutomationServerTests/UnitTests/SolarWebChartModelTests.cs
  - HomeAutomationServerTests/UnitTests/Avalonia/SolarWebChartViewTests.cs
  - HomeAutomationServerTests/UnitTests/FirmwareUpdateNoticeTests.cs
  - HomeAutomationServerTests/UnitTests/Fakes/FakeUpdateService.cs
  - HomeAutomationServer/Contracts/ISolarWebClient.cs
  - HomeAutomationServer/Contracts/ISolarWebHistoryStore.cs
  - HomeAutomationServer/Contracts/ISolarWebService.cs
  - HomeAutomationServer/Services/SolarWeb/**
  - HomeAutomationServer/Services/SolarWebHistoryStore.cs
  - HomeAutomationServer/Services/DataCollectors/SolarWebService.cs
  - HomeAutomationServer/Services/SqliteStoreBase.cs
  - HomeAutomationServer/Misc/TimeZones.cs
  - HomeAutomationServer/Controllers/SolarWebController.cs
  - HomeAutomationServer/Models/Settings/Settings.cs
  - HomeAutomationServer/Settings.xml.example
  - HomeAutomationServer/Program.cs
  - HomeAutomationServerTests/UnitTests/SolarWebSettingsTests.cs
  - HomeAutomationServerTests/UnitTests/SolarWebChartParserTests.cs
  - HomeAutomationServerTests/UnitTests/SolarWebHistoryStoreTests.cs
  - HomeAutomationServerTests/UnitTests/SolarWebFirmwareTests.cs
  - HomeAutomationServerTests/UnitTests/SolarWebServiceTests.cs
  - HomeAutomationServerTests/UnitTests/HtmlFormTests.cs
  - HomeAutomationServerTests/UnitTests/Hosted/SolarWebLoginTests.cs
  - HomeAutomationServerTests/UnitTests/Fakes/FakeSolarWeb.cs
---

# Fronius Solar.web: the history charts and the firmware status

Solar.web (`www.solarweb.com`) is Fronius' portal with the long-term history of an inverter. There is no free API;
the server reads what the portal's own chart page reads, logged in as the user, caches it and serves it to the
Avalonia client, which draws it (both since 2026-09-20). `HomeAutomationClient/Server only` - the WPF app is not
touched. The models both heads share - `SolarWebChart`, `SolarWebSeries` (with Solar.web's `Color`), `SolarWebPoint`,
`SolarWebInterval`, `SolarWebView`, `SolarWebFirmwareStatus`, `SolarWebFirmwareComponent`, `SolarWebVersion` - are
`Fronius/Models/SolarWeb`; `SolarWebPeriod` and the exceptions stay the server's ([[Fronius.SharedLibraryBoundary]]).

## What the chart page does (found out with the browser on 2026-09-20)

`https://www.solarweb.com/Chart/Chart?pvSystemId=<guid>` loads one JSON per click from

```
GET /Chart/GetChartNew?pvSystemId=<guid>&year=2026&month=9&day=20&interval=<i>&view=<v>
```

| Button | `interval` | Points | Unit | Times |
|---|---|---|---|---|
| TAG | `day` | every 5 min, 288 per day; the battery state as one `bubble` with a text | W, % | real UTC instants, the day starts at local midnight (22:00Z in summer) |
| MONAT | `month` | one per day | kWh | midnight **UTC** of the day - a date written as an instant, whatever the zone |
| JAHR | `year` | one per month | kWh / MWh | midnight UTC of the first |
| GESAMT | `all` | one per year | MWh | midnight UTC of 1 January |

| Tab | `view` | Series ids seen |
|---|---|---|
| PRODUKTION | `production` | `FromGenToGrid`, `FromGenToBatt`, `FromGenToConsumer`, `FromGenToWattPilot`, `FromGenToSomewhere`, `PvForecastTruncated` (month), `ToConsumer`, `StateOfCharge`, `BattOperatingState` (day) |
| VERBRAUCH | `consumption` | `FromGridToConsumer`, `FromBattToConsumer`, `FromGenToConsumer`, `FromGen`, `StateOfCharge`, `BattOperatingState` (day) |
| RENTABILITÄT (Premium) | `returnofinvestment` | `Saving` (Ersparnis), `Income` (Ertrag), EUR |
| KOSTEN (Premium) | `expense` | `Saving`, `Expense` (Tatsächliche Kosten), EUR |

**The two Premium views have no day chart**: `interval=day` with either answers `500 - Internal server error`
as HTML, and the page does not offer them under TAG. `SolarWebService` refuses that combination with an
`ArgumentException` (400 from the controller) before asking.

The answer is a Highcharts configuration: `settings.series[]` with `id`, `name` (localized by the request's
culture), `type` (`column`, `areaspline`, `spline`, `bubble`), `yAxis` (the unit), `data` as `[ms, value]` or
`[ms, value, "text"]`, plus `isPremiumFeature`, `title` (`19.09.2026`, `September 2026`, `Gesamt`), `sumValue`
(`72,78 kWh`), `navOptions`. `SolarWebChartParser` keeps the series and those fields and drops colours, axes and
tooltips. `SolarWebChartParserTests` holds cut-down recorded answers.

**Solar.web is touchy.** During the exploration it answered `429` to its own telemetry posts and a `503` to one
chart request for no reason of ours. Hence everything below about pacing.

## The login (`SolarWebClient`)

Solar.web is an OpenID Connect client (Microsoft OWIN, `x-client-SKU=ID_NET472`) of a WSO2 Identity Server at
`login.fronius.com`. Without a session:

1. `GET /Chart/GetChartNew…` → 302 `/Account/ExternalLogin?ReturnUrl=…` → 302
   `login.fronius.com/oauth2/authorize?client_id=mf_o9iTAyKemNLQTa6Sp6HYonCIa&response_type=code id_token&response_mode=form_post&scope=openid profile solarweb solweb_browserid_<hash>&state=<opaque>&nonce=…`
   (sets an `OpenIdConnect.nonce.*` cookie on solarweb.com) → 302 `/authenticationendpoint/login.do?…&sessionDataKey=<guid>`.
2. The login page is a plain form, `action="../commonauth"`, POST: `username` (the e-mail; the visible field is
   `usernameUserInput`, the page's script copies it into the hidden `username`), `password`, `sessionDataKey`,
   the hidden `authenticators`, `tenantDomain=carbon.super`, `allLoginParams`, optional `chkRemember`. The
   page's `GET ../logincontext?...` before submitting only asks whether a session exists and is skipped.
3. `/commonauth` → 302 `/oauth2/authorize` → a page whose form auto-posts `code`, `id_token`, `state` to
   `https://www.solarweb.com/Account/ExternalLoginCallback` (`response_mode=form_post`) → sets the Solar.web
   session cookie → 302 to the `ReturnUrl`, which is the chart JSON itself.

`HttpClientHandler` with `AllowAutoRedirect` and a `CookieContainer` does the redirects and cookies; the client
only submits the two forms (`HtmlForm`, a regex form reader - two pages did not justify an HTML parser package).
The `state` and the `solweb_browserid_<hash>` scope differ per round and are never composed. A wrong password
brings the login form back with `authFailure=true`; the client throws `SolarWebLoginException` **after one attempt
and never retries by itself**: Friendly Captcha is loaded on the page and a few rejections bring it up, after which
nothing logs in until a human solves it. The service remembers the failure until the server is restarted.

One client, one cookie jar per server (singleton). `SolarWebLoginTests` drives the real client against a Kestrel
host that plays Solar.web and the login at once - the chain, the form post, the code post-back, a rejected
password, a 429, a 500.

## Settings.xml

```xml
<SolarWeb BaseUrl="https://www.solarweb.com" UserName="you@example.com" ClearTextPassword="…" PvSystemId="<guid>" RefreshMinutes="15" TimeZoneId="Europe/Berlin" />
```

`SolarWebSettings : WebConnection`, so the password is encrypted on the first save like the Toshiba account's.
`PvSystemId` is the GUID of the system's page address. `IsConfigured` needs a user name and a system id; without
either the service says so once and serves nothing (404 from the controller). `TimeZoneId` says when a day is over;
`Misc/TimeZones.Resolve` is the one resolver, shared with `EnergyDataSettings`. `Program.cs` puts the element into
a fresh default file, like the Toshiba and EnergyData ones.

## The cache: `history/SolarWebHistory.db`

`SolarWebHistoryStore`, a second SQLite file beside `PriceAndWeatherHistory.db`, in the same mounted folder. The
plumbing both files share - connection string, `user_version` upgrade steps, one writer, `WriteAsync`,
`WriteInTransactionAsync`, `ReadAsync` - is `SqliteStoreBase` since 2026-09-20; `EnergyHistoryStore` derives from
it too. Tables (`user_version` 1): `SolarWebChart` (rowid `Id`, unique `PvSystemId, ChartInterval, ChartView,
Period`, `FetchedUtc`, captions), `SolarWebSeries (ChartId, Ordinal)`, `SolarWebPoint (ChartId, Ordinal, TimeMs)`
with `Value REAL NULL, Text TEXT NULL`. Point times are **milliseconds** as Solar.web sends them (the battery
states carry seconds), unlike the seconds of the price history. A chart is replaced as a whole (delete the three
tables' rows, insert again, one transaction) - no `ON DELETE CASCADE`, so it does not depend on the foreign-key
pragma. A chart is also **read** in one SQLite transaction (since 2026-09-22): its row, series and points come
from one snapshot, so a reader sees either the old chart or its replacement, never an old row without the child
rows that the replacement has deleted. `View` and `Interval` are SQL keywords, hence `ChartView`,
`ChartInterval`.

**Period** is the cache key's date: the day, the first of the month, 1 January, `DateOnly.MinValue` for GESAMT
(`SolarWebPeriod.Normalize`). `SolarWebPeriod.EndUtc` is the instant a period was over in the system's zone
(`null` for the whole history), `Previous` the period before it.

## The policy (`SolarWebService`, decided 2026-09-20, polling since the evening of that day)

- **The running periods are polled.** Every `RefreshRate` (`RefreshMinutes`, default 15, floor 1) `TickAsync`
  reads the firmware status, then today (production, consumption), this month, this year and GESAMT (all four
  views), and **the periods that ended less than `FinalAfter` (2 days) ago while their charts are not complete**
  (`PeriodsToRefresh`: yesterday and the day before, the previous month on the 1st and 2nd, the previous year on
  1 and 2 January). **Complete** (`TickPredicateAsync`): a day once **every** measured series - not the forecast,
  not the battery bubbles, not a series without a single value such as a Wattpilot the system does not have -
  has a value in the day's last five minutes, 23:55 local (`SolarWebPeriod.IsDayComplete`; *every* since
  2026-09-22, *any one* before: a chart whose consumption stopped at 23:10 while another series stood at zero
  until midnight must not count as whole); a month or a year once the production day chart of its **last day**
  is complete and the month's or year's chart was fetched after that day chart (Solar.web builds the one from
  the other). The tick reads the days before the months and the years for exactly that reason. Solar.web can be
  **hours behind** the inverter, so a day read after midnight may still stop at 21:xx; it is read every tick
  until 23:55 is there, and the `Solar.web answered ...` log line of a day that is over ends in `, complete` or
  `, not complete yet: the data ends at <local time>` (`DescribeCompleteness`, `SolarWebPeriod.LastValueUtc`),
  so the log shows how far behind Solar.web is. **What has not arrived two days after the end never will** (the
  developer's rule, 2026-09-20 evening, repeated 2026-09-22), so nothing older is touched, complete or not. A
  Premium view the account is locked out of (`IsPremiumFeature`) is tried again only every `LockedViewRetry`
  (1 day). The first tick comes one interval after start, not at start. A refusal (429, 503, maintenance, login)
  ends the tick: the rest would be refused the same way. **Any other failure is that one chart's** (since
  2026-09-22): `TryAsync` logs it with the chart's name and the tick goes on, where before an exception the tick
  did not expect - a store that would not write, an answer the parser did not understand - ended the tick and,
  recurring, left every chart after it in the list unread for good, the day that had just ended among them.
  **The timer is re-armed from the end of each tick** (`NextTickDelay`, `RearmTimer`): after `RefreshRate`, or
  as soon as a back-off is over where that is sooner - a 15-minute back-off from a 429 in the middle of a tick
  used to reach past the next tick, which then started inside the block and did nothing, so one 429 cost two
  intervals. The tick does not backfill: last month, last year and older days are fetched only when a client
  asks.
- **A client is served what is cached, however old** (`IsGoodEnoughForAClient`): a period that is over does not
  change, a running one is as fresh as the last tick. Solar.web is asked only for a chart the cache does not
  have - and for a **forecast day** (a period after today), which no tick refreshes, once the cached one is a
  `RefreshRate` old. Before the polling (until 2026-09-20 afternoon) a running period was refetched on demand
  after `RefreshRate` and a period counted as final a day after its end; the developer wanted neither.
- Solar.web is asked **by the period's first day**, whichever day the client named (`FetchChartAsync`).
- **Day charts are kept 30 days** (`DayRetention`): the purge runs at start and after every day chart written,
  `Today - 30 days` as the cutoff. Months, years and GESAMT are kept for good.
- Requests go out **one at a time** (a semaphore, and the cache is checked again after waiting on it) and at least
  `MinimumRequestInterval` (2 s) apart.
- **429, 503 and the maintenance page** make Solar.web "unavailable": the client throws
  `SolarWebRateLimitException` (429, derived) or `SolarWebUnavailableException` (503, or a 200 HTML page whose text
  says "Maintenance Work" / "Wartungsarbeiten" - Fronius' Sunday maintenance, seen 2026-09-20), each with the
  `Retry-After` where there was one. The service blocks every request until `now + RetryAfter`, or
  `DefaultRateLimitBackoff` (15 min) after a 429 and `UnavailableBackoff` (5 min) otherwise; `UnavailableUntil`
  says until when. The message and the log line name that time **in the PV system's zone** (`SolarWebService.Local`),
  which is what the user reads in the client's error box. While blocked, and on any other failure (500, an unknown HTML page, a timeout), a **cached chart
  is served however stale it is**; only without one does the failure reach the caller. The controller answers 503
  with `Retry-After` for the block, 504 for the client's timeout (90 s), 502 for a login failure or any other
  refusal, 400 for a Premium day chart, 404 without the section.
- **An HTML page that is neither the login form nor the code post-back is never a login failure.** It was, until
  the production test of 2026-09-20 hit the maintenance page and the service refused everything until a restart.
  Now such a page is logged as a warning with `HtmlForm.Summarize` (title plus the text without markup, 400
  characters) and thrown as `InvalidDataException` (transient) or, for the maintenance page, as unavailable. Only
  a rejected password (the login form coming back a second time) and a missing password are login failures.
- `GET api/SolarWeb/{day|month|year|all}/{production|consumption|returnofinvestment|expense}/{yyyy-MM-dd?}`,
  role User. The date defaults to the system's today. Enums bind by name, case does not matter.

`SolarWebServiceTests` pins all of this with a settable clock, a fake client and an in-memory store;
`SolarWebHistoryStoreTests` uses a real SQLite file in the temp folder. `FakeSolarWebClient.Lag` is how far the
fake is behind: a day chart's last point is the last slot once the day has been over for that long, otherwise
`min(now, last slot) - Lag`, so a day that has just ended comes back stopping hours short of midnight the way the
real one does; `FailWhen` makes one particular chart throw while every other one is answered.

**2026-09-22, the day that stayed incomplete.** The client showed 21.09 ending at 23:10 on the morning after,
though the policy above was in place. The code implements the rule against the fake (the tests prove it), and
nothing in the repo shows what a real answer of a lagging day looks like, so the cause could not be pinned from
here. The three changes of that day make the service robust against every way found in which a cached day could
stay stale: a series padded to midnight (every series must be there), a chart that keeps failing (the tick goes
on), a back-off that swallows the next tick (re-armed). What was not changed: a rejected login still blocks
everything until a restart, by decision, and the cache is then served stale without an error - the log says
`The Solar.web login failed and is not tried again until the server is restarted`. When a day stays incomplete
again, read the server log for that line, for `Solar.web is left alone until`, and for the `, not complete yet`
notes, before looking at the code.

## The firmware status (added 2026-09-20)

`GET /Firmware/GetComponentUpdateInfos?pvSystemId=<guid>` is what Solar.web's firmware page loads: `data.UpdateInfos[]`,
Pascal case, one entry per component and data source - a Gen24 that reports through two data loggers is listed
twice. Each has `AvailableUpdate` (`InstalledVersion`, `UpdateVersion`, `ChangelogUrl` - a PDF -,
`NewerVersionAvailable`, `IsUpdateAllowed`, `LastUpdate`), `InfoDescription`, `IsOnline`, `UpdateRecommendationInfo`
(`LatestVersionInstalled`, `Empty`, ...) and `UpdateStatus`. **Not every component has firmware Solar.web manages**:
older inverters and third-party batteries come with both versions `null` and `UpdateRecommendationInfo` `Empty`;
`SolarWebFirmwareComponent.IsManagedBySolarWeb` is false for them and they can never be `IsOutdated`.

**Versions are `System.Version`.** Solar.web writes `1.41.11-1`: major, minor, build, and the revision after a dash
(the developer's instruction of 2026-09-20). `SolarWebVersion.Parse` turns the dash into the fourth dot, so the JSON
to the client carries `1.41.11.1` (System.Text.Json's own `Version` form) and `ToSolarWebString` puts the dash back
for display. A version that does not parse is an `InvalidDataException` - the format is fixed, and a change to it
should be seen, not swallowed.

`SolarWebFirmwareStatus : IHaveUniqueId` travels like `EnergyChartData`: `SolarWebService` publishes it to
`IDataControlService` under `SolarWebFirmwareStatus.DeviceId` **whenever it differs from the one before**
(`SameAs`: the components by value, the time stamp does not count) - so the clients get a `SolarWebFirmwareStatus`
hub message when a firmware becomes outdated and again when it has been installed, a connecting client gets the
current one replayed, and `GET api/SolarWeb/firmware` answers it on request (read again where older than
`RefreshRate`). The service polls it every `RefreshRate` (`TickAsync`, a timer whose first tick is one interval
after start, so a slow login never holds up the server; the first status comes with the first client that asks).
The firmware requests go through the same gate and back-off as the charts (`AskAsync`). Users only, like the price
data: `DeviceVisibility` does not list it for guests.

## The client (added 2026-09-20)

- **The chart dialog**: `SolarWebChartViewModel` / `SolarWebChartView`, opened by `MainViewModel.ShowSolarWebChart`
  from the View menu (entry visible while `IUpdateService.HasSolarWeb`), resizeable like the price chart, the same
  row of choices below the plot: the four views as radio buttons (the two Premium ones disabled for a day, and a
  Premium view switches back to production when the day is chosen), the four intervals, a `DatePicker` with a step
  to either side by a day, a month or a year (disabled for GESAMT), and Solar.web's Premium hint where
  `IsPremiumFeature`. The date runs from 2000 to **two days ahead**: Solar.web forecasts that far, and each of
  those days is a day chart of its own (developer's decision 2026-09-20). Every choice is one `GET api/SolarWeb/...` through `IWebClientService.GetSolarWebChart`; a
  load counter drops the answer to a choice the user has already left behind. The Solar.web calls get 30 s
  (`WebClientService.SolarWebTimeout`) where every other request gets 15 s - the timeout is per request since
  2026-09-20, the `HttpClient` itself has none.
- **`SolarWebChartModel`** is the plain-numbers model (no charting type), built from a `SolarWebChart`: **two
  shapes**. A day is drawn **over time** (**exactly the chart's day, local midnight to midnight**, whatever Solar.web
  sent - today's answer carries two days of forecast, which are looked at by stepping, and points outside the day
  neither show nor stretch the axis; local instants; **stacked bottom-up by Highcharts' `index` descending**, because Solar.web stacks
  with `reversedStacks` - direct consumption (6) at the bottom, then the Wattpilot (4), the battery (2), the grid (1),
  the forecast (0); the parser keeps the index (`SolarWebSeries.Index`, cache schema 3), a series without one - a
  chart cached before that - takes the index Solar.web is known to give its id (`knownIndices`), and an unknown
  id without one stays where Solar.web listed it; `areaspline` stacked as areas, `spline` as lines with
  `NaN` gaps where Solar.web sent `null`, `%` on a right axis 0-100); month, year and GESAMT are **categorical**,
  one column per date stacked in series order, the columns labelled by day of month, abbreviated month or year -
  Solar.web stamps them at midnight UTC and they are read as dates, never converted. Because the day is the
  **client's local** one, a test of a day chart derives its instants from the zone it runs in
  (`SolarWebChartModelTests.sep19Midnight`): a written-down 22:00Z is midnight only in summer time in Central
  Europe, and two tests failed in the UTC cloud container for that reason until 2026-09-23. Bubbles
  (`BattOperatingState`) are not drawn. **Colours**: a fixed palette **by series id** wins (`SolarWebChartModel.knownColors`: the two
  battery series green, `FromGenToConsumer` yellow, `ToConsumer` **orange** although Solar.web draws it light
  blue, `PvForecastTruncated` **light blue** (#70AFCD, Solar.web's consumption blue) although Solar.web hatches
  it yellow - the developer's choices of 2026-09-20; the rest Solar.web's own), then Solar.web's colour where it
  sent one (the parser takes the stroke of the forecast's hatch pattern too), then the first fallback colour no
  other series uses. `IsForecast` (`PvForecast*`) is drawn translucent where Solar.web hatches. Disabled radio
  buttons (the Premium views on a day) are at half opacity through `RadioButton.Wrap:disabled` in
  `Styles/CompactForms.axaml`, because the `Wrap` template lost Fluent's disabled look.
- **The tooltip** (since 2026-09-20 evening, the developer wanted Solar.web's): the view's code-behind takes
  `PointerMoved` on the `AvaPlot`, turns the position into a ScottPlot `Pixel` (times `DisplayScale`), checks it
  against `RenderManager.LastRender.DataRect`, maps it with `Plot.GetCoordinates(pixel, Bottom, Left)` and asks
  the model: `TooltipForCategory(round(x))` for the columns, `TooltipForTime(FromOADate(x))` for the day, which
  snaps to the nearest instant of `TimePoints`. The model answers a `SolarWebChartTooltip` (title = date or
  time, rows top of the stack first then the lines, zero columns left out, totals for a column: the view's name
  with the stack's sum and, in production, `SelfConsumption` = 1 - grid/sum; watts written as kW with two
  decimals, `%` without). The view model holds it (`Tooltip`, cleared with `ChartModel`), an Avalonia
  `ContentControl` overlay in the same grid cell binds to it (`FlowCardBackground`/`FlowCardBorder`, not hit
  testable; the rows in tabular figures, `FontFeatures="+tnum"`, the headline not - the developer's request),
  and the code-behind sets its `Margin` beside the pointer, flipping left or up at the edges. A dotted
  `VerticalLine` is the crosshair; `Plot.Reset()` drops it. No touch support yet, the developer's choice.
- **`SolarWebChartRenderer`** draws it with ScottPlot: `Bars` with `ValueBase` for the stacks and `NumericManual`
  ticks for the categories, `FillY` between cumulative arrays for the areas, `Scatter` per stretch for the lines.
  **The ranges are locked with axis rules** (`LockedVertical`, `LockedHorizontal`), not only set: the second chart
  of the dialog came up with -10..10 on the left axis on 2026-09-20 although the model's maximum was set, and the
  cause could not be reproduced headless (three renders in a row into one control all kept 0..max, and the
  plottables all sit on the new bottom axis). A rule is applied on every render, so whatever re-scales the live
  control between two drawings loses; the probe shows an explicit `AutoScale()` and a manual -10..10 undone by the
  next render. Interaction is off anyway. The time axis is created before the plottables, as the price chart does.
  What every chart shares with the price chart is `ChartTheme` ([[EnergyData]]). **An empty plot is themed too**
  (since 2026-09-21): `SolarWebChartView.Render` calls `ChartTheme.Apply` when `ChartModel` is still null, and
  again on `Loaded`, so a dark dialog does not sit on ScottPlot's white until the server answers.
  `SolarWebChartViewTests` pins the empty figure to the dark `DialogBackground`. Verified 2026-09-20 by rendering
  headless with Skia (month, day, dark) - the probe is the scratchpad's, not the repo's.
- **The firmware notice**: `UpdateService` reads `GET api/SolarWeb/firmware` at start (not awaited: the server may
  have to log in to Solar.web first) and after every reconnect, and takes the `SolarWebFirmwareStatus` hub message;
  `HasSolarWeb` is "the server answered a Solar.web request" and gates the menu entry. `MainViewModel` listens and
  shows a `MessageBox` - title, text, one line per component (`FirmwareUpdateNotice.Describe`: family, installed
  -> offered), buttons Show changelog / Close - **once per component and offered version** (`FirmwareUpdateNotice`,
  reset on logout), whatever how often the same status arrives. Show changelog opens every distinct `ChangelogUrl`
  through `IUriLauncher`.
- Strings: `SolarWeb` (a name, neutral only), `Production`, `Profitability`, `Costs`, `Day`, `Month`, `Year`,
  `LoadingSolarWebData`, `SolarWebPremiumHint`, `FirmwareUpdateAvailable(Text)`, `ShowChangelog`, in every culture
  file where the word differs (`fr` has no `Production`). Series names come from Solar.web in the server's culture.

## Not done yet

- The dialog runs against the production server from the client (the developer's screenshot of 2026-09-22 is
  the 21.09 production day chart served from the cache).
- The chart has no tooltips, so the battery state bubbles have nowhere to go; the day chart could show them as
  markers with a hover text once ScottPlot's interaction is wanted.
- The live login **works from the production server** (verified 2026-09-20 against `home.hochstaetter.de`: the
  month chart came back with real data, the cached answer in 0.2 s). The same test showed a day chart taking over
  30 s during Fronius' Sunday maintenance, hence the 90 s timeout. The maintenance page's markup and status code
  were not captured - it was gone by the time it was looked for - so it is recognized by its two headings only.
- Nothing warms the cache, so the first request for an old month goes to Solar.web while the client waits. A
  background fill of past months could come later, paced by the same gate.
- Fronius sells a Solar.web Query API (`api.solarweb.com`, access key) - the fallback if the portal changes.
