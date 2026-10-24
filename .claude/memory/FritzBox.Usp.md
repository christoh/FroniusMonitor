---
paths:
  - FritzBox/**
  - Tools/FritzBoxUspProbe/**
  - HomeAutomationServerTests/UnitTests/FritzBoxTr064Tests.cs
  - HomeAutomationServerTests/UnitTests/FritzBoxUspCodecTests.cs
  - HomeAutomationServerTests/UnitTests/FritzBoxUspControllerTests.cs
---

# The FritzBox library: TR-064 and USP

`FritzBox/FritzBox.csproj` talks to a FRITZ!Box through TR-064 (SOAP) and as a USP controller (TR-369) over MQTT.
It is the start of phase 1.1 of `.claude/plans/FritzBoxUsp-plan.md`. Only `Tools/FritzBoxUspProbe` and the tests
use it so far. The HA server and the WPF app still go through AHA in `Fronius/Services/FritzBoxService.cs`.

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
  - The boxes here have the confirmation switched off.
- **Not known before the probe has run:**
  - Which of `MQTTControllerTopic` and `MQTTResponseTopic` the box subscribes to. AVM's wording is ambiguous, so the
    probe listens on `usp/#`.
  - Whether the box sends an MQTT Connect Record.
  - Which smart-home objects the box offers over USP, and whether it allows `ValueChange` subscriptions for them.
- `.claude/plans/FritzBoxUsp-plan.md` phase 1.0 has the gate criteria. Replace this section with the findings once
  `explore` has run.

## How the controller finds the box

`UspMqttController` needs the box's Endpoint ID and topic before it can send anything.

- **The normal way:** the box sends an MQTT Connect Record to each controller after it connects to the broker
  (USP 1.2 and later). The Record is not retained, so a controller that comes up later misses it.
- **What the probe does about that:** it switches its own controller off and on with
  `SetUSPControllerEnableByIndex`. The box reconnects and introduces itself again. The probe then keeps the result
  in `probe-output/agent.json`.
- **Fallback:** the controller also learns the box from the first Record that is addressed to it, taking the topic
  from the MQTT 5 Response Topic or from the 3.1.1 `/reply-to=` suffix.
- **Not yet decided:** how the HA server will do this later.

## Rules the code follows, from TR-369

- MQTT 5.0, Content Type `usp.msg`, and our topic as the Response Topic of every PUBLISH (R-MQTT.23, R-MQTT.27).
- `usp-endpoint-id` as a CONNECT user property (R-MQTT.13).
- If the box says in its Connect Record that it uses MQTT 3.1.1, the reply-to topic is appended to the topic name
  instead (R-MQTT.24).
- A Notify with `send_resp` is answered with a NotifyResp that carries the Notify's own message ID.

## Things that will bite

- **`Record` clashes with `Xunit.Record` in the tests.** `FritzBoxUspControllerTests` aliases it
  (`using Record = UspRecord.Record;`).
- **The in-process MQTTnet broker of the tests must not open an IPv6 listener.** Claude Code on the web has no
  IPv6, and the default endpoint fails there with "Address family not supported". The tests bind it to
  `IPAddress.Loopback` with `IPAddress.None` for IPv6.
- **TR-064 answers 402 to an argument it does not know**, and older firmware knows fewer.
  `UspControllerRegistry.AddAsync` therefore reads the action's SCPD from the box and sends only what is declared
  there.
- **Protoc runs at build time** (`Grpc.Tools`). The Docker image builds on `$BUILDPLATFORM`, so protoc is always
  native there.
- **The `.proto` files** in `FritzBox/Usp/Proto` are the Broadband Forum's, unchanged (USP 1.5). The generated
  types are in the namespaces `Usp` and `UspRecord`.
