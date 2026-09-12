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
  - HomeAutomationServerTests/UnitTests/ToshibaHvacSettingsTests.cs
  - HomeAutomationServerTests/UnitTests/ToshibaHvacViewModelTests.cs
  - HomeAutomationClient/HomeAutomationClient/Contracts/IToshibaHvacCommander.cs
  - HomeAutomationClient/HomeAutomationClient/Contracts/IUpdateService.cs
  - HomeAutomationClient/HomeAutomationClient/Services/UpdateService.cs
  - HomeAutomationClient/HomeAutomationClient/Models/KeyedDevices.cs
  - HomeAutomationClient/HomeAutomationClient/Models/HvacOptions.cs
  - HomeAutomationClient/HomeAutomationClient/ViewModels/ToshibaHvacViewModel.cs
  - HomeAutomationClient/HomeAutomationClient/Controls/ToshibaHvacControl.axaml
  - HomeAutomationClient/HomeAutomationClient/Controls/ToshibaHvacControl.axaml.cs
  - HomeAutomationClient/HomeAutomationClient/Controls/Hvac*Icon.axaml
  - HomeAutomationClient/HomeAutomationClient/Controls/Hvac*Icon.axaml.cs
  - HomeAutomationClient/HomeAutomationClient/Assets/Images/QuietIcon.axaml
  - HomeAutomationClient/HomeAutomationClient/Assets/Images/SpeakerIcon.axaml
  - HomeAutomationClient/HomeAutomationClient/Views/DashboardView.axaml
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

## The Avalonia client

The air conditioners sit on the dashboard next to the Wattpilot and the Fritz!Box devices, in `AllPowerConsumers`
as **`KeyedToshibaHvac`** (`KeyedDevice<ToshibaHvacMappingDevice>`): `UpdateService.StartAsync` loads them once
with `IWebClientService.GetToshibaHvacDevices` (`GET api/ToshibaHvac`), and the hub handler for the SignalR method
**`ToshibaHvacMappingDevice`** copies each push into the existing instance (`CopyFrom`) or adds a new one. Because
the instance and its `State` object stay the same, everything bound to or subscribed to them keeps working.

**The control follows the rule that the view model owns the interaction** ([[ViewModelsForInteractionLogic]]),
which the WPF control did not: everything its code behind did - cycling the fan speed, clamping the temperature,
building the per-model menus, disabling itself until the echo - is in **`ToshibaHvacViewModel`**, one per device.

- `DashboardView` has a `DataTemplate` for `KeyedToshibaHvac` that creates a **`ToshibaHvacControl`** (a
  `DeviceControlBase`) with `Device` and `DeviceKey` from the item. The control resolves a *transient*
  `ToshibaHvacViewModel` from `IoC` and makes it the data context of its inner `Root` element - **not of the
  control itself**, because the control's own data context is the dashboard item that the template's
  `Device="{Binding Device}"` is resolved against. It hands `Device` and `DeviceKey` on to the view model and
  keeps only the background for itself (`ChangeOuter`: cleaning → `CleaningBackground`, on → `OuterRunning`,
  off → `OuterOther`, the Fritz!Box colours - the WPF `PowerStatus2Brush` also had "not connected" → OrangeRed,
  which the client cannot know).
- The view model takes **`IToshibaHvacCommander`**, a one-method contract that `IUpdateService` extends and
  `UpdateService` fulfils with `Hub.InvokeAsync("SendToshibaHvacCommand", ids, state)`. It exists so that
  `ToshibaHvacViewModelTests` can fake it: `IUpdateService` has an `internal` event and cannot be implemented from
  the test project. `App.axaml.cs` registers the update service instance under it as well.
- Every command builds a **delta** `ToshibaHvacStateData` (all 0xff but the bytes it sets), sends it with the
  device's key, sets `IsSending` (which disables every command through `CanSend`) until the server answers -
  the server waits up to ten seconds for the echo - and shows `Resources.ToshibaHvacCommandNotConfirmed` as a
  warning box for every target in `Unconfirmed`. Exceptions (`HubException` for no connection or unknown device,
  no hub) go through `TaskExceptionHandler`. `ShowUnconfirmed` is virtual so the test overrides it.
- **Left click cycles, right click (long press on touch) chooses**: the icon buttons carry a `MenuFlyout` whose
  `ItemsSource` is a list of **`HvacOption`s** (`FanSpeedOption`, `PowerLimitOption`, `MeritFeatureOption`,
  `SwingModeOption`, `TemperatureOption` - one concrete class per kind because a `DataTemplate` cannot name a
  generic type). `HvacOption.SelectCommand` applies the value, `IsSelected` is the check mark and is rewritten from
  the state on every change. The `MenuItem` theme `HvacOptionItem` in the control's resources binds those two with
  `ReflectionBinding`, since a `ControlTheme` has no data type. The merit and swing lists depend on
  `AcModelId` and the `MeritFeature` bits exactly as in the WPF control (`AvailableMeritFeatures`,
  `AvailableSwingModes`, both static and tested).
- The visuals are one `Viewbox` control per state, each a port of a WPF button: `HvacModeIcon` (ring, filled with
  `HvacModeSelected` for the current mode), `HvacFanSpeedIcon` (fan, five bars or AUTO or the quiet icon),
  `HvacPowerLimitIcon`, `HvacMeritFeatureIcon` (8°C, ECO, Normal, Hi Power, Floor, or quiet icon + `SpeakerIcon`
  for the two silent modes - Floor is new, WPF left the button blank), `HvacSwingModeIcon`, `HvacWifiLedIcon`;
  `QuietIcon` and `SpeakerIcon` in `Assets/Images`. They switch their parts in code behind from the state
  property, like `WifiControl`; the brushes come in as styled properties and the control's styles set them from
  the theme: `ForegroundBrush` for the glyphs, and six new theme brushes `HvacModeSelected` (Aquamarine /
  DarkCyan), `HvacActive` (LimeGreen / #006400 - the developer's choice for every green in the dark variant),
  `HvacInactive` (DarkGray / #A0A0A0 - lighter than the dark "off" card it is painted on), `HvacOffMark` (Red /
  #FF5C5C), `HvacHoverFill` and `HvacHoverStroke` (the WPF hover frame).
  `Button.HvacHover` and `Button.HvacMode` in `Styles/Buttons.axaml` are the WPF HoverButton and the mode
  button: transparent, hover frame and pressed nudge addressed at the template's `PART_ContentPresenter`.
- **Porting WPF paths with a `RenderTransform`**: an Avalonia `Path` keeps the geometry's absolute coordinates
  and sizes itself to `Bounds.Right × Bounds.Bottom`, as WPF does, so a pure `TranslateTransform` ports as it is.
  A group with a `ScaleTransform` does not: WPF scales about the top left, Avalonia about the centre
  (`RenderTransformOrigin` defaults to 50 %, 50 %), so such a path needs `RenderTransformOrigin="0,0"` - the fan of
  `HvacFanSpeedIcon` was a quarter of its size and in the wrong place without it. Font metrics differ too, and
  **a headless probe only shows that when it registers Inter (`WithInterFont()`), as the heads do** - without it
  the probe renders with Segoe UI, the WPF font, and looks right while the app does not. Measured with Inter: the
  "A" of the auto mode icon needs no top margin (WPF had 8) to sit centred on its ring, and the set temperature
  (`TextAlignment="Center"` on a stretched `TextBlock`) is centred on the arrows only when neither it nor the
  arrows carry a margin (WPF had 2,-3 and 2 - the developer took the last one off himself) and the arrows stretch
  to the text's width, because Inter sets "24°C" wider than the 66 unit triangle. The mode buttons keep WPF's
  hover: `Button.HvacMode:pointerover` sets `HvacModeIcon.IsHovered` through a style, and an unselected ring is
  then filled with `HvacHoverFill`.
  Glyphs drawn as filled outlines (the house, the tree) need a hairline `Stroke` in the same brush to survive
  14 px, and each sits in a `Canvas` cut to its own bounds (the house path begins 28 units below its origin, the
  tree at 4,2) - in a box the size of `Bounds.Right × Bounds.Bottom` the glyph hangs at the bottom and the text
  beside it looks too high, which is what WPF's `-24` and `-4,-2` offsets corrected. Inter's digits themselves
  are exactly centred in their line box (ascent 0.969, descent 0.241, cap height 0.727 em), so a text with no
  descenders needs no vertical correction. All of this was found by rendering, not by reading - the last two
  points with a *headed* probe on the developer's own screen (see the auto memory on render verification).
- The control is laid out at the WPF width of 150 and scaled by its `Viewbox` to the dashboard's 210, so the
  font sizes are the WPF ones. Verified by a Skia headless render in both variants (see
  [[avalonia-render-verification]] in the auto memory): three devices, on / off / silent-fixed-louver, next to
  the WPF original.
- No details page and no settings dialog for an air conditioner: `DetailDevices` and `DevicesWithSettings` do
  not include it, and `MainViewModel.Settings` is never called with one.

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

- The client control cannot show "not connected" (the WPF control painted the card OrangeRed when the IoT Hub
  connection was down): the server does not publish the service's `IsConnected`. A stale device keeps its last
  state until the next mapping read or push.
- The bearer token is in clear text in both `Settings.xml` files.
- 401/403 as "token rejected" is not yet confirmed against the live service (see above).
- `IsConnected` is the SDK's word; a hub that is connected but delivers nothing is only caught by the mapping
  read.
- `ToshibaHvacDeviceBase.TurnOnOff` throws, so `DevicesController`'s generic switch endpoint refuses these devices
  (`IsSwitchingEnabled` false); switching is the hub command.
- **Logging is temporarily loud**, commit `ca0d28c` ("Temporarily set logging to Information here ..."): seven
  `ToshibaHvacService` messages that belong at Debug (registration, device list, every sent command, every
  received method, every raw HTTPS answer) are at Information with their `IsEnabled` guards raised to match, the
  `#if DEBUG` around the extra IoT Hub handlers is commented out, and the raw JSON branch of `Deserialize` is
  `#if !DEBUG`. It is one commit so that `git revert ca0d28c` takes all of it back once the live behaviour is
  understood; do not "fix" any of it piecemeal, and keep guard and call level in step if you touch one.
