---
paths:
  - FritzBox/Http/**
  - FritzBox/SmartHome/**
  - FritzBox/FritzBox.csproj
  - Fronius/Models/FritzBox*.cs
  - Fronius/Contracts/IFritzBoxService.cs
  - Fronius/Services/FritzBoxService.cs
  - HomeAutomationServerTests/UnitTests/FritzBoxColorControlTests.cs
  - HomeAutomationServerTests/UnitTests/FritzBoxHttpClientTests.cs
  - HomeAutomationServerTests/UnitTests/FritzBoxSmartHomeMappingTests.cs
  - HomeAutomationServerTests/UnitTests/FritzBoxSmartHomeServiceTests.cs
  - HomeAutomationServerTests/UnitTests/Fakes/FritzBoxSmartHomeFixtures.cs
  - HomeAutomationServerTests/UnitTests/Fakes/FakeFritzBox.cs
  - HomeAutomationServer/Services/DataCollectors/FritzBoxDataCollector.cs
  - HomeAutomationServerTests/UnitTests/FritzBoxDataCollectorTests.cs
---

# The FritzBox library: smart home over the REST API, AHA as the fallback

Phase 1.1 of `.claude/plans/FritzBoxUsp-plan.md`. `FritzBoxSmartHomeService` (in `FritzBox/SmartHome`) is an
`IFritzBoxService` that reads and switches the smart home of a box:
- over the **Smart Home REST API** (`api/v0/smarthome`) on FRITZ!OS 8.20 and later,
- over **AHA** (`webservices/homeautoswitch.lua`) before that.

It hands out the same `FritzBoxDevice`s either way, so the server, its JSON and both clients need not know which
interface is in use.

**The HA server uses it since phase 1.2** (2026-10-24): `Program.cs` registers it as `IFritzBoxService`, and
`FritzBoxDataCollector` asks for one per configured box. The WPF app still registers the old AHA service
`Fronius/Services/FritzBoxService.cs` until phase 1.3, and the old service is deleted then. Until that happens, its
four AHA command strings exist twice: once there and once as the fallback here.

## The parts

| File | What |
|---|---|
| `Http/FritzBoxLogin.cs` | The response to the `login_sid.lua?version=2` challenge: PBKDF2 (`2$…`) or MD5. The tests hold it against AVM's published test vectors. |
| `Http/FritzBoxHttpClient.cs` | One `HttpClient` per box, with the session ID. It logs in when it first needs to. On 401 or 403 it logs in again and repeats the request once. Also: `GetFritzOsVersionAsync` from `jason_boxinfo.xml`, which needs no login. |
| `SmartHome/SmartHomeOverview.cs` | The REST JSON as far as it is read or written. The same interface classes serve as the PUT body. |
| `SmartHome/SmartHomeClient.cs` | `GET smarthome/overview`, `PUT …/units/{UID}`, `POST …/templates/{UID}` (apply), `PUT …/triggers/{UID}` (enable). |
| `SmartHome/SmartHomeMapper.cs` | Overview → `FritzBoxDeviceList`. |
| `SmartHome/FritzBoxSmartHomeService.cs` | The `IFritzBoxService`. It picks REST or AHA once per connection, from the version. |

`FritzBox.csproj` references `Fronius` for the device models and `IFritzBoxService`. The reference does not go the
other way. See `Fronius.SharedLibraryBoundary.md` for why the library is not part of `Fronius`.

## Mapping REST onto the AHA model

- **One `FritzBoxDevice` per unit, under the unit's AIN.**
  - AHA lists a lamp twice: as the device (`13077 0050820`, `functionbitmask` 1, it can do nothing) and as its unit
    (`13077 0050820-1`, which switches, dims and colors). Only the unit is of use, so REST makes no device entry.
  - An outlet is a single unit with the AIN of its device in both interfaces.
  - The keys on the server (AIN without spaces) stay the same.
  - Group units are left out, as AHA's `<group>` elements always were.
- **Units:**
  - Power in mW and voltage in mV, the same as AHA.
  - Energy in Wh.
  - Temperature in °C. AHA counts it in 0.1 °C.
  - Level 0..100. AHA counts 0..255 and also gives the percentage.
  - Hue 0..359 and saturation 0..255, the same as AHA.
- **Locks:** AHA `lock` is REST `isLockedDeviceApi`, and AHA `devicelock` is REST `isLockedDeviceLocal`. This was
  checked on all 13 outlets in Neufahrn. AHA has a `<switch>` element (with the locks) only for outlets.
- **What REST does not have:**
  - The numeric `id`. `SmartHomeMapper.GetId` makes one up: the FNV-1a hash of the UID, the same at every poll.
    The WPF app still tells devices apart by `Id`.
  - The switching mode `auto`/`manuell`, and `txbusy`. Both are left empty.
  - The microphone bit of `functionbitmask`. Some outlets have it in AHA.
  - The **temperature offset**. It is only in `smarthome/configuration/units/{UID}`. The REST temperature is the
    one the device measures, without the offset; AHA's `celsius` presumably includes it. All offsets in Neufahrn
    are 0, so the two agree there.
- **What REST does better:** model and firmware of a lamp come from its device. AHA's lamp unit says `fwversion="0.0"`
  and sometimes an empty `productname`.
- **A device out of radio range:** the REST API leaves its values out and reports `state: notConnected`. The
  specification says so, but no such device has been seen yet.

The fixture in `FritzBoxSmartHomeFixtures` is the same moment of the Neufahrn box in both interfaces, anonymized.
`FritzBoxSmartHomeMappingTests` holds the mapping against AHA field by field. **When the mapping changes, that test
says whether AHA and REST still agree.**

## How the HA server polls: `FritzBoxDataCollector`

- **One loop per box**, every `RefreshRate`, which `Program.cs` sets to 2 s.
- **A device is published only when its JSON differs from what was last published.** The JSON is what a client
  would see, so "changed" means changed for a client. `SignalRDispatcher` pushes what is published, so the clients
  hear of changes only. A new connection is still greeted with every device (`HomeAutomationHub.OnConnectedAsync`),
  which is why the clients need no catch-up for these devices (`UpdateVisibility.Lifecycle.md`).
- **A device that has not changed still takes the fresh values** through `FritzBoxDevice.CopyFrom`, without a push.
  - This matters because a command (`TurnOnOff` and the others) locks the device instance: `IsSwitchingEnabled`
    and its siblings turn false, and `DevicesController` refuses the next command until the device has been read
    again.
  - When every poll replaced the instance, the lock went with it. Now a command that changes nothing (switching on
    an outlet that is on) would have locked the device for good without the copy.
- **A command reads the box again a second later.** `IFritzBoxService.CommandSent` fires after a command, and the
  collector then reads after `CommandEchoDelay` (1 s) instead of at the end of the interval, through a
  `ReadNowRequest` (`Gen24Polling.md`).
- **A device the box no longer lists is withdrawn** (`DeviceAction.Delete`). Before 1.2, it stayed until the server
  restarted.
- **Failures are logged once** through `RepeatedFailure.Log`, the same as for the inverters. The recovery is logged
  once as well. `RepeatedFailureTests` covers both with a capturing logger.
- **A failed round ends that round only.** That includes a timeout of the box, which `HttpClient` reports as a
  `TaskCanceledException`. Only the collector's own token ends the loop. Catching "everything but
  `OperationCanceledException`" let a timeout end the loop of that box for good, without a word in the log.
- **How it is tested:** `FritzBoxDataCollectorTests` runs the real collector against a real `DataControlService` and a
  fake `IFritzBoxService` whose `GetDevices` takes the next round (devices or an exception) from a `Channel` the test
  writes to. `RefreshRate` is zero, and a test goes on when the collector has *asked for round n+1*, which proves
  that round n is published - so nothing sleeps, and every wait has a timeout. Reading the control service from the
  test thread is safe at that point: the collector is blocked in `GetDevices`. The one real wait is the check that
  two commands cause only one read, because a read that does not happen cannot be waited for.
- **The device ids are `Manufacturer;Model;SerialNumber`** (`IHaveUniqueId.Id`). AHA left the model of one lamp unit
  empty, and REST fills it from the device. That lamp's id therefore changed once, with the switch to REST:
  `AVM;130770053217-1` became `AVM;FRITZ!Smart_Light_500;130770053217-1`. The AHA device entries of the lamps
  (`functionbitmask` 1, not switchable) are gone from the server.

## Measured on the box in Neufahrn (FRITZ!OS 8.25), 2026-10-24

- `jason_boxinfo.xml` answers without a login: `<j:Version>279.08.25</j:Version>`, which is hardware 279 and
  FRITZ!OS 8.25. It also names the box's serial number.
- The login with PBKDF2 works. `GET smarthome/overview` returns 17 units: 13 outlets and 4 lamps.
- `PUT …/units/08761 0004275` with `{"interfaces":{"onOffInterface":{"active":true}}}` switches the outlet.
  - An overview read in the same instant still shows the old state.
  - The next read, about a second later, shows the new one.
  - The power follows after about five seconds.
  - **So a poll straight after a command (phase 1.2) has to wait about a second**, or it shows the old state.
- On the lamp `13077 0050820-1`, switched off, these were each read back about 1.5 s later:
  - `colorTemperature` 2700
  - `level` 50
  - `hsColor` 120/128, which turns `currentColorMode` to `hueSaturation`
  - `colorTemperature` 3000, which turns it back to `temperature`
  As AVM says, none of them switched the lamp on.
- `PUT …/triggers/{UID}` with only `{"enabled":true}` is accepted.
- Applying a template (`POST …/templates/{UID}`) has not been tried, because it switches things in the house.
- Commands work with the AIN written with or without its space. The service finds the UID in the last overview.
- Probe commands for all of this: `smarthome [<ain> on|off|level <percent>|kelvin <K>|color <hue> <percent>]` and
  `routine <uid> on|off` (`Tools/FritzBoxUspProbe/README.md`).

## Things that bite

- **When the version cannot be read, AHA is used.** That covers a 404, an answer that is not XML (an HTML page, an
  empty body), and a `j:Version` that is not `<hw>.<major>.<minor>`. AHA works on every box, so this is the safe
  side. `XDocument.Parse` throws on what is not XML; without the catch, every call of the service would fail.
- `HttpClient.BaseAddress` needs the trailing `/`, otherwise a path in the base URL is lost. The service adds it.
  A `BaseUrl` that is not an absolute http or https address is refused with `InvalidOperationException` before
  anything is sent. Without that check an empty one would become `file:///` on Linux.
- A level, temperature or color of NaN (the web API's `double` parameters let it through) is sent as the lowest
  allowed value, not as 0.
- The service accepts the box's certificate unchecked, as the old AHA service did. Phase 1.4 of the plan changes
  that.
