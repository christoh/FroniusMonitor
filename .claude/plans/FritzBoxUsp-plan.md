---
paths:
  - Fronius/Services/FritzBoxService.cs
  - Fronius/Contracts/IFritzBoxService.cs
  - Fronius/Models/FritzBoxDevice.cs
  - HomeAutomationServer/Services/DataCollectors/FritzBoxDataCollector.cs
  - HomeAutomationServer/Models/Settings/FritzBoxDataCollectorParameters.cs
  - FroniusMonitor/Services/DataCollectionService.cs
---

# Fritz!Box: push over USP/MQTT, IPv4 watchdog, extensions

Status: draft of 2026-10-24. Nothing of it is implemented yet.

## Where we are

- Both heads poll the AHA-HTTP interface (`webservices/homeautoswitch.lua?switchcmd=getdevicelistinfos`) through
  `FritzBoxService`. The server's `FritzBoxDataCollector` polls every `RefreshRate` (5 s by default). The WPF app's
  `DataCollectionService` polls on every third cycle (`FritzBoxUpdateRate = 3`).
- The server pushes every Fritz!Box device whole to every client on every poll, whether it changed or not (see
  `UpdateVisibility.Lifecycle.md`).
- AVM has frozen the AHA interface: "will not be developed any further". Its successor, the Smart Home REST API
  (FRITZ!OS 8.20 and later, `https://fritz.box/api/v0`), has no push either.
- TR-064 has no push: every state variable is `sendEvents = no`. Its `X_AVM-DE_Homeauto` service can also do less
  than AHA, with no dimming and no color.
- TR-069 (CWMP) has a single ACS slot that the provider may be using. It would open a new HTTPS session for every
  change. Not pursued.
- **USP (TR-369) is the way in.** The box is a USP agent. TR-064
  `X_AVM-DE_USPController:AddUSPController` adds a controller of our own, next to the provider's and MyFRITZ's.
  - Transport (MTP) is `websocket` or `mqtt`. The MQTT parameters exist since FRITZ!OS 8.01.
  - Rights are flags: `Smarthome`, `Mesh`, `Internet`, `System`, `Controller`, `WiFi`, `VoIP`.
  - Adding a controller needs a second factor: a button on the box, or a code on a phone.
  - Push is `Device.LocalAgent.Subscription.{i}.` with `ValueChange`, `ObjectCreation`/`ObjectDeletion` or `Event`.
- **The open question:** the smart-home part of the box's USP data model is not documented publicly. Nobody knows
  yet which smart-home parameters the box exposes, or whether it accepts `ValueChange` for them. Phase 1.0
  answers this before anything else is built.

## Decisions needed before work starts

| # | Question | Recommendation |
|---|---|---|
| D1 | FRITZ!OS version and box model? | MQTT needs at least 8.01, the REST API needs 8.20. |
| D2 | Which MQTT broker? | A Mosquitto service in `docker-compose.yml`, with ACLs. The HA server and the WPF app are both just clients of it. The alternative, an embedded MQTTnet broker inside the HA server, would make the WPF app depend on the server. |
| D3 | Where does the USP/TR-064 code live? | A new class library, for example `FritzBox/FritzBox.csproj`, referenced by `HomeAutomationServer` and `FroniusMonitor`. By `Fronius.SharedLibraryBoundary.md` it does not belong in `Fronius`, because the Avalonia client does not use it. Duplicating it would break the copy & paste rule. The device models (`FritzBoxDevice` and its parts) stay in `Fronius`. |
| D4 | Does the box ask for a confirmation (button or phone) when IPv6 is switched off in the web UI? | If it does, step 2 cannot be automated as specified. Please check by hand once. |
| D5 | Does the HA server reach the box over the LAN? | See step 2: if it does, the box's LAN IPv4 address does not depend on the provider's IPv4. One command channel is then enough, and the IPv6/IPv4 split is unnecessary. |
| D6 | Is the probe of phase 1.0 committed? | As `Tools/FritzBoxUspProbe` on the feature branch. Delete it once its findings are in a memory document. |
| D7 | Does the provider manage the box through TR-069 or USP? This is "Internet → Zugangsdaten → Anbieter-Dienste", if that tab is shown at all. | Only to know. Our controller does not touch the provider's. |

## Step 1 - Push for everything that works today, in the HA server and the WPF app

### Phase 1.0 - Feasibility probe (gate)

A console tool, run by hand in the house LAN:

1. TR-064 discovery: read `tr64desc.xml` on port 49000 and switch to HTTPS on 49443 with `GetSecurityPort`.
2. Read `X_AVM-DE_USPController:GetInfo` and the existing controllers (`GetUSPControllerByIndex`).
3. `AddUSPController` with MTP `mqtt` against the broker, rights `Smarthome` and `Internet`. Handle the second
   factor: answers 866, 867 and 868, then `X_AVM-DE_Auth:SetConfig(start)`, and poll `GetState` until
   `authenticated`.
4. Over MQTT:
   - `GetSupportedDM` on `Device.` (parameters, events, commands, and the `value_change` flag per parameter).
   - `GetInstances` and `Get` on everything that looks like smart home, internet or hosts.
5. A `ValueChange` subscription on one outlet's switch state and power, a lamp's level, and a temperature.
   Record when the notifications arrive and how often.
6. A `Set` on an outlet's switch state, a lamp's level and color, and the color temperature.
7. Find a writable IPv6 switch for step 2: `Device.IP.IPv6Enable`, `Device.IP.Interface.{i}.IPv6Enable`, or an
   `X_AVM-DE_*` parameter. Read it only. Writing it is tried once by hand, with Christoph present.
8. Save every response as a dump. The dumps become the input for the memory document and, with AINs, MACs and
   host names replaced, the test fixtures.

**Gate.** Each criterion is checked separately.

- **G1:** the smart-home units that the code supports today are visible:
  - outlet: on/off, power, energy, voltage
  - lamp: level, color, color temperature
  - temperature
- **G2:** the AIN can be read, or mapped. `FritzBoxDevice` is keyed by `Ain` (`IHaveUniqueId.SerialNumber`), and
  device visibility and other keys depend on it.
- **G3:** `ValueChange` is allowed at least for the switch states.
- **G4:** `Set` switches. If it does not, commands stay on HTTP and only the reading moves to USP.

**If G1 to G3 fail:** USP is used for step 2 only. Step 1 becomes "polling, but better": the REST API if FRITZ!OS
is at least 8.20, otherwise AHA, plus the server pushing to clients only on change (phase 1.3, last point).

### Phase 1.1 - The `FritzBox` library (see D3)

- **`Tr064Client`:** SCPD discovery, SOAP, HTTP digest authentication, HTTPS on 49443, the second-factor flow of
  `X_AVM-DE_Auth`, and typed calls for the services we use: `X_AVM-DE_USPController`, `WANIPConnection` /
  `WANPPPConnection`, `DeviceInfo`.
- **`UspController`:**
  - MQTTnet client; USP Record and Msg from the Broadband Forum `.proto` files through `Google.Protobuf` and
    `Grpc.Tools`, generated at build time. No `dynamic`.
  - `msg_id` correlation with timeouts.
  - `Notify` with `NotifyResp`.
  - Subscription management: create or reuse by our own `ID`, `NotifRetry = false`, and recreate on start.
  - Reconnect, followed by a full resync.
- **Mapping:** the USP data model is mapped onto the existing `FritzBoxDevice` with `FritzBoxSwitch`,
  `FritzBoxPowerMeter`, `FritzBoxLevel`, `FritzBoxColorControl` and `FritzBoxTemperatureSensor`. That way the JSON,
  the Avalonia client and the WPF UI stay unchanged.
- All logging is in English and guarded by `Logger.IsEnabled`.

### Phase 1.2 - Registering the controller (once, with the second factor)

- One controller per installation, each with its own `EndpointID`: `proto::ha-server-<id>` for the server,
  `proto::froniusmonitor-<machine>` for each WPF installation. Each gets its own MQTT topics.
- Least privilege. The server gets `Smarthome` and, for step 2, `Internet`. The WPF app gets `Smarthome` only.
- Registration is idempotent: look the controller up by `EndpointID` first, and add it only if it is missing.
- Who presses the button:
  - **Server:** an administrator-only endpoint starts the registration. The Avalonia client shows "Press the button
    on the Fritz!Box now" and the state as it progresses.
  - **WPF app:** the same flow in the settings dialog. The logic lives in the view model
    (`ViewModelsForInteractionLogic.md`).
- Every new UI string goes into all languages (`Localization.md`).

### Phase 1.3 - HA server

- `FritzBoxDataCollector` chooses its mode by itself: **USP** when it is configured and connected, otherwise
  **polling** as today.
- **USP mode:**
  - At start: a full `Get`, then `AddOrUpdateAsync`.
  - On `Notify`: apply the change to the cached `FritzBoxDevice`, then `AddOrUpdateAsync` for that device only.
    Changes are coalesced per device over a short window (for example 250 ms), so that a burst of power values is
    one update.
  - A safety resync every few minutes.
  - When the broker connection drops: polling until it is back.
- **Commands** (switch, level, color, color temperature) use USP `Set` if G4 holds, otherwise HTTP as today.
  Either way the user sees the result through the notification that follows. No poll is needed for that.
- **Settings** in `Settings.xml`:
  - broker host, port, TLS, user, and password, encrypted like the other passwords
  - `EndpointID`
  - USP on or off
- `docker-compose.yml` gets a commented Mosquitto service, with an ACL example so that only the box, the HA server
  and the WPF users can use their topics.
- **The server pushes to clients on change only, in every mode.** That is how the Toshiba devices already work.
  `UpdateVisibility.Lifecycle.md` gets updated in the same change.

### Phase 1.4 - WPF app

- `DataCollectionService` uses the same library. The Fritz!Box part becomes event driven and raises
  `NewDataReceived` when something changes.
- Polling with `FritzBoxUpdateRate` stays as the fallback, for when no broker is configured or the broker cannot be
  reached.
- The settings dialog gets the broker fields and "Register with Fritz!Box" (see phase 1.2).
- The WPF app only builds and runs on Windows. In the cloud it is compile-checked with `-p:VerifyOnLinux=true`.

### Phase 1.5 - Improve the HTTP path, which stays as the fallback

- **Login:** PBKDF2 (`login_sid.lua?version=2`) instead of MD5. MD5 stays only for boxes that offer nothing else.
- **HTTP client:** one `HttpClient` per box instead of a new one per request.
- **Certificates:** `ServerCertificateCustomValidationCallback` currently accepts any certificate. Pin the box's
  self-signed certificate on first use instead.
- **Missing connection:** throw `InvalidOperationException` instead of throwing `NullReferenceException` by hand.
- **Optional, only if USP does not cover the writes (G4):** use the REST API instead of AHA when FRITZ!OS is at
  least 8.20.

### Phase 1.6 - Tests and documentation

- **xUnit tests in `HomeAutomationServerTests`:**
  - a round trip through the USP codec
  - the mapping, from the sanitized probe dumps
  - the collector's switch between USP and polling, with a fake `TimeProvider`
  - the server pushing on change only
- **`[SystemFact]` tests against the live box and broker.**
- **Memory:**
  - a new `.claude/memory/FritzBox.Usp.md`, holding the data model as found, the topics, the rights and the
    registration
  - updates to `UpdateVisibility.Lifecycle.md` and `Fronius.SharedLibraryBoundary.md` (the new project)
  - the README and `docker-compose.yml`

## Step 2 - IPv4 watchdog in the HA server

### Behavior as specified

```
Monitoring --(every 30 s: 5 of 5 IPv4 pings fail)--> DisableIpv6
DisableIpv6 --(about 30 s)--> EnableIpv6
EnableIpv6 --(30 s)--> Verify
Verify --(IPv4 works)--> Monitoring
Verify --(IPv4 still down)--> Backoff --> DisableIpv6 (limited, see below)
```

### Details and proposed additions (to be decided)

- **Ping targets** are configurable IPv4 literals, at least two different hosts (for example `1.1.1.1` and
  `9.9.9.9`), so that one host being down does not count as an outage. Pinging uses
  `System.Net.NetworkInformation.Ping`.
  - In the container as user `app`, this needs unprivileged ICMP sockets. Docker allows them by default
    (`net.ipv4.ping_group_range`), but this has to be verified.
- **Guards before acting:**
  - The box answers on the LAN. Otherwise the problem is local and toggling does nothing.
  - **IPv6 to the internet works** (a ping to `2606:4700:4700::1111`). This is the failure mode described ("IPv4
    gone, IPv6 still there"). If both are down, the whole line is down and toggling is pointless.
  - At most N recovery cycles per hour, then stop and report.
- **Never leave IPv6 switched off:**
  - Switch it back on in a `finally`.
  - Persist the state, so that a server restart in the middle of a cycle switches IPv6 back on at start.
- **Option:** re-enable IPv6 after 30 s at the earliest, but only once IPv4 answers again, with an upper limit of
  for example 120 s. The manual procedure works that way: "wait until the connection is back".
- **Diagnostics** before and after each cycle: read `WANIPConnection` or `WANPPPConnection`
  `GetInfo`/`GetStatusInfo` over TR-064. That gives `ConnectionStatus`, `X_AVM-DE_IPv6ConnectionStatus`, the
  external IPv4 address and `LastConnectionError`, which are logged for the conversation with the provider.
- **Command channel:** TR-064 has no action that switches IPv6 on or off. It can only read the status.
  - **A:** USP `Set` on the parameter found in phase 1.0 step 7. Preferred.
  - **B:** replay the request the web UI sends (`data.lua`). Christoph captures it once in the browser's developer
    tools while switching. This is fragile across FRITZ!OS versions, so it is the fallback only.
- **"Via IPv6 / via IPv4"** (see D5): the HA server sits in the LAN, so it talks to the box's LAN IPv4 address. That
  address does not depend on the provider's IPv4, and the MQTT connection to the broker is LAN IPv4 too. So one
  channel suffices for both switching off and switching on. If the server reaches the box from outside, the
  specified split stays.
- **Outage log:** start, detection, every action, recovery, duration. Stored in SQLite under `history` (the same
  place as the price and weather history). The log entries are in English.
- **Notification:** a SignalR message to administrators on detection, on recovery, and on giving up.
- **Settings** in `Settings.xml`: on/off (off by default), targets, interval, ping count, the delays, and the cycle
  limit.
- **Tests:** the state machine with a fake `TimeProvider`, a fake pinger and a fake box.
- **Order:** with channel B, step 2 does not depend on step 1 and can be done first if the outages hurt more.

## Step 3 - Additions we cannot do today

The HA server and the Avalonia client, with roles as usual: `User` reads, `PowerUser` switches, `Operator`
configures. The WPF app only if wanted, because it is being ported away from.

| Priority | Addition | Needs |
|---|---|---|
| High | **Outlets switched by PV surplus, price or battery state of charge** on the server, the way the Wattpilot charging rules work: on above a surplus for a minimum time, off below, with hysteresis and minimum on and off times, and a priority relative to the Wattpilot. Push makes the reaction immediate. | Step 1 |
| High | **Internet status** in the client: IPv4/IPv6 state, external addresses, uptime, throughput, and the outage history from step 2. | Step 2, TR-064 or USP |
| Medium | **More smart-home device types:** thermostats (target temperature, window open, boost), window and door contacts and alarm sensors, buttons (a press triggers a rule on the server, for example a Wattpilot boost), humidity, blinds. | USP or REST; push for buttons and contacts |
| Medium | **Templates and routines** of the box: apply a template, enable or disable a routine. | REST (8.20) or USP |
| Medium | **Energy history per outlet** in the history charts. | REST statistics. Caution: every request for them triggers a radio request on the box. |
| Medium | **Presence:** which phones are in the WLAN, as an input to rules (air conditioning off when nobody is home, and so on). | USP `Device.Hosts.Host.{i}.Active` with `Mesh` rights |
| Low | **Guest WLAN** on/off with a QR code. **WAN reconnect** button for `Operator`. | USP with `WiFi`/`Internet` rights, TR-064 |
| Low | **Box housekeeping:** FRITZ!OS version, update available, event log (like the Solar.web messages). | TR-064 `DeviceInfo` |
| Optional | **Telephony:** a notification for incoming calls (call monitor, TCP 1012), answering machine messages. | Call monitor, TR-064 `X_AVM-DE_TAM` |
| Follow-up | **The Home Automation MCP connector:** tools for whatever of the above gets built. Its code is not in this repository. | - |

## Risks

- **The data model can change.** AVM documents neither the USP data model nor the smart-home part of it, and
  reserves the right to change it. Mitigations: the polling fallback stays, and `[SystemFact]` tests show a change
  after a FRITZ!OS update.
- **The broker becomes a way to control the box.** Anyone allowed to publish to the agent's topic controls the box
  within the granted rights. Hence ACLs, TLS if the box takes it, and the smallest set of rights.
- **Controllers stay registered** on the box until they are deleted. A WPF installation that is uninstalled leaves
  one behind. "Unregister" belongs in the settings.
- **The second factor needs someone at the box.** It is needed once per controller, not in normal operation.

## Sources

- FRITZ! interfaces: https://fritz.com/en/pages/interfaces
- TR-064 `X_AVM-DE_USPController`: https://fritz.support/resources/TR-064_USP_Controller.pdf
- TR-064 `ManagementServer`: https://fritz.support/resources/TR-064_Management_Server.pdf
- TR-064 First Steps (rights, second factor): https://fritz.support/resources/TR-064_First_Steps.pdf
- TR-064 `X_AVM-DE_Auth`: https://fritz.support/resources/TR-064_Authentication.pdf
- TR-064 `WANIPConnection`: https://fritz.support/resources/TR-064_WAN_IP_Connection.pdf
- Smart Home REST API 0.9.5: https://fritz.support/resources/SmarthomeRestApiFRITZOS82.yaml
- AHA-HTTP interface: https://fritz.support/resources/AHA-HTTP-Interface.pdf
- FRITZ!OS 8.01 release note (USP over MQTT): https://netcon.store/media/pdf/33/1e/82/Release-Note-FRITZ-Box-6690-Cable-FRITZ-OS-08-01-Release-AVMH6h96qOlN30c6.pdf
- USP specification and `.proto` files: https://usp.technology
