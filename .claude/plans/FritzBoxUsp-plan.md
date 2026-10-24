---
paths:
  - Fronius/Contracts/IFritzBoxService.cs
  - Fronius/Models/FritzBoxDevice.cs
  - HomeAutomationServer/Services/DataCollectors/FritzBoxDataCollector.cs
  - HomeAutomationServer/Models/Settings/FritzBoxDataCollectorParameters.cs
  - FroniusMonitor/Services/DataCollectionService.cs
  - FritzBox/**
  - Tools/FritzBoxUspProbe/**
---

# Fritz!Box: smart home over the REST API, IPv4 watchdog, extensions

Status of 2026-10-24: phase 1.0 (the USP probe) is done. Push over USP works, but only for the outlets, and the
lamps are missing from it entirely. **Step 1 therefore goes on without USP, over the Smart Home REST API.** The USP
setup (Mosquitto on amazon2 and the controller on the box) stays for now, in case step 2 needs it for the Swiss
box. **Phase 1.1 (the REST client) is done and tested live in Neufahrn. Phase 1.2 puts it into the HA server and
phase 1.3 into the WPF app** (`.claude/memory/FritzBox.SmartHome.md`). Phases 1.4 and 1.5 are still to do.

## Where we are

- Before step 1, both heads polled the AHA-HTTP interface (`webservices/homeautoswitch.lua?switchcmd=getdevicelistinfos`)
  through `FritzBoxService`, and the server pushed every Fritz!Box device whole to every client on every poll.
  - The server has used `FritzBoxSmartHomeService` since phase 1.2 (REST API, AHA before FRITZ!OS 8.20). Its
    `FritzBoxDataCollector` polls every `RefreshRate`, which `Program.cs` sets to 2 s, and pushes a device only
    when it has changed.
  - The WPF app has used the same service since phase 1.3. It reads the box on every third cycle
    (`FritzBoxUpdateRate = 3`), as before. The old `FritzBoxService` is deleted.
- AVM has frozen the AHA interface: "will not be developed any further". Its successor is the Smart Home REST API
  (FRITZ!OS 8.20 and later, `https://<box>/api/v0`). It has no push either.
- TR-064 has no push: every state variable is `sendEvents = no`. Its `X_AVM-DE_Homeauto` service can also do less
  than AHA, with no dimming and no color.
- TR-069 (CWMP) has a single ACS slot that the provider may be using. Not pursued.
- **USP (TR-369), tested in phase 1.0 against the box in Neufahrn.** The details are in
  `.claude/memory/FritzBox.Usp.md`.
  - Outlets: switch, power, energy and temperature are there. A switch is pushed within about a second, and
    switching over USP works.
  - Lamps (FRITZ!Smart Light 500) have no capabilities over USP at all.
  - Power changes on the box only every two minutes, so pushing it gains nothing over polling.
  - At most 32 subscriptions, one path each. The box insists on TLS, which needs a broker with a public
    certificate (now on amazon2).
  - What USP would add over polling is the switch state a few seconds earlier. That is not worth a second
    transport, a broker on AWS and its dependency on the internet, while lamps and measurements need HTTP anyway.

## Decisions

| # | Question | Answer |
|---|---|---|
| D1 | FRITZ!OS version and box model? | FRITZ!Box 5690 Pro with FRITZ!OS 8.25 (`info`, 2026-10-24). The REST API (8.20) is there. |
| D2 | Which MQTT broker? | Mosquitto on amazon2 (AWS), with TLS on port 8883 and the Let's Encrypt certificate of `amazon2.hochstaetter.de`, set up on 2026-10-24 for the probe. Files in `Tools/FritzBoxUspProbe/amazon2`. Not used by step 1. |
| D3 | Where does the Fritz!Box code live? | `FritzBox/FritzBox.csproj`: TR-064, USP, and now the REST client. It is not part of `Fronius`, because the Avalonia client does not use it. The device models (`FritzBoxDevice` and its parts) stay in `Fronius`. |
| D4 | Does the box ask for a confirmation (button or phone)? | The box in Neufahrn has it **on** (`X_AVM-DE_Auth:GetInfo`, 2026-10-24). Adding the USP controller needed one button press. Whether the Swiss box asks when IPv6 is switched in its web UI is still to be checked. |
| D5 | How does the HA server reach the box of step 2? | It is a different box, in Switzerland. The HA server and the smart-home box (192.168.44.11) are in Neufahrn. Step 2 below is written for that. |
| D6 | Is the probe of phase 1.0 committed? | `Tools/FritzBoxUspProbe` on `feature/fritzbox-usp`. Its findings are in `.claude/memory/FritzBox.Usp.md`. It can go once step 2 has decided about USP. |
| D7 | TR-069 on the box? | Switched off by Christoph. Not needed. |
| D8 | USP or REST API for step 1? | The REST API, proposed on 2026-10-24 after the probe. USP stays an option for the switch states only (see the end of step 1). |
| D9 | What happens to the USP setup? | It stays: Mosquitto and the security group `mosquitto-mqtts` on amazon2, and the controller `fqdn:hochstaetter.de:home-automation` on the box in Neufahrn. Christoph decided this on 2026-10-24, because step 2 may need USP for the Swiss box. It is removed once step 2 has decided against it. |

## Step 1 - Better smart home in the HA server and the WPF app

### Phase 1.0 - USP feasibility probe (done)

The gate is in `.claude/memory/FritzBox.Usp.md`:
- G1: partly. Outlets yes, lamps no.
- G2 (AIN): yes.
- G3 (push): yes.
- G4 (switching over USP): yes.

### Phase 1.1 - REST client in the `FritzBox` library (done)

Built as `FritzBoxSmartHomeService` in `FritzBox/SmartHome`. What turned out differently from the points below, and
what was measured, is in `.claude/memory/FritzBox.SmartHome.md`. In short:
- The version comes from `jason_boxinfo.xml`.
- AHA is the fallback inside the same service, over the same `FritzBoxHttpClient`.
- The overview shows a command about a second later.
- The REST API has no numeric `id`, so one is made from the UID.

- **Login:** `login_sid.lua?version=2` with PBKDF2, then the SID in `Authorization: AVM-SID <sid>`. Log in again
  when the SID has expired. The user needs the right "Smart Home".
- **Reading:** `GET /api/v0/smarthome/overview` returns devices, units, groups, templates, triggers and globals in
  one call. That is one request per poll for everything.
- **Writing:**
  - `PUT /api/v0/smarthome/overview/units/{UID}` for on/off, level, color and color temperature.
  - `POST .../templates/{UID}` to apply a template, `PUT .../triggers/{UID}` to switch a routine on or off.
- **Statistics:** not polled. AVM warns that each request triggers a radio request to the outlet.
- **Mapping onto the existing `FritzBoxDevice`** with its switch, power meter, level, color and temperature parts,
  so that the JSON, the Avalonia client and the WPF UI stay unchanged.
  - The REST API counts level 0..100 where AHA counts 0..255, and power in mW where AHA counts 0.01 W.
  - The AIN is the key, as now.
- **AHA stays as the fallback** for boxes without the REST API (before FRITZ!OS 8.20). The client reads the version
  from `tr64desc.xml` or `/jason_boxinfo.xml`.
- All logging is in English and guarded by `Logger.IsEnabled`.

### Phase 1.2 - HA server (done)

Built as described below. How the collector works is in `.claude/memory/FritzBox.SmartHome.md`. Beyond the plan:
- It withdraws devices the box no longer has.
- A device that did not change still takes the values of every poll, because that lifts the lock a command puts on
  it.
- The failure logging of the Gen24 collector moved into `RepeatedFailure`, so that both collectors share it.
- Templates and routines are in the library (`SmartHomeClient`) but not exposed by the server yet; that is step 3.

- `FritzBoxDataCollector` polls the overview, every 2 to 5 s (configurable, as now).
- **The server pushes to clients on change only.** Today it pushes every device on every poll; this changes it to
  work the way the Toshiba devices already do. `UpdateVisibility.Lifecycle.md` gets updated in the same change.
- **Commands** go over the REST API. A poll about a second after a command makes the result visible; one at once
  still shows the old state (measured 2026-10-24).
- Register `FritzBoxSmartHomeService` instead of `Fronius/Services/FritzBoxService.cs`. Once the WPF app has switched
  too (1.3), delete the old service: its AHA commands exist twice until then.
- **Templates and routines** become available on the way, for step 3.

### Phase 1.3 - WPF app (done)

- `App.xaml.cs` registers `FritzBoxSmartHomeService`.
- `Fronius/Services/FritzBoxService.cs` is deleted, together with its two strings, which nothing else used.
- The polling of `DataCollectionService` is unchanged. Its power button already waits a second after a command,
  which is what the REST API needs.
- Compile-checked with `-p:VerifyOnLinux=true`. Not run, because WPF does not run off Windows.

- `DataCollectionService` uses the same library and the REST API, with AHA as the fallback.
- The WPF app only builds and runs on Windows. In the cloud it is compile-checked with `-p:VerifyOnLinux=true`.

### Phase 1.4 - Improve the HTTP path

- **HTTP client:** one `HttpClient` per box instead of a new one per request.
- **Certificates:** `ServerCertificateCustomValidationCallback` currently accepts any certificate. Address the box by
  the name its certificate is for (`hgm-fs.dns.army` in Neufahrn), or pin the certificate.
- **Missing connection:** throw `InvalidOperationException` instead of throwing `NullReferenceException` by hand.

### Phase 1.5 - Tests and documentation

- **xUnit tests in `HomeAutomationServerTests`:**
  - the REST JSON mapped onto `FritzBoxDevice`, from a sanitized overview of the box
  - the PBKDF2 login
  - the server pushing on change only
- **`[SystemFact]` tests against the live box.**
- **Memory:** a document for the REST client; updates to `UpdateVisibility.Lifecycle.md` and
  `Fronius.SharedLibraryBoundary.md` (the new project).

### Later, if wanted: switch states over USP

The library can do it already. It would take 13 subscriptions for 13 outlets, plus two for new and removed
devices, out of 32. It makes switching from elsewhere (the button on the outlet, the FRITZ! app, templates) visible
at once instead of within the polling interval. Not planned for now.

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
    box could keep its MQTT connection over IPv6 while IPv4 is down. That needs a controller on the Swiss box (one
    button press there), and it is to be tested whether its MQTT client falls back to IPv6. The library and the
    broker are ready for it (D9).
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
| High | **Outlets switched by PV surplus, price or battery state of charge** on the server, the way the Wattpilot charging rules work: on above a surplus for a minimum time, off below, with hysteresis and minimum on and off times, and a priority relative to the Wattpilot. The surplus comes from the inverter; the outlet's own power lags by up to two minutes. | Step 1 |
| High | **Internet status** in the client: IPv4/IPv6 state, external addresses, uptime, throughput, and the outage history from step 2. | Step 2, TR-064 or USP |
| Medium | **More smart-home device types:** thermostats (target temperature, window open, boost), window and door contacts and alarm sensors, buttons (a press triggers a rule on the server, for example a Wattpilot boost), humidity, blinds. | REST (8.20); push for buttons and contacts would need USP, untested for them |
| Medium | **Templates and routines** of the box: apply a template, enable or disable a routine. | REST (8.20), comes with step 1 |
| Medium | **Energy history per outlet** in the history charts. | REST statistics. Caution: every request for them triggers a radio request on the box. |
| Medium | **Presence:** which phones are in the WLAN, as an input to rules (air conditioning off when nobody is home, and so on). | USP `Device.Hosts.Host.{i}.Active` with `Mesh` rights |
| Low | **Guest WLAN** on/off with a QR code. **WAN reconnect** button for `Operator`. | USP with `WiFi`/`Internet` rights, TR-064 |
| Low | **Box housekeeping:** FRITZ!OS version, update available, event log (like the Solar.web messages). | TR-064 `DeviceInfo` |
| Optional | **Telephony:** a notification for incoming calls (call monitor, TCP 1012), answering machine messages. | Call monitor, TR-064 `X_AVM-DE_TAM` |
| Follow-up | **The Home Automation MCP connector:** tools for whatever of the above gets built. Its code is not in this repository. | - |

## Risks

- **The REST API is version 0.9.5.** AVM reserves the right to change it. Mitigations: AHA stays as the fallback,
  and `[SystemFact]` tests show a change after a FRITZ!OS update.
- **The USP setup that stays (D9):**
  - Port 8883 on amazon2 is open to the internet, with TLS, a password per client and an ACL.
  - Anyone with the password of `usp-probe` can control the box within the controller's rights (Smarthome,
    Internet). The password stays on amazon2.
  - It is removed once step 2 no longer needs it.

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
