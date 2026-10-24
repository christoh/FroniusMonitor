---
paths:
  - Fronius/Services/FritzBoxService.cs
  - Fronius/Contracts/IFritzBoxService.cs
  - Fronius/Models/FritzBoxDevice.cs
  - HomeAutomationServer/Services/DataCollectors/FritzBoxDataCollector.cs
  - HomeAutomationServer/Models/Settings/FritzBoxDataCollectorParameters.cs
  - FroniusMonitor/Services/DataCollectionService.cs
  - FritzBox/**
  - Tools/FritzBoxUspProbe/**
---

# Fritz!Box: push over USP/MQTT, IPv4 watchdog, extensions

Status of 2026-10-24: phase 1.0 is built (`FritzBox/FritzBox.csproj`, `Tools/FritzBoxUspProbe`) and installed on
amazon2 together with Mosquitto. `info` has run; `register` waits for somebody at the box in Neufahrn. Nothing else
is implemented yet.

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

## Decisions

| # | Question | Answer |
|---|---|---|
| D1 | FRITZ!OS version and box model? | FRITZ!Box 5690 Pro with FRITZ!OS 8.25 (`info`, 2026-10-24). MQTT for USP (8.01) and the REST API (8.20) are both there. |
| D2 | Which MQTT broker? | Mosquitto on amazon2 (AWS), with TLS on port 8883 and the Let's Encrypt certificate of `amazon2.hochstaetter.de`, decided on 2026-10-24. The box insists on TLS, and amazon2 already has a renewed certificate. Push from the box depends on the internet and AWS; without them the HA server falls back to polling. The HA server and the WPF app are both clients of it. Files in `Tools/FritzBoxUspProbe/amazon2`. |
| D3 | Where does the USP/TR-064 code live? | `FritzBox/FritzBox.csproj`, as recommended. It is not part of `Fronius`, because the Avalonia client does not use it. The device models (`FritzBoxDevice` and its parts) stay in `Fronius`. |
| D4 | Does the box ask for a confirmation (button or phone)? | The box in Neufahrn has it **on** (`X_AVM-DE_Auth:GetInfo`, 2026-10-24). Adding a USP controller needs somebody at the box once: a button, or the code on a phone connected to it. The code handles it. Whether the Swiss box asks when IPv6 is switched in its web UI is still to be checked. |
| D5 | How does the HA server reach the box of step 2? | It is a different box, in Switzerland. The HA server and the smart-home box (192.168.44.11) are in Neufahrn. Step 2 below is written for that. |
| D6 | Is the probe of phase 1.0 committed? | `Tools/FritzBoxUspProbe` on `feature/fritzbox-usp`. Delete it once its findings are in `.claude/memory/FritzBox.Usp.md`. |
| D7 | TR-069 on the box? | Switched off by Christoph. Not needed: the controller is added through TR-064. The USP agent still runs: the box's MyFRITZ controller is enabled (`info`, 2026-10-24). |

## Step 1 - Push for everything that works today, in the HA server and the WPF app

### Phase 1.0 - Feasibility probe (gate)

A console tool, run by hand in the house LAN:

1. TR-064 discovery: read `tr64desc.xml` on port 49000 and switch to HTTPS on 49443 with `GetSecurityPort`.
2. Read `X_AVM-DE_USPController:GetInfo` and the existing controllers (`GetUSPControllerByIndex`).
3. `AddUSPController` with MTP `mqtt` against the broker, rights `Smarthome` and `Internet` (the latter for what
   step 3 might read about the internet connection). Handle the second
   factor: answers 866, 867 and 868, then `X_AVM-DE_Auth:SetConfig(start)`, and poll `GetState` until
   `authenticated`.
4. Over MQTT:
   - `GetSupportedDM` on `Device.` (parameters, events, commands, and the `value_change` flag per parameter).
   - `GetInstances` and `Get` on everything that looks like smart home, internet or hosts.
5. A `ValueChange` subscription on one outlet's switch state and power, a lamp's level, and a temperature.
   Record when the notifications arrive and how often.
6. A `Set` on an outlet's switch state, a lamp's level and color, and the color temperature.
7. Note any writable IPv6 switch (`Device.IP.IPv6Enable`, `Device.IP.Interface.{i}.IPv6Enable`, `X_AVM-DE_*`).
   Step 2 cannot use USP for the Swiss box (see there), so this is for the record only.
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

**If G1 to G3 fail:** USP is not used for now. Step 1 becomes "polling, but better": the REST API if FRITZ!OS
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
- Least privilege. The server gets `Smarthome`, and `Internet` only once step 3 reads something with it. The WPF
  app gets `Smarthome` only.
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

### The setting

- **The HA server** is in Neufahrn (192.168.44.50). **The box with the IPv4 problem** is in Switzerland:
  - LAN 192.168.3.1, reachable from Neufahrn through the site-to-site WireGuard tunnel of the two boxes. TR-064
    answers through the tunnel (checked on 2026-10-24).
  - Public IPv4 178.21.230.55 (fixed) and IPv6 2a01:aec0:8000:1::788d, the A and AAAA records of
    `li9ldnoisizdei41.myfritz.net`.
  - Also reachable as `https://fritzbox.hochstaetter.de/` through nginx on AWS (A 3.77.190.163, AAAA
    2a05:d014:9eb:d01:1b::2), over HTTPS only.
  - FRITZ! does not allow a port forwarding to the box itself.
- **The tunnel probably goes down together with IPv4**, so it is no way in during an outage. Which address family
  the WireGuard tunnel uses is still to be found out.

### Details and proposed additions (to be decided)

- **Detection:** five pings to the box's fixed IPv4 178.21.230.55 every 30 s. Whether the box answers pings from
  the internet is still to be checked. If it does not, connect to its HTTPS port over IPv4 instead.
  `System.Net.NetworkInformation.Ping` works in the container, which runs as root with `network_mode: host`.
- **Guards before acting:**
  - **Neufahrn's own IPv4 works** (a ping to, for example, `1.1.1.1` or `9.9.9.9`). Otherwise the problem is on
    this side and toggling the Swiss box does nothing.
  - **The Swiss box answers over IPv6.** That is the failure mode described ("IPv4 gone, IPv6 still there"). If
    both are down, the line is down and toggling is pointless.
  - At most N recovery cycles per hour, then stop and report.
- **Switching IPv6 off has to go over IPv6.** Two ways, to be chosen:
  1. **Directly from the HA server** to `[2a01:aec0:8000:1::788d]`. This needs IPv6 internet at the HA server.
     Christoph's laptop has it; the HA server is still to be checked (`ping -6` on the Pi).
  2. **Through nginx on AWS**, with two names that each reach the box over one address family only (`resolver …
     ipv4=off` or `ipv6=off`, upstream by the MyFRITZ name). This works without IPv6 in Neufahrn.
- **Switching IPv6 on again goes over IPv4:** through the tunnel (192.168.3.1) once it is up again, otherwise to the
  public IPv4 or through nginx.
- **Command channel:** the request the box's web UI sends (`data.lua`), which Christoph captures once in the
  browser's developer tools while switching.
  - It goes over the box's remote HTTPS access, and needs a box user who may connect from the internet.
  - It is fragile across FRITZ!OS versions.
  - TR-064 can only read the IPv6 status, not switch it.
  - USP over the broker on amazon2 might work after all: the broker is reachable over IPv4 and IPv6, so the Swiss
    box could keep its MQTT connection over IPv6 while IPv4 is down. Whether its MQTT client falls back to IPv6 is
    to be tested before relying on it.
- **Never leave IPv6 switched off:**
  - Switch it back on in a `finally`.
  - Persist the state, so that a server restart in the middle of a cycle switches IPv6 back on at start.
- **Option:** switch IPv6 back on after 30 s at the earliest, but only once IPv4 answers again, with an upper limit
  of for example 120 s. The manual procedure works that way: "wait until the connection is back".
- **Diagnostics** before and after each cycle, through the tunnel whenever it is up: `WANIPConnection` or
  `WANPPPConnection` `GetInfo`/`GetStatusInfo` over TR-064. That gives `ConnectionStatus`,
  `X_AVM-DE_IPv6ConnectionStatus`, the external IPv4 address and `LastConnectionError`, which are logged for the
  conversation with the provider.
- **Outage log:** start, detection, every action, recovery, duration. Stored in SQLite under `history` (the same
  place as the price and weather history). The log entries are in English.
- **Notification:** a SignalR message to administrators on detection, on recovery, and on giving up.
- **Settings** in `Settings.xml`: on/off (off by default), addresses, targets, interval, ping count, the delays, and
  the cycle limit.
- **Tests:** the state machine with a fake `TimeProvider`, a fake pinger and a fake box.
- **Order:** step 2 does not depend on step 1 and can be done first if the outages hurt more.

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
