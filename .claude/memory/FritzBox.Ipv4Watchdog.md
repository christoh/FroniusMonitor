---
paths:
  - FritzBox/Internet/**
  - HomeAutomationServer/Services/Ipv4Watchdog/**
  - HomeAutomationServer/Services/Ipv4WatchdogStore.cs
  - HomeAutomationServer/Contracts/IIpv4WatchdogStore.cs
  - HomeAutomationServer/Contracts/IIpv4WatchdogService.cs
  - HomeAutomationServer/Contracts/IPinger.cs
  - HomeAutomationServer/Models/Ipv4Watchdog/**
  - HomeAutomationServer/Models/Settings/Ipv4WatchdogSettings.cs
  - HomeAutomationServer/Models/Settings/Ipv4WatchdogParameters.cs
  - HomeAutomationServer/Controllers/Ipv4WatchdogController.cs
  - HomeAutomationServerTests/UnitTests/FritzBoxIpv6ClientTests.cs
  - HomeAutomationServerTests/UnitTests/Ipv4WatchdogServiceTests.cs
  - HomeAutomationServerTests/UnitTests/Ipv4WatchdogStoreTests.cs
  - HomeAutomationServerTests/UnitTests/Fakes/FakeIpv6Box.cs
---

# The IPv4 watchdog of the Swiss FRITZ!Box

Step 2 of `.claude/plans/FritzBoxUsp-plan.md`. The provider of the box in Switzerland (FRITZ!Box 7590, FRITZ!OS
8.25) sometimes drops IPv4 and keeps IPv6. Switching the box's IPv6 off and on again brings IPv4 back; the HA
server in Neufahrn does that now by itself. Built on 2026-10-27; the plan has the decisions (D10, D11) and the live
test that everything here rests on.

## Switching IPv6: `FritzBox/Internet/Ipv6Client.cs`

- **The web interface's own request**, captured in the browser: `GET` and `PUT`
  `api/v0/generic/sync_groups/sg/sg1001` with `Authorization: AVM-SID`, through `FritzBoxHttpClient`
  (`GetRestAsync`/`SendRestAsync`), so login and session handling are the smart home's ([[FritzBox.SmartHome]]).
  AVM does not publish it; a FRITZ!OS update may change it. TR-064 can only read the IPv6 status, USP has it
  read-only ([[FritzBox.Usp]]), and `data.lua` is no longer used for it by the web interface.
- **The body is `{"ipv6_mode": "…"}` alone** (`ipv6_off`, `ipv6_automatic`). The web interface sends the whole
  group of 35 fields; the box takes the one field and leaves the others as they were (compared field by field in
  the live test). So nothing stale is written back.
- **The box answers 200 before it acts**, and then reconnects, IPv4 included, for 10 to 20 s. The answer proves
  nothing; reading the mode back once the box answers again does.
- **The session expires between cycles.** The watchdog asks the box only during a cycle, so its session is long
  gone by the next outage (20 minutes without a request end it), and the REST API refuses a stale session with
  400 and error code 3001 ("permission denied"). On 2026-10-28 that stopped every cycle of the night and the
  morning, because `FritzBoxHttpClient` logged in again on 401 and 403 only. It does on 400 with code 3001 too since
  then ([[FritzBox.SmartHome]]).

## The watchdog: `HomeAutomationServer/Services/Ipv4Watchdog/Ipv4WatchdogService.cs`

A tick-driven state machine like `EnergyDataCollector` ([[EnergyData]]): a timer of the injected `TimeProvider`
calls `TickAsync` every `Ipv4WatchdogParameters.TickInterval` (5 s), one tick at a time (`tickSemaphore`; the timer
skips while one runs), and each tick does what is due. Nothing sleeps, which is what makes the tests possible.

| Phase | What a tick does when it is due |
|---|---|
| `Monitoring` | Every `IntervalSeconds` (30): up to `PingCount` (5) pings to `PublicAddress`; one answer is enough. None: open an outage, then the guards, then a cycle. |
| `SwitchingOn` | `SwitchOnDelaySeconds` (30) after switching off, and then every tick until the box answers: switch back to the mode from before. |
| `Verifying` | `VerifyDelaySeconds` (30) after switching on, once the box answers: read the mode back, ping once more, store the result. |

**The guards before a cycle**, each logged once per reason (`Hold`):
- one of `ReferenceAddresses` answers - otherwise it is this side's IPv4 that is down;
- fewer than `MaximumCyclesPerHour` (3, D11) cycles started in the last hour, **test cycles included** - counted in
  the store, so a restart does not reset it;
- the box answers through the tunnel, and its mode is not `ipv6_off` - IPv6 switched off by somebody else is theirs.

**Restart safety:** the cycle is stored *before* the switch-off. A cycle without a result found at start puts the
watchdog into `SwitchingOn` at once; `SwitchOnAsync` reads the mode and switches only when the box is not in the
mode from before. One that had switched back already (`SwitchedOnUtc` set) only gets its check.

**What a cycle can end in** (`Ipv4WatchdogCycleResult`): `Ipv4Back`, `Ipv4StillDown` (the next check may start
another cycle, within the limit), `Failed` (the switch-off was refused, or the box reports another mode after
switching back). Somebody switching IPv6 back on during the wait is not switched again, only noted in `Detail`.

**The box unreachable after the switch:** the watchdog keeps trying every tick and logs one error after
`UnreachableWarningMinutes` (5), saying that IPv6 may have to be switched on at the box. It cannot do more: with
IPv6 off and IPv4 still down, the box has no way in at all.

**A host that cannot ping** throws `PingException` from `IcmpPinger`, which the tick logs (`RepeatedFailure`); it is
never read as "no answer", so it cannot start a cycle. The container runs as user `app`, so .NET needs unprivileged
ICMP sockets, which `net.ipv4.ping_group_range` of the Pi has to allow (`network_mode: host` makes it the host's).
It does: the checks were answered from the first deployment on (2026-10-27).

## Which way the watchdog reaches the box

`BaseUrl` is `http://192.168.3.1`: the HA server reaches the Swiss box's LAN through the box in Neufahrn and the
**direct WireGuard tunnel between the two boxes**, which runs over IPv6 and so stays up while IPv4 is down. The
live test showed:
- both tunnels of the Swiss box move to IPv4 when IPv6 is switched off, and come back within about 28 s;
- the direct tunnel to Neufahrn went back to IPv6 by itself within 7 minutes; the tunnel to amazon2 stayed on IPv4
  for 35 minutes after the live test, because amazon2 keeps the address the last handshake came from, and went back
  to IPv6 within 10 s after the first test cycle - it varies. `fritzbox.hochstätter.ch` goes through amazon2 and is
  **not** the way for the watchdog.

When IPv4 fails while the direct tunnel happens to be on IPv4, the watchdog cannot reach the box and only logs it
(`Hold`). Pulling amazon2's tunnel back to IPv6 as a second way in is still to be decided (plan, "Later").

## The outage log: `history/Ipv4Watchdog.db`

`Ipv4WatchdogStore : SqliteStoreBase`, the third SQLite file beside the price history and the Solar.web cache, in
the same mounted folder. Schema 1:
- `Outage`: `LastGoodUtc`, `DetectedUtc`, `RecoveredUtc`, `Recovery` (`ByItself`, `AfterCycle`)
- `Cycle`: `OutageId` (none for a test), `IsTest`, `StartedUtc`, `PreviousMode`, `SwitchedOffUtc`, `SwitchedOnUtc`,
  `FinishedUtc`, `Result`, `Detail`

Unix seconds, enums by name, `OutageId` a plain column without a foreign key. A negative count reads nothing, and an
update of an unknown id writes nothing.

## Settings and endpoints

`<Ipv4Watchdog BaseUrl=… UserName=… ClearTextPassword=… PublicAddress=… />` in `Settings.xml`, an
`Ipv4WatchdogSettings : WebConnection`, so the password is encrypted on the first save (`Settings.Save` updates its
checksum). Without `PublicAddress`, `UserName` or `BaseUrl` nothing is watched; an address that does not parse, or
an outage log that cannot be opened, logs an error and leaves the watchdog stopped instead of failing the server.
`CertificateFingerprint` works as for the smart home (`UntrustedCertificatePolicy`, public since then). The box user
is one of its own for the HA server (D11) with the right "FRITZ!Box Einstellungen".

`Ipv4WatchdogController`, both for `Operator`:
- `GET api/Ipv4Watchdog`: phase, last check, the outage and cycle that run, the newest 20 of each from the log.
- `POST api/Ipv4Watchdog/test-cycle`: 202, and the cycle starts at the next tick although IPv4 works (D11: to try
  the installed watchdog without an outage). 409 while a cycle runs, 404 without a watchdog. It interrupts the
  Swiss box's internet twice for about 20 s.

## On the Pi

Deployed on 2026-10-27 with `dev`. The first test cycle took 71 s and ended in `ipv4Back`: IPv6 off 2 s after the
request, back on 33 s later at the first try after the delay, checked 35 s after that. The plan (phase 2.3) has the
timeline next to what amazon2 saw.

On 2026-10-28 the night and the morning brought seven outages (03:53 to 10:03 UTC). The first six ended by
themselves after 5 to 20 minutes; for none of them a cycle was started, because of the stale session above, and the
log said "the box does not answer" for a box that did answer, with an error. The message names the error now.

## Tests

- `FritzBoxIpv6ClientTests`: the request, the body, the session, a mode other than off and automatic, an answer
  without a mode, a refused PUT.
- `Ipv4WatchdogServiceTests`: `ManualTimeProvider`, ticks by hand, a fake pinger, the real store in a temp file, and
  `FakeIpv6Box` behind the real `FritzBoxHttpClient`. `FakeIpv6Box.IsReachable` and `AfterPut` play the reconnect.
- `Ipv4WatchdogStoreTests`: a real file, every field round-tripped, the restart.
