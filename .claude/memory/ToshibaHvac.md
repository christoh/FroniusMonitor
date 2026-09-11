---
paths:
  - Fronius/Models/ToshibaAc/**
  - Fronius/Models/JsonConverters/ToshibaDateTimeConverter.cs
  - Fronius/Models/JsonConverters/ToshibaHexConverter.cs
  - Fronius/Models/JsonConverters/ToshibaStateDataConverter.cs
  - Fronius/Models/Settings/AzureConnection.cs
  - Fronius/Models/Settings/ToshibaHvacDataCollectorParameters.cs
  - Fronius/Models/Settings/SettingsBase.cs
  - Fronius/Contracts/IToshibaHvacService.cs
  - Fronius/Contracts/IToshibaHvacSessionStore.cs
  - Fronius/Exceptions/ToshibaHvacUnauthorizedException.cs
  - Fronius/Services/ToshibaHvacService.cs
  - Fronius/Services/DataCollectors/ToshibaHvacDataCollector.cs
  - HomeAutomationServer/Controllers/ToshibaHvacController.cs
  - HomeAutomationServer/Hubs/HomeAutomationHub.cs
  - HomeAutomationServer/Models/Settings/Settings.cs
  - HomeAutomationServer/Models/Settings/ToshibaHvacSettings.cs
  - HomeAutomationServer/Services/ToshibaHvacSessionStore.cs
  - HomeAutomationServer/Program.cs
  - HomeAutomationServer/Settings.xml.example
  - HomeAutomationServerTests/UnitTests/Fakes/FakeToshibaHvacService.cs
  - HomeAutomationServerTests/UnitTests/Hosted/HubToshibaHvacTests.cs
  - HomeAutomationServerTests/UnitTests/ToshibaHvacJsonTests.cs
  - HomeAutomationServerTests/UnitTests/ToshibaHvacDataCollectorTests.cs
  - FroniusMonitor/Controls/ToshibaHvacControl.xaml
  - FroniusMonitor/Controls/ToshibaHvacControl.xaml.cs
  - FroniusMonitor/Controls/HvacButton.cs
  - FroniusMonitor/Controls/HvacFanSpeedButton.xaml.cs
  - FroniusMonitor/Controls/HvacMeritFeatureAButton.xaml.cs
  - FroniusMonitor/Controls/HvacWifiLedButton.xaml.cs
  - FroniusMonitor/Controls/ToshibaHvacSwingModeButton.xaml.cs
---

# Toshiba HVAC (Toshiba Home AC Control)

A Toshiba air conditioner with the Wi-Fi module is not reached locally at all. Everything goes through Toshiba's
cloud: an HTTPS API at `https://mobileapi.toshibahomeaccontrols.com` for login, registration and the device list,
and an **Azure IoT Hub** for live state and commands, which this app talks to with the `Microsoft.Azure.Devices.Client`
SDK as if it were one of Toshiba's phone apps. Nothing about it is Fronius, go-e or Fritz!Box; none of the other
device documents apply. The service class is `ToshibaHvacService` in `Fronius`, shared by the WPF app and the
server, and this document is the one place its protocol is written down.

## The account, the login and the token - the service does not like repeated logins

- **`POST /api/Consumer/Login`** with `Username` / `Password` answers a `ToshibaHvacSession`: `consumerId`,
  `access_token` (a bearer token), `token_type`, `consumerMasterId`, `countryId`. The token **lasts months**.
- **Toshiba's service is sensitive to logging in with user name and password again and again.** So the session is
  persisted and reused for as long as the service accepts it, through **`IToshibaHvacSessionStore`**: `Session`
  to read the stored one, `SaveSessionAsync` to persist a new one. The WPF app's `SettingsBase` implements it
  itself (`ToshibaHvacSession` / `ToshibaHvacSessionTime`, saved with the rest of `Settings.xml`); the server's is
  `ToshibaHvacSessionStore` over `Settings.ToshibaHvac.Session`, written to the server's `Settings.xml` the moment
  a login succeeded. **The token is stored in clear text for now**; encrypting it is a stated later step.
- **A new login is made only when the stored token is rejected**, which `ToshibaHvacService.Deserialize` defines
  as **HTTP 401 or 403** from the API (`ToshibaHvacUnauthorizedException`). `RegisterMobileDevice` tries the stored
  session, logs in once on that exception, and tries again; any other failure - network, 5xx, `IsSuccess: false`
  with a message - is thrown as it is and is *not* a reason to log in. (Before this, any failure at all triggered
  a login, which is exactly what the service resents.) **Assumption to verify against the live service:** that a
  stale token really comes back as 401/403 and not as a 200 with `IsSuccess: false`. If it turns out to be the
  latter, extend the check in `Deserialize`, not the retry logic.
- Every API answer is wrapped: `{ "IsSuccess": bool, "ResObj": <payload>, "Message": "..." }`
  (`ToshibaHvacResponse<T>`); `IsSuccess: false` becomes an `InvalidDataException` with the message.

## The mobile device id (`AzureDeviceId`)

The app registers itself with **`POST /api/Consumer/RegisterMobileDevice`** (`DeviceID`, `DeviceType: "1"`,
`Username`) and gets back `ToshibaHvacAzureCredentials`: `HostName`, `DeviceId`, `PrimaryKey`, `SecondaryKey`,
`SasToken`. The `DeviceID` it registers is **`<user name in lower case>_<six digit number>`**. That number is the
**AzureDeviceId**: drawn once at random (`ToshibaHvacAzureDeviceId.CreateRandom`, `0`-`999999`, written as
`D6`), stored in the settings and **never changed afterwards**, because the Toshiba service knows this
installation by it. A missing or unparsable value gets a fresh random one, with a warning
(`ToshibaHvacAzureDeviceId.Parse`), and the server's save at start-up writes it into `Settings.xml`.

- WPF: `SettingsBase.AzureDeviceId` / `AzureDeviceIdString` (`<AzureDeviceId>` element).
- Server: `ToshibaHvacSettings.AzureDeviceId` / `AzureDeviceIdString` (`AzureDeviceId` attribute of the
  `<ToshibaHvac>` element). Both use the same helper so the two never drift.

Registration must happen on every start: the IoT Hub connection string is built from the credentials it returns
(`HostName=...;DeviceId=...;SharedAccessKey=<PrimaryKey>`), with the transport from `AzureConnection.TransportType`
(`Protocol` Amqp/Mqtt/Http1 × `TunnelMode` Auto/Websocket/NoTunnel; the settings default is AMQP, auto).

## The device list (`GetConsumerACMapping`)

**`GET /api/AC/GetConsumerACMapping?consumerId=<session.ConsumerId>`** answers a list of `ToshibaHvacMapping`
(a *group*: `GroupId`, `GroupName`, `ConsumerId`, `TimeZone`, `ACList`), each holding `ToshibaHvacMappingDevice`s:
`Name`, `Id` (→ `AcId`), `DeviceUniqueId` (the air conditioner's identity, a GUID), `ACModelId`, `Description`,
`CreatedDate`, plus the base `ACStateData` (the **full** current state), `FirmwareVersion`, `MeritFeature` (a bit
mask of what the model can do, read by the WPF control to offer swing and merit modes) and `ModeValues`.

- `ToshibaHvacService.RefreshDevices` reads it again and **merges in place**: an existing group or device (by
  `GroupId` / `DeviceUniqueId`) takes the new values (`ToshibaHvacMappingDevice.CopyFrom`, the state replaced as a
  whole), new ones are added, vanished ones removed. The instance the control service and a client hold stays the
  instance.
- The **server reads it every 30 minutes** (`ToshibaHvacSettings.MappingRefreshMinutes` →
  `ToshibaHvacDataCollectorParameters.MappingRefreshRate`) because **message queuing loses the odd message**; the
  HTTPS side always has the whole state. Live changes in between come over the IoT Hub.
- `ToshibaHvacStatusDevice` models `GET /api/AC/GetCurrentACState?ACId=...`, which nothing calls today.

## Live data over the IoT Hub: the `smmobile` direct method

The air conditioners talk *to* the app: the SDK's `DeviceClient` registers a **direct method handler named
`smmobile`**, and every message is a `ToshibaHvacAzureSmMobileCommand`: `sourceId` (→ `DeviceUniqueId`, who sent
it), `messageId`, `targetId` (a list), `cmd`, `payload`, `timeStamp`, `timeZone`. `cmd` is one of

- **`CMD_FCU_FROM_AC`** - `payload.data` is the state as a hex string; only the bytes that are not `0xff` are
  taken over (`ToshibaHvacStateData.UpdateStateData`). It comes when the state changed, and **as the echo of a
  command** (below).
- **`CMD_HEARTBEAT`** - `payload` is a `ToshibaHvacHeartbeat` (`iTemp`, `oTemp`, `fcuTcTemp`, `fcuTcjTemp`,
  `fcuFanRpm`, `cduTdTemp`; `0xff` = not available). Only indoor and outdoor temperature are kept
  (`UpdateHeartBeatData`, state bytes 8 and 9).
- **`CMD_SET_SCHEDULE_FROM_AC`** - ignored.

A message from a device that is not in `AllDevices` is answered with status 1 and logged. After a message has been
applied the service raises **`DeviceUpdated(device, command)`** (for the two state carrying commands) and then
`LiveDataReceived(command)` (for every message; the WPF control uses it to match the echo).

The SDK reports its own connection through `SetConnectionStatusChangesHandler` → `IsConnected`.
`Disconnected_Retrying` is the SDK still working (retry policy: exponential back-off, five attempts);
`Disconnected` / `Disabled` other than `Client_Close` means it has **given up and will not come back** - the
service raises **`ConnectionLost`**, and the server's collector starts it again. `IsRunning` only says the
service holds a session and a client.

## Commands: `CMD_FCU_TO_AC`, and the echo that confirms them

`SendDeviceCommand(state, targets)` sends a `ToshibaHvacAzureSmMobileCommand` **as an event** (`SendEventAsync`):
`cmd: "CMD_FCU_TO_AC"`, `sourceId` = the mobile device id (`<user>_<AzureDeviceId>`), `targetId` = the
`DeviceUniqueId`s (GUID, `D` format) of the air conditioners, `payload: { "data": "<state hex>" }`, and
**`messageId: "MB_<AzureDeviceId upper case>-<8 digit counter>"`**. A command is a *delta*: a fresh
`ToshibaHvacStateData` is all `0xff`, and only the bytes the caller set are meaningful - `new ToshibaHvacStateData
{ IsTurnedOn = true }` switches on and changes nothing else.

**The air conditioner echoes the command as a `CMD_FCU_FROM_AC` with the same `messageId`** (the WPF control has
always relied on that to re-enable its buttons; ten seconds was its patience).
`SendDeviceCommandAndWait(state, timeout, targets)` builds on it: the pending record is keyed by the message id
*before* the send, each echoing target is struck off, and the `ToshibaHvacCommandResult` names the targets that
stayed silent (`Unconfirmed`, by the id strings the caller passed). Silence is reported, not treated as failure -
with message queuing the command may well have been taken and only the echo lost; the periodic mapping read
settles it.

## The state bytes (`ToshibaHvacStateData`)

19 bytes; the JSON form on **every** channel - Toshiba's API, the IoT Hub, and now the server's REST API and hub
too - is the **hex string** (`[JsonConverter(typeof(ToshibaStateDataConverter))]` on the type, and `ToString()`
produces the same). Never let it serialize as an object: byte `0xff` reads as temperature `-1` through the
property, and `-1` is a legitimate temperature the setter encodes as `0x7e`, so a round trip through the properties
would turn "leave it alone" into "set minus one". `ToshibaHvacJsonTests` pins the string form.

| Byte | Property | Values |
| --- | --- | --- |
| 0 | `IsTurnedOn` | `0x30` on, `0x31` off (`ToshibaHvacPowerState`) |
| 1 | `Mode` | `ToshibaHvacOperatingMode`: Auto `0x41`, Cooling `0x42`, Heating `0x43`, Drying `0x44`, FanOnly `0x45` |
| 2 | `TargetTemperatureCelsius` | signed byte; `0x7f` = none, `0x7e` = -1 |
| 3 | `FanSpeed` | `ToshibaHvacFanSpeed`: Quiet `0x31`, Manual1-5 `0x32`-`0x36`, Auto `0x41` |
| 4 | `SwingMode` | `ToshibaHvacSwingMode`: Off `0x31`, Vertical `0x41`, Horizontal `0x42`, Both `0x43`, Fixed1-5 `0x50`-`0x54` |
| 5 | `PowerLimit` | percent (the WPF control offers 50/75/100) |
| 6 | `MeritFeaturesA` | low nibble only; `ToshibaHvacMeritFeaturesA` (None, HighPower, Silent1, Eco, Heating8C, SleepCare, Floor, Comfort, Silent2) - the high nibble is preserved |
| 8 | `CurrentIndoorTemperatureCelsius` | read only, also fed by the heartbeat |
| 9 | `CurrentOutdoorTemperatureCelsius` | read only, also fed by the heartbeat |
| 14 | `IsSelfCleaning` | `0x18` yes, `0x10` no |
| 15 | `WifiLedStatus` | `ToshibaHvacWifiLedStatus`: On 1, Off 2 |

`ToshibaHexConverter<T>` reads the hex strings Toshiba uses for numbers and enums in the API answers (a JSON number
is accepted too); it is registered in the service's private `jsonOptions`, not on the types, so the server's own
JSON writes enums as camel case names like every other device.

## The models are devices of the control service

`ToshibaHvacDeviceBase` is `ISwitchable` (but `IsSwitchingEnabled` is false and `TurnOnOff` throws - switching
goes through the hub command), **`IHaveUniqueId`** (`Manufacturer` "Toshiba", `Model` "HVAC", `SerialNumber` =
`DeviceUniqueId` as `N`, so the id a client knows a device by is **`Toshiba;HVAC;<32 hex digits>`**) and
`IHaveDisplayName` (the mapping device's `Name`). The identity properties carry `[JsonIgnore]`, so they are not
in the JSON a client receives; the client gets the id as the message key.

## The server

- **`Settings.ToshibaHvac`** (`ToshibaHvacSettings`, one element, not a list - one account covers every air
  conditioner). **The element is the connection**: `ToshibaHvacSettings` derives from `AzureConnection`, so
  `BaseUrl`, `UserName`, `Password` (encrypted), `PasswordChecksum`, `Protocol` and `TunnelMode` are its own
  attributes, and `WebConnection`'s password handling applies - write `ClearTextPassword="..."` once and the
  server's save turns it into the encrypted `Password` (the developer asked for exactly this; a nested
  `<Connection>` element was the first version). Its own attributes are `AzureDeviceId`, `MappingRefreshMinutes`
  (30) and `SessionTime`, plus the `<Session>` element. Absent element or empty user name = no Toshiba. The
  default `Settings.xml` written on first start carries an empty section to show the shape; `Settings.xml.example`
  shows it too, and `ToshibaHvacSettingsTests` pins the XML shape and the password round trip.
- **`ToshibaHvacDataCollector`** (`Fronius/Services/DataCollectors`) runs the **singleton** `IToshibaHvacService`
  of the server (registered as a singleton in `Program.cs`, unlike the transient Wattpilot and Gen24 services,
  because the hub sends through the very instance the collector runs). It publishes every
  `ToshibaHvacMappingDevice` to `IDataControlService` with `SupportsPushMessages: false`, so `SignalRDispatcher`
  broadcasts the **whole device** as the SignalR method `ToshibaHvacMappingDevice` on every change - a few hundred
  bytes, a few times a minute per device; no separate delta message as for the Wattpilot. It republishes on
  `DeviceUpdated`, republishes all (and withdraws vanished ones) on every mapping read, and one one-shot timer is
  both the mapping refresh (30 min) and the retry (1 min) for a service that did not start or whose IoT Hub client
  gave up (`ConnectionLost` pulls it to now). `StopAsync` withdraws the devices.
- **`ToshibaHvacController`**: `GET api/ToshibaHvac` and `GET api/ToshibaHvac/{id}` (role `User`) from the
  control service - how a client loads the devices once.
- **`HomeAutomationHub.SendToshibaHvacCommand(string[] ids, ToshibaHvacStateData state)`**, role
  **`PowerUser`** - the role that switches Fritz!Box outlets in `DevicesController`, and deliberately *not*
  `Operator`: `Roles` are flags and an operator without the PowerUser bit is refused. It maps the client's ids to
  `DeviceUniqueId`s (unknown id → `HubException` with `Resources.DeviceNotFound`; service not running →
  `HubException` with `Resources.NoToshibaHvacConnection`), calls `SendDeviceCommandAndWait` with
  `ToshibaHvacEchoTimeout` (10 s) and answers the `ToshibaHvacCommandResult` with `Unconfirmed` translated back
  to the client's ids. **A client shows an error for every unconfirmed target.** See [[SignalR.MessageDirection]]
  for the attribute shape.
- Tests: `UnitTests/Hosted/HubToshibaHvacTests` (real hub, real connection: power user allowed, operator and
  watcher refused, unknown id by name, silent targets, service down), `UnitTests/ToshibaHvacDataCollectorTests`,
  `UnitTests/ToshibaHvacJsonTests`; `FakeToshibaHvacService` in `Fakes` is the stand-in.

## The WPF app

`FroniusMonitor` registers `IToshibaHvacService` as a singleton with its UI `SynchronizationContext` (the service
takes an optional one; without it - the server - `AllDevices` gets a plain context that runs inline) and its
`Settings` as the `IToshibaHvacSessionStore`. `DataCollectionService.TryStartToshibaAc` starts it when
`HaveToshibaAc && ShowToshibaAc` and puts `AllDevices[*].Devices` into `SwitchableDevices`; `MainWindow` shows a
`ToshibaHvacControl` per device, whose buttons build a delta state, call `SendDeviceCommand` and disable
themselves until the echo with their message id arrives or ten seconds pass. That control is the model for the
Avalonia one still to be written; the merit and swing options it offers depend on `AcModelId` and `MeritFeature`
bits (`0x2000` silent, `0x0400` 8 °C heating, `0x8000` floor, `0x4000` horizontal swing, `0x2` fixed positions on
model 3).

## Known gaps, as of 2026-09-12

- No Avalonia client side yet: no `KeyedToshibaHvac`, no hub handler for `ToshibaHvacMappingDevice`, no control.
- The bearer token is in clear text in both `Settings.xml` files.
- 401/403 as "token rejected" is not yet confirmed against the live service (see above).
- `IsConnected` is the SDK's word; a hub that is connected but delivers nothing is only caught by the mapping
  read.
- `ToshibaHvacDeviceBase.TurnOnOff` throws, so `DevicesController`'s generic switch endpoint refuses these devices
  (`IsSwitchingEnabled` false); switching is the hub command.
