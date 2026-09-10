---
paths:
  - Fronius/Models/Charging/**
  - Fronius/Attributes/WattPilotAttribute.cs
  - Fronius/Attributes/WattPilotIndexedAttribute.cs
  - Fronius/Contracts/IWattPilotService.cs
  - Fronius/Extensions/WattPilotExtensions.cs
  - Fronius/Services/WattPilotService.cs
  - Fronius/Services/WattPilotElectrictyService.cs
  - Fronius/Services/DataCollectors/WattPilotDataCollector.cs
  - Fronius/Models/Settings/WattPilotParameters.cs
  - Fronius/Validators/WattPilotFallbackCurrentAttribute.cs
  - HomeAutomationServer/Controllers/WattPilotController.cs
  - HomeAutomationServer/Services/SignalRDispatcher.cs
  - HomeAutomationClient/HomeAutomationClient/Services/UpdateService.cs
  - HomeAutomationClient/HomeAutomationClient/Services/UriLauncher.cs
  - HomeAutomationClient/HomeAutomationClient/Contracts/IUpdateService.cs
  - HomeAutomationClient/HomeAutomationClient/Contracts/IUriLauncher.cs
  - HomeAutomationClient/HomeAutomationClient/ViewModels/Dialogs/WattPilotSettingsDialogViewModel.cs
  - HomeAutomationClient/HomeAutomationClient/ViewModels/Dialogs/WattPilotSettingsViewModel.cs
  - HomeAutomationServer/Hubs/HomeAutomationHub.cs
  - HomeAutomationServerTests/UnitTests/Hosted/HubWattPilotSettingsTests.cs
  - HomeAutomationClient/HomeAutomationClient/Controls/WattPilotControl.axaml
  - HomeAutomationClient/HomeAutomationClient/Controls/WattPilotControl.axaml.cs
  - FroniusMonitor/Controls/WattPilotControl.xaml
  - FroniusMonitor/Controls/WattPilotControl.xaml.cs
  - FroniusMonitor/ViewModels/WattPilotSettingsViewModel.cs
  - HomeAutomationServerTests/UnitTests/WattPilotJsonTests.cs
---

# The Wattpilot

A Fronius Wattpilot is a **go-e Charger** with a Fronius label. Nothing about it is a Fronius inverter: the
firmware, the protocol, the names, the cloud are go-e's, and none of the Gen24 documents (`Gen24Polling.md`,
`DeviceJson.md` apart from the generic JSON reading rules) apply to it. The one point of contact is the hello
message, see below. Everything else that is specific to the charger is in this document; the detail view has its
own contract in `WattPilotDetailsView.Lifecycle.md`.

## It pushes, it is not polled

An inverter is asked over HTTP every few seconds. A Wattpilot is connected to **once**, over a WebSocket at
`ws://<host>/ws`, and from then on it sends what changed, when it changed - a `deltaStatus` with only the keys
that moved. There is no request for "the current status" after the handshake; the model in memory *is* the
status, kept current by applying every delta to it. That shapes everything:

- **The model is mutated in place, for its whole life.** `WattPilotService.WattPilot` is one `WattPilot` instance
  that every delta is written into, and every binding in every app hangs off that instance. `UpdateFromJson` is
  not "parse a document into a new object", it is "apply these keys to this object". A property that a delta
  never mentions simply keeps its value.
- **Losing the socket loses the data.** When the reader dies, `WattPilot` becomes `null`, `IsUpdating` (and so
  `IsPresent`) becomes `false`, and `OnLostConnection` fires with the old instance and a clone of the connection
  so the owner can reconnect. The old instance is kept in `savedWattPilot`: if the charger greets the next
  connection with a `deltaStatus` instead of a `hello` (it does, when it still considers the session alive), that
  saved instance is reused and the delta applied to it, because there will be no `fullStatus` to rebuild from.
- **Every key is go-e's abbreviation**, two to five letters, stated once per property by `[WattPilot("amp")]`.
  `nrg` is voltages, currents, powers and power factors in one 16-slot array; `tma` two temperatures; `pha` six
  phase flags; `cards` and now `c0n`... the RFID cards. The full list of what the charger knows about itself is
  the model, `WattPilot.cs`, and the fastest way to see what a live charger actually sends is its cloud API - see
  the end of this document.

## The connection, step by step (`WattPilotService.StartAsync`)

**`StartAsync` may be called on a running service.** It ends the connection it has first - cancel, await the
reader - and that reader's tear-down does **not** raise `OnLostConnection`, because a connection replaced on
purpose has not been lost and an owner that restarts from the event would otherwise start a second time while
this start is under way (`raiseLostConnection` is the flag, restored once the old reader has finished). Two
concurrent starts are serialized on `startLock`; the second replaces whatever the first left, connected or
failed. This is what lets the server's watchdog and the WPF poll simply call `StartAsync` - it used to open a
second socket beside the first and leave the old reader racing the handshake on it. `tokenSource` is the
"something is running" flag inside the service (created first thing, nulled when the handshake fails and when
the reader ends); `Connection` is the same thing seen from outside.

The tear-down (`CloseSocketAsync`, shared by the failed handshake and the reader's `finally`) **bounds the close
handshake to two seconds.** `ClientWebSocket.CloseAsync` waits for the peer's close frame, and a charger that
has gone half open - the case the watchdog reconnects for - never sends one; unbounded, that wait would hang the
reader's `finally` and with it every `StopAsync` and `StartAsync` awaiting the reader, for good. Measured against
a fake charger that swallows the close frame: replacing such a connection takes well under a second, because
cancelling the pending receive already aborts the socket and the close then fails fast; the two seconds are the
ceiling for the case where it does not.

1. A `CancellationTokenSource` with a **10 second** timeout covers the whole handshake. Afterwards it is replaced
   by one without a timeout for the reader. `Token` throws `WebSocketException` when there is no source, which is
   how "not connected" is reported everywhere.
2. `ClientWebSocket` with `KeepAliveInterval` 2 minutes and deflate enabled. The auth message is the one thing
   sent with `DisableCompression`.
3. The first message must be `hello`, else the service waits 5 seconds and throws - deliberately slow, so a
   misconfigured address does not spin. **The hello is read through the Gen24 reader**: `WattPilot.Parse` calls
   `IGen24JsonService.ReadFroniusData<WattPilot>`, and the handful of `[FroniusProprietaryImport(..., Root)]`
   attributes on `WattPilot` - `serial`, `hostname`, `friendly_name`, `manufacturer`, `devicetype`, `version`,
   `protocol`, `secured` - exist only for this one message. Do not add more of them; everything after the hello
   comes through `WattPilotAttribute`.
4. If the next message is `authRequired`, `Authenticate` runs the go-e handshake: the password is hashed with
   **PBKDF2-SHA512, 100 000 rounds, 24 bytes, salted with the serial number**, base 64 - which is why the cached
   hash is thrown away on every `StartAsync` (a different charger is a different salt) and whenever the
   connection's encrypted password changes. Then `hash1 = SHA256(token1 + hashedPassword)`,
   `hash = SHA256(token3 + token2 + hash1)` with `token3` 32 random lower-case hex digits of our own, sent as
   `{ type: "auth", token3, hash }`. `authError` becomes `UnauthorizedAccessException` with the charger's message.
5. Then `fullStatus` messages until one arrives with `partial` not `true` - the charger splits the initial state
   into several parts. `deltaStatus` may be interleaved and is applied too.
6. The reader task starts. It applies every `deltaStatus`, and matches every `response` to a pending write (see
   below). **Any exception ends the reader and tears the connection down** - close, dispose, null everything,
   save the instance, fire `OnLostConnection` on a thread pool thread. Reconnecting is the owner's job.

Messages are reassembled across frames in `ReceiveTextMessage`; the 8 KB buffer is only a frame buffer, a
`fullStatus` is far larger.

The firmware-update notification (`NewFirmwareAvailable`, comparing `fwv` with `onv`) is **switched off** with an
`&& false` and a `TODO` to use `ocu` instead. It never fires.

## Writing a value (`SendValue`, `Send`, and the acknowledges)

A write is `{ type: "setValue", requestId, key, value }`. When the hello said `secured`, that JSON is wrapped in
`{ type: "securedMsg", data: <the JSON as a string>, requestId: "<id>sm", hmac }` with an **HMAC-SHA256 over the
inner JSON text, keyed with the PBKDF2 hash** from above. The value is rendered by
`WattPilotExtensions.ToWattPilotJson` - never by a serializer, see "The wire format" below.

**`[WattPilot("x")]` means read-only. Writable is `[WattPilot("x", false)]`.** The default of `isReadOnly` is
`true`, so a new property is read-only until said otherwise, and `Send` silently skips it. When a setting is
"not written" and nothing complains, this is the first thing to check.

`Send(local, old)` is the settings dialog's write: it walks every property of `WattPilot` that carries **exactly
one** `WattPilotAttribute`, skips the read-only ones (which is what keeps the array-slot properties like `nrg[0]`
out - they carry one attribute, and it is read-only) and the ones whose value equals `old`'s, and sends the rest
one `setValue` each. Nothing different at all throws
`ArgumentException(Resources.NoSettingsChanged)` - the WPF dialog shows that as a warning, not an error. Errors
while sending are collected per property and returned, not thrown.

Every `SendValue` leaves a `WattPilotAcknowledge` (request id, property, value, a `ManualResetEventSlim`) in
`outstandingAcknowledges`. The reader's `response` handling sets `IsConfirmed` and the event when `success` is
true, and applies the `status` the response carries - so a written value comes back through the normal reading
path and the model shows what the charger actually took. A failed write is left **unconfirmed and unsignalled**
on purpose, so that `WaitSendValues` (default 5 s, `WaitHandle.WaitAll`) times out and the dialog reads
`UnsuccessfulWrites` to say which ones. The protocol around a batch of writes is therefore always
`BeginSendValues()` (clears the list) → `Send`/`SendValue` → `WaitSendValues()`, and `WattPilotSettingsViewModel`
is the reference for the error handling around it. Disposal of the events is guarded by `IsDisposed` under the
list's lock, because the reader may be setting an event the waiter is about to dispose.

`RebootWattPilot` is a clone with `Reboot` (`rst`) set, sent through `Send`, followed by `StopAsync`.

`OpenChargingLog` / `OpenConfigPdf` open `dll` (the charger's download link, `export` swapped for `documentation`
plus the UI language for the PDF) with `Process.Start(UseShellExecute)`. That is a desktop thing and only the
WPF app calls it; the Avalonia client builds the same two links in `WattPilotSettingsViewModel` and opens them
through `IUriLauncher` - Avalonia's `ILauncher` behind the `TopLevel` - which is what works in the browser and on
the phones.

## Writing settings from the Avalonia client goes over the hub

Not over a controller, unlike every Gen24 setting, because of the shape of the write described above: a
conversation on the socket the server holds, each `setValue` answered later, the resulting status reaching every
client as a `WattPilotUpdate` delta anyway. What the push channel does not carry is the answer to the one who
asked - which writes failed, which went unconfirmed - and that is what the hub method returns.

- **`HomeAutomationHub.SetWattPilotSettings(id, wanted, loaded)`** finds the service through `IWattPilotServices`
  (`WattPilotDataCollector` implements it: the service whose `WattPilot` has that `IHaveUniqueId.Id`), runs
  `BeginSendValues` → `Send(wanted, loaded)` → `WaitSendValues`, and answers a `WattPilotWriteResult`: `Errors`
  (could not be sent) and `Unconfirmed` (the charger never acknowledged, from `UnsuccessfulWrites`). An unknown id
  is a `HubException` with `Resources.NoWattPilotConnection` - the one exception type whose message reaches the
  caller. `RebootWattPilot(id)` beside it.
- **The difference is taken against `loaded`, the state the client's dialog started from**, not against the live
  device: `WattPilot.ChangedSettings(other)` over `WattPilot.WritableSettings`, which `Send` itself now uses. A
  value the charger changed on its own while the dialog was open - a load balancing current the master adjusts,
  `dyn` - is not something the user asked to change and must not be written back to what it was. The WPF dialog
  has always compared against its own clone for the same reason; sending both copies is what lets the server do
  the comparing.
- **Role `Operator` on the method**, as on every Gen24 write endpoint; the hub gate only proves `User`. See
  [[SignalR.MessageDirection]] for the attribute and the test that pins it.
- **Client side:** `IUpdateService.SetWattPilotSettings` / `RebootWattPilot` are `HubConnection.InvokeAsync` on
  the connection `UpdateService` already holds - nothing new is connected, and nothing blocks: the browser head
  has no synchronization objects to wait on, only `await`.
- **The dialog** (`WattPilotSettingsDialogViewModel`, `WattPilotSettingsViewModel`, `NumberField`,
  `NumberSlider`) is described in [[SettingsDialogs.Lifecycle]]: one Apply for six tabs, a clone of the live
  charger the client already holds, every number a box-and-slider `NumberField` with its rule as an instance.

## The wire format is stated by `WattPilotAttribute`, and nowhere else

This broke twice during the Newtonsoft → System.Text.Json conversion, both times without an error message.

The charging models carry **two** sets of names on purpose:

| attribute | for | example on `WattPilotWifiInfo.IpV4AddressString` |
|---|---|---|
| `[WattPilot("ip")]` | the WebSocket protocol of the charger | `ip` |
| `[JsonPropertyName("ipV4Address")]` | the server-to-client channel | `ipV4Address` |
| `[JsonProperty("ip")]` (Newtonsoft) | **dead** - was the charger protocol | `ip` |

So a `JsonSerializer` call on one of these models produces the *client* names, or the C# names where there are
none. Both are wrong for the charger, and neither the charger nor the serializer complains:

- **Writing.** `WattPilotExtensions.ToWattPilotJson` writes a value through an explicit type switch, and
  `ToWattPilotObject` writes a nested one (`lot`, the load balancing currents) from the `WattPilotAttribute`s -
  never through `JsonSerializer`. An attribute with an `Index` names one slot of an array the charger *sends*
  (`nrg`, `tma`, `pha`, the WiFi `f` capabilities) and is skipped when writing. A type nobody has thought about
  throws `NotSupportedException` rather than being serialized wrongly.
- **Reading.** `SetWattPilotValue` handles a `JsonObject`, and a list of them, through the same attributes before
  it ever offers the value to `JsonSerializer.Deserialize`. Left to the serializer, a `{ "amp": 16, ... }` is
  taken happily and answers an object with **every property at its default** - the charger appears to report
  zeroes, and there is no exception anywhere.
- **A property can have several attributes.** With an `Index`, several properties share one array key (`nrg`
  0 to 15). Without one, one property answers to several keys - `WattPilotWifiInfo.IsB` is `b` in a scanned
  network and slot 2 of `f` in the current one; `WattPilotCard.Name` is `name` in the `cards` array and `n` in a
  `c0n` key. `ParseUpdateToken` looks a key up across all attributes of all properties.
- **A `byte[]` is base 64 to System.Text.Json**, in both directions. The charger writes the phase map (`map`) as
  an array of numbers, so `SetWattPilotValue` reads that shape itself and `ToWattPilotJson` writes it.
- The **text fallback** at the end of `SetWattPilotValue` is not optional: `Deserialize` refuses a JSON string
  where a number is wanted, and a number where a flag is wanted, and the charger is not consistent about which it
  writes. A flag goes through `AsBoolean` because `Convert.ToBoolean` refuses `"1"`. The generic rules for
  reading device JSON as text are in `DeviceJson.md`.
- **Enums carry go-e's numbers**, not ours: `ChargingLogic.None = 3`, `Eco = 4`, `NextTrip = 5`;
  `LoadBalancingPriority.Low = 60 / Medium = 50 / High = 40`; `AwattarCountry` is go-e's price zone table
  (`Austria = 0`, `GermanyLuxembourg = 1`, everything else 10000 and up). Never renumber one to look tidier.

`HomeAutomationServerTests/UnitTests/WattPilotJsonTests.cs` pins all of that, including that a written object
reads back through the reader unchanged. If you touch either side, that round trip is the test that matters.

### The cards come two ways, and the second one is a list spelled out key by key

Older firmware sent the RFID cards as one `cards` array of `{ name, energy, cardId }`. Newer firmware (seen
2026-09-10) sends every property of every card as a **key of its own**: `c0n`, `c0e`, `c0i`, `c0p` for card 0,
`c1n` ... for card 1, and so on - a card charging is then a stream of single `c1e` keys. The cloud status endpoint
sends both shapes at once, so neither may be dropped.

`[WattPilotIndexed("c")]` on `WattPilot.Cards` says the list is also sent that way. `ParseUpdateToken` falls back
to it for a key **no property has claimed**: prefix, then digits, then the rest is looked up as a
`WattPilotAttribute` name on the *entry* type - which is why `WattPilotCard` carries `n`/`e`/`i` beside
`name`/`energy`/`cardId`. Three things that are deliberate:

- The list **grows by replacement** (a new `List<>` copying the old entries, empty cards filling the gap up to the
  index) so a binding to `Cards` sees a new card; an entry that already exists is **changed in place**, because a
  `WattPilotCard` is a `BindableBase` and tells its own bindings. `Clone()` puts a `WattPilotCard[]` there, which
  cannot grow, so the copy is not optional.
- `c0p` (a `"{}"` string, purpose unknown) has no property and is ignored, the same as any unclaimed key. So are
  `cae`, `cco`, `c`, `c0` - prefix without digits, or digits without a name.
- `Cards` also notifies `CurrentUser`, which reads the authenticated card's name out of it.

## What the model derives, and the traps in it

- **`trx` / `AuthenticatedCardIndex`**: `null` is nobody authenticated, `0` is guest charging, `n` is card
  `n - 1`. `CurrentUser` indexes `Cards` with that offset. The settings dialog writes `0` for "authenticated" and
  `null` for "not".
- **`nrg` slots**: 0-2 voltages L1-L3, 3 neutral voltage (`VoltageL0`), 4-6 currents, 7-9 powers, 10 neutral
  power, 11 total power, 12-15 power factors in percent (L1, L2, L3, N). `PowerFactorL1` etc. divide by 100.
- **`ChargingPhases`** is *derived*: the number of phases with more than 10 W, and when none is charging, 1 for
  `PhaseSwitchMode.Phase1` and 3 otherwise. `pnp` (`NumberOfCarPhases`) is only maintained by the charger in
  Eco mode and is not used for this.
- **`MaximumChargingCurrentPossiblePerPhase`** is the smallest of `amp` (`MaximumChargingCurrent`), `ama`
  (`AbsoluteMaximumChargingCurrent`), `la1` (only when charging on one phase) and `cbl` (the cable). Times
  `ChargingPhases` is `MaximumChargingCurrentPossible`; the power variants multiply by the measured voltages.
  The Avalonia detail view invents 32 A / 96 A fallbacks when these are `null` - see its lifecycle document.
- **`IsUpdating` is the presence flag** (`IsPresent => IsUpdating`), set by the service, and its setter is also
  what pushes the charger's price forecast into `WattPilotElectricityService` (see below).
- **`utc` → `Latency`**: every timestamp the charger sends is compared with `DateTime.UtcNow`, so `Latency` is
  the WebSocket delay plus the clock difference between charger and this machine.
- **`awc` / `EnergyPriceCountry`** is not nullable and defaults to `Austria`. It is written to the price service
  whenever prices arrive.
- **Times as text**: `AllowChargingFromBatteryStartString` / `StopString` are `HH:mm` views of the seconds
  `pdls` / `pdlo`; `OhmPilotTemperatureLimitFahrenheit` is a view of `fot`. All three are `[JsonIgnore]` for the
  client channel so the number travels, not the text.
- **`AllowChargingPause` false forces `PhaseSwitchMode.Phases3`** - a rule of the charger the settings dialog
  applies on load (`Undo`) and again on `Apply`, so the two never disagree on the wire.
- **`MinimumChargingInterval`** (`mci`) is 0 for "off"; the dialog's `RequiresChargingInterval` switch turns it
  into at least 300 000 ms (5 minutes) when on and back to 0 when off. `NextTripTime` (`ftt`) is seconds from
  midnight, capped at 86 399; `NextTripEnergyToCharge` (`fte`) is Wh, the dialog edits kWh.
- **`LoadBalancingCurrents`** (`lot`) is a nested object written **as a whole**, all four values in one
  `setValue`, never one at a time - `Send` only knows the properties of `WattPilot`, and `ToWattPilotObject`
  renders the object. It has value equality (`Equals`/`==` overridden) so that `Send` can tell whether it changed
  - and when it did, the dialog stamps `TimeStamp` with `UtcNow` first, because the charger wants a fresh `ts`
  with every change. **That value equality bit back once, and the property on `WattPilot` is hand-written
  because of it:** an `[ObservableProperty]` setter drops a value that is `Equals` to the one it has, so
  `Clone()` handing it `LoadBalancingCurrents.Copy()` kept the *original* instance in the clone, the dialog then
  edited the original through the clone, `Send` found old and new identical and threw "no settings changed" -
  the currents could never be written (found 2026-09-11 against a fake charger). The setter compares by
  reference (`SetProperty(ref field, value, ReferenceEqualityComparer.Instance)`), so a copy is always taken;
  `WattPilotModelTests` pins it. Any other value-equal type that gets cloned into an `[ObservableProperty]`
  will do the same thing.
- **`Clone()`** is a `MemberwiseClone` plus deep copies of `Cards` (as an array) and `LoadBalancingCurrents`;
  `IsUpdating` is reset to `false` on the copy. **`CopyFrom`** assigns every writable public property by
  reflection - including `IsUpdating`, `Cards` by reference, and `LoadBalancingCurrents` by reference.
- `WifiPassword` (`wak`) is write-only on the wire; the dialog keeps it in a field of its own and only writes it
  when the user typed something.
- `WattPilotFallbackCurrentAttribute` is the one validation rule with a hole: `lof` is 6 to 32 A **or 0**,
  because 1 to 5 A would be accepted by the box and ignored by the charger.

## Where the service lives in each app

**WPF (`FroniusMonitor`)** registers `IWattPilotService` as a **singleton**. `DataCollectionService.TryStartWattPilot`
runs on every poll tick: `StartAsync` when there is a connection configured and the service has none,
`StopAsync` when the connection was taken away (`Settings.HaveWattPilot && ShowWattPilot`, the menu switch). So
a lost connection is reconnected by the next tick, and `HomeAutomationSystem.WattPilot` is the service's
instance. The main window menu has settings, charging log and reboot; `WattPilotSettingsView` /
`WattPilotSettingsViewModel` is the settings dialog described above. `ElectricityPriceService.WattPilot` in the
settings chooses `WattPilotElectricityService` as the app's price source.

**Server (`HomeAutomationServer`)** registers `IWattPilotService` as **transient** and `WattPilotDataCollector`
as the singleton that owns one service per configured `WebConnection` (`Settings.WattPilotConnections` →
`WattPilotParameters.Connections`). It reconnects from two places, and both are one `StartAsync`:
`OnLostConnection` when the reader ends for any reason, and a 15 second watchdog for a service that has been
silent for 15 seconds - a half-open socket delivers nothing and raises nothing - or that never came up. A start
attempt stamps `LastMessageReceived`, so the watchdog does not interrupt a handshake that is still within its own
ten second timeout. Every `OnUpdate` publishes **two** managed devices to `IDataControlService`:

- the `WattPilot` itself, with `SupportsPushMessages: true` - which is precisely what makes `SignalRDispatcher`
  **not** broadcast it. A full `WattPilot` per delta would be the whole model, several times a second.
- a `WattPilotUpdate` - the serial number and the **raw JSON of the delta** as a string, with `Manufacturer`
  "Fronius" and `Model` "WattPilotUpdateMessage" so its `IHaveUniqueId.Id` differs from the charger's. This one
  is broadcast, as the SignalR method `WattPilotUpdate`.

`WattPilotController` serves `GET api/WattPilot` and `GET api/WattPilot/{id}` (role `User`) from the control
service, which is how a client gets the full model once.

**Avalonia client (`HomeAutomationClient`)** - `UpdateService`: `GetWattPilots` on start puts a `KeyedWattPilot`
per charger into `AllPowerConsumers` (they are also `DetailDevices` and `DevicesWithSettings`). The
`WattPilotUpdate` handler queues the delta per id and applies it with **the same `UpdateFromJson` the server
uses** - the client runs the full wire-format reader on the raw go-e JSON, so every rule in this document about
keys, cards and fallbacks holds on the phone and in the browser too. A delta for a serial the client does not
know triggers a fresh `GetWattPilots`. The `WattPilot` handler (`CopyFrom` on the existing instance, or a new
`KeyedWattPilot`) is reached from exactly one place: `HomeAutomationHub.OnConnectedAsync` sends the caller every
device the control service holds, `SupportsPushMessages` or not - the `WattPilotUpdate` message included, whose
handler then queues a delta that has long been applied. On a **reconnect** that `CopyFrom` is what brings a
client's instance back in step after the deltas it missed; the dispatcher never sends a `WattPilot` after that.

Both apps have a `WattPilotControl` for the dashboard with the same `WattPilotDisplayMode` enum and the same
click-to-cycle logic (power → power factor, voltage, current, and a "more" cycle of frequency, neutral wire,
temperatures, RFID card energies, WiFi). The two are copy and paste of each other, one in `Controls/` of each
app; the Avalonia one takes the `WattPilot` as a styled property where the WPF one takes the service.

## Electricity prices from the charger

`awpl` is the charger's own price forecast: `marketprice` (an array of ct/kWh), `start` (epoch seconds) and
`interval` (seconds). `WattPilotElectricityPrice` holds it, and `WattPilot.ElectricityPrices`' setter - and
`IsUpdating`'s, for the moment the connection comes up - hand it to `WattPilotElectricityService` when that is
the registered `IElectricityPriceService`, expanded into one `ElectricityPrice` per interval. The service does
not support history or choosing a zone (`CanSetPriceRegion => false`); the zone is whatever `awc` says.

## The cloud API, for looking at a live charger

`https://<serial>.api.v3.go-e.io/api/status?token=<cak>` answers the **whole status as one JSON document with
exactly the keys of the WebSocket** - `cae` (`CloudAccessEnabled`) must be on, `cak` (`CloudAccessKey`) is the
token, both are settings in the WPF dialog, which also shows this link (`ApiUri`). It is the quickest way to see
what a firmware actually sends, and how the card change above was found. Treat the token as a password: it is in
the URL.

## Known gaps and bugs, as of 2026-09-10

- The firmware-update check is switched off (`&& false`), see above.
- The scanned WiFi tab of the Avalonia dialog is a plain grid: no signal strength icon, no row tooltip.
- A `HubException` from the server carries a message in the server's language, like a `ProblemDetails` does.
- `WattPilotDisplayMode` and the cycling logic exist twice, once per app.
