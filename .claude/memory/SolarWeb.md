---
paths:
  - HomeAutomationServer/Models/Settings/SolarWebSettings.cs
  - HomeAutomationServer/Models/Settings/SolarWebParameters.cs
  - HomeAutomationServer/Models/SolarWeb/**
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

# Fronius Solar.web: the history charts

Solar.web (`www.solarweb.com`) is Fronius' portal with the long-term history of an inverter. There is no free API;
the server reads what the portal's own chart page reads, logged in as the user. Server side only as of
2026-09-20; the client that draws the charts is not built yet. `HomeAutomationClient/Server only` - the WPF app
is not touched.

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
pragma. `View` and `Interval` are SQL keywords, hence `ChartView`, `ChartInterval`.

**Period** is the cache key's date: the day, the first of the month, 1 January, `DateOnly.MinValue` for GESAMT
(`SolarWebPeriod.Normalize`). `SolarWebPeriod.IsFinal` says whether a period ended more than `FinalAfter` (1 day)
ago in the system's zone; the whole history never is.

## The policy (`SolarWebService`, decided 2026-09-20)

- **Nothing is polled.** A chart is fetched when a client asks and then kept.
- A **final** period is served from the cache for good and never asked again. A **running** period (today, this
  month, this year, GESAMT) is asked again only when the cached chart is older than `RefreshRate`
  (`RefreshMinutes`, default 15, floor 1).
- **Day charts are kept 30 days** (`DayRetention`): the purge runs at start and after every day chart written,
  `Today - 30 days` as the cutoff. Months, years and GESAMT are kept for good.
- Requests go out **one at a time** (a semaphore, and the cache is checked again after waiting on it) and at least
  `MinimumRequestInterval` (2 s) apart.
- **429, 503 and the maintenance page** make Solar.web "unavailable": the client throws
  `SolarWebRateLimitException` (429, derived) or `SolarWebUnavailableException` (503, or a 200 HTML page whose text
  says "Maintenance Work" / "Wartungsarbeiten" - Fronius' Sunday maintenance, seen 2026-09-20), each with the
  `Retry-After` where there was one. The service blocks every request until `now + RetryAfter`, or
  `DefaultRateLimitBackoff` (15 min) after a 429 and `UnavailableBackoff` (5 min) otherwise; `UnavailableUntil`
  says until when. While blocked, and on any other failure (500, an unknown HTML page, a timeout), a **cached chart
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
`SolarWebHistoryStoreTests` uses a real SQLite file in the temp folder.

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

## Not done yet

- **No client.** The models (`SolarWebChart`, `SolarWebSeries`, `SolarWebPoint`, the two enums) are the server's
  by the rule in [[Fronius.SharedLibraryBoundary]]; when the Avalonia client draws them they move to
  `Fronius/Models/SolarWeb` - the JSON does not change, the namespace does. Series `Name` is localized by
  Solar.web to the request's culture; a client should key its legend on `Id`.
- The live login **works from the production server** (verified 2026-09-20 against `home.hochstaetter.de`: the
  month chart came back with real data, the cached answer in 0.2 s). The same test showed a day chart taking over
  30 s during Fronius' Sunday maintenance, hence the 90 s timeout. The maintenance page's markup and status code
  were not captured - it was gone by the time it was looked for - so it is recognized by its two headings only.
- Nothing warms the cache, so the first request for an old month goes to Solar.web while the client waits. A
  background fill of past months could come later, paced by the same gate.
- Fronius sells a Solar.web Query API (`api.solarweb.com`, access key) - the fallback if the portal changes.
