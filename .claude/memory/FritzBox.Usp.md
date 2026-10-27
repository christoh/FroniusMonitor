---
paths:
  - FritzBox/Tr064/**
  - FritzBox/Usp/**
  - FritzBox/FritzBox.csproj
  - Tools/FritzBoxUspProbe/**
  - HomeAutomationServerTests/UnitTests/FritzBoxTr064Tests.cs
  - HomeAutomationServerTests/UnitTests/FritzBoxUspCodecTests.cs
  - HomeAutomationServerTests/UnitTests/FritzBoxUspControllerTests.cs
---

# The FritzBox library: TR-064 and USP

`FritzBox/FritzBox.csproj` talks to a FRITZ!Box through TR-064 (SOAP) and as a USP controller (TR-369) over MQTT.
The USP and TR-064 parts are used only by `Tools/FritzBoxUspProbe` and the tests. The HA server and the WPF app
use the smart-home part of the library (`FritzBox.SmartHome.md`).

**USP is not the way for smart home** (`.claude/plans/FritzBoxUsp-plan.md`, D8):
- It has no lamps.
- It pushes power no faster than the box updates it, which is every two minutes.
- It allows at most 32 subscriptions.

Step 1 uses the REST client of this library instead (`FritzBox.SmartHome.md`). Step 2 does not need USP either: it
cannot switch IPv6 (D10).

**So the setup was removed on 2026-10-27 (D9):**
- the controller on the box in Neufahrn, which `unregister` deleted without asking for a confirmation at the box
- Mosquitto on amazon2
- the security group `mosquitto-mqtts`

The library code, the probe and the tests stay.

It is a project of its own, not part of `Fronius`, because its users will be the HA server and the WPF app and not
the Avalonia client (see `Fronius.SharedLibraryBoundary.md`).

## What is known and what is not, as of 2026-10-24

- **Known from AVM's documentation:**
  - A controller of our own is added with TR-064 `X_AVM-DE_USPController:AddUSPController`. The arguments are
    endpoint ID, MTP `mqtt` or `websocket`, host, port, TLS, two MQTT topics, client ID, broker user and password,
    and one access-right flag per area.
  - The MQTT arguments need FRITZ!OS 8.01 or later.
  - Error 866 means the box wants a second factor. The flow is `X_AVM-DE_Auth:SetConfig(start)`, then the user
    presses a button or dials the code, `GetState` until `authenticated`, then the same action again with the token
    in the SOAP header (`<avm:token s:mustUnderstand="1">`).
- **Known from `info` against the box in Neufahrn (192.168.44.11), 2026-10-24:**
  - FRITZ!Box 5690 Pro, FRITZ!OS 8.25 (`279.08.25`).
  - The SCPD of `AddUSPController` declares all 19 arguments, the MQTT topics and `NewMQTTClientID` included.
  - The confirmation for protected actions is **on**. Registering a controller needs somebody at the box once.
  - The box already has one controller: MyFRITZ, `pen:872:myfritz-usp`, over WebSocket to
    `connect.myfritz.net:443/fritzos/usp/v1/[[BOX_ID]]`, enabled, with the rights Smarthome, Mesh, Internet and
    System. Its smart-home right strongly suggests that smart home is part of the USP data model.
  - The HTTPS certificate is issued to `hgm-fs.dns.army`, so it fails validation only because the box is addressed
    by IP. Phase 1.5 can use that name instead of pinning the certificate.
  - **The box takes a controller only with TLS.** `AddUSPController` with `NewUseTLS = 0` fails with UPnP error 600
    "Argument Value Invalid", for MQTT and WebSocket alike. Found by changing one argument at a time: a valid call
    ends at 866 (confirmation required) and adds nothing. Everything else was accepted as it was: an IP address as
    host, an empty client ID, `proto::` and `fqdn:` endpoint IDs, topics with "/".
  - All the `NewMinChars*` limits of `GetInfo` are 0, so `UspControllerRegistry.Validate` finds nothing there today.
    It is kept because the box gives no other hint which argument it rejects.
- **`.claude/plans/FritzBoxUsp-plan.md` phase 1.0 has the gate criteria.** The results are in the next section.

## What the box offers over USP (probe run of 2026-10-24)

**Connection:**
- The box is the agent `os::00040E-<MAC>` (AVM's OUI). It speaks USP 1.3 and data model `Device:2` (TR-181), with
  84 objects and 463 parameters.
- AVM's `MQTTResponseTopic` is the topic the **box subscribes to** (`usp/agent/fritzbox-neufahrn`, QoS 0). It sends
  to the controller on `MQTTControllerTopic`.
- Its records carry the Content Type `application/vnd.bbf.usp.msg`.
- After connecting it sends an MQTT Connect Record and a Notify without `send_resp`. When the controller is switched
  off it sends a Disconnect Record first.
- Switching a controller off and on with `SetUSPControllerEnableByIndex` needs no confirmation at the box.

**Smart home** is TR-181's `Device.ProxiedDevice.{i}.`:
- **The device:** `Name`, `Type` (`PowerStrip`, `Light`, empty for templates), `Online`, `ProxyProtocol` (`DECT`),
  `Alias` (writable). `DeviceInfo.SerialNumber` is the AIN (`11630 0396659`), next to `ModelName`, `Manufacturer`
  and `SoftwareVersion`.
- **`IoTCapability.{i}.`:** `Name` (the AIN again), `Class`, and one of these:
  - `BinaryControl.Value`: writable, plus a `Toggle()` command
  - `LevelControl.Value`: writable
  - `LevelSensor`: `Type`, `Value`, `Unit`, `X_AVM-DE_IsFeedin`
  - `BinarySensor`: `Value`, `LastChange`
- **FRITZ!Smart Energy 200 outlets** have capability 1 `BinaryControl` (Switch), 4 `LevelSensor` Temperature (degC),
  5 Power (W) and 6 Energy (Wh). There is no voltage.
- **FRITZ!Smart Light 500 lamps have no capabilities at all.** Over USP they cannot be read or controlled: no on/off,
  no level, no color. They stay with AHA or the REST API.
- **Templates and scenes** ("Licht an", "Haus aus") appear as proxied devices without capabilities. Their serial
  number is `tmp<MAC>-<id>`.

**Setting:** `Set` on `BinaryControl.Value` switches the outlet.

**Subscriptions:**
- `ValueChange` works and is event driven: a switch arrives within about a second.
- Power arrives only every two minutes. That is how often the outlet reports to the box, and the FRITZ! app shows
  the same delay.
- FRITZ!OS 8.25 limits what a subscription may be:
  - one path per `ReferenceList`: two give 7008
  - no wildcards: 7008
  - no object paths for `ValueChange`: 7807
  - some parameters refuse `ValueChange`: 7808, for example `DeviceInfo.UpTime`
  - **at most 32 subscriptions**: the 33rd gives 7004
- `ObjectCreation` and `ObjectDeletion` on `Device.ProxiedDevice.` are accepted (`{i}.` is not). Whether they fire was
  not seen, because no device was added or removed.
- The box sets `Recipient` itself; it is read-only.

**Gate of phase 1.0:**
- G1 (the values are visible): partly. Outlets yes, lamps no.
- G2 (AIN): yes.
- G3 (push): yes.
- G4 (switching over USP): yes.

## Where the broker ran (removed on 2026-10-27)

The broker is gone; this is what it was, should USP come back. Its files are in commit `2e576ad`.

On `amazon2.hochstaetter.de` (AWS), decided on 2026-10-24: Mosquitto with TLS on port 8883 and the Let's Encrypt
certificate of `amazon2.hochstaetter.de`, which certbot in `nginx.kartellvz` already renews. The box connects over
the internet (IPv4 or IPv6), the probe on amazon2 locally on port 1883. A broker on the HA server would have needed
a certificate for a name the box resolves to a private address. The README of the probe listed every file on
amazon2 until then.

From the home networks amazon2 is also reachable through WireGuard as 172.31.15.1. The HA server can use that,
with `amazon2.hochstaetter.de` as the TLS target host. The box cannot: the certificate is for the public name, and
its DNS rebind protection blocks a public name that resolves to a private address.

The controller was registered with the names the HA server is meant to keep (`fqdn:hochstaetter.de:home-automation`,
`usp/controller/home-automation`), because every new entry on the box needs somebody to confirm it at the box.

## How the controller finds the box

`UspMqttController` needs the box's Endpoint ID and topic before it can send anything.

- **The normal way:** the box sends an MQTT Connect Record to each controller after it connects to the broker
  (USP 1.2 and later). The Record is not retained, so a controller that comes up later misses it.
- **What the probe does about that:** it switches its own controller off and on with
  `SetUSPControllerEnableByIndex`. The box reconnects and introduces itself again. The probe then keeps the result
  in `probe-output/agent.json`.
- **Fallback:** the controller also learns the box from the first Record that is addressed to it, taking the topic
  from the MQTT 5 Response Topic or from the 3.1.1 `/reply-to=` suffix. A Disconnect Record does not count: it comes
  first when the controller is switched off, and a request sent then is lost.
- **Not yet decided:** how the HA server will do this later.

## Rules the code follows, from TR-369

- MQTT 5.0, Content Type `usp.msg`, and our topic as the Response Topic of every PUBLISH (R-MQTT.23, R-MQTT.27).
- `usp-endpoint-id` as a CONNECT user property (R-MQTT.13).
- If the box says in its Connect Record that it uses MQTT 3.1.1, the reply-to topic is appended to the topic name
  instead (R-MQTT.24).
- A Notify with `send_resp` is answered with a NotifyResp that carries the Notify's own message ID.

## Things that will bite

- **Every run of the probe connects with an MQTT client ID of its own.** With one shared ID each run threw the
  previous one off the broker, and a running `subscribe` could no longer delete its subscriptions.
- **`Record` clashes with `Xunit.Record` in the tests.** `FritzBoxUspControllerTests` aliases it
  (`using Record = UspRecord.Record;`).
- **The in-process MQTTnet broker of the tests must not open an IPv6 listener.** Claude Code on the web has no
  IPv6, and the default endpoint fails there with "Address family not supported". The tests bind it to
  `IPAddress.Loopback` with `IPAddress.None` for IPv6.
- **`curl --digest` does not work against TR-064.** It sends the first POST without a body, and the box answers that
  with 500 "XML error" instead of 401, so curl never authenticates. AVM's content level authentication (SOAP header
  `InitChallenge`, then `ClientAuth` with `MD5(MD5(user:realm:password):nonce)`) works with plain curl and md5sum.
  `Tr064Client` is not affected: `SocketsHttpHandler` resends the body.
- **TR-064 answers 402 to an argument it does not know**, and older firmware knows fewer.
  `UspControllerRegistry.AddAsync` therefore reads the action's SCPD from the box and sends only what is declared
  there.
- **Protoc runs at build time** (`Grpc.Tools`). The Docker image builds on `$BUILDPLATFORM`, so protoc is always
  native there.
- **The `.proto` files** in `FritzBox/Usp/Proto` are the Broadband Forum's, unchanged (USP 1.5). The generated
  types are in the namespaces `Usp` and `UspRecord`.
