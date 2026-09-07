---
paths:
  - Fronius/Services/DataCollectors/Gen24DataCollector.cs
  - Fronius/Services/DataCollectors/ReadNowRequest.cs
  - Fronius/Services/Gen24Service.cs
  - Fronius/Services/DigestAuthHttp.cs
  - Fronius/Contracts/IGen24ConfigRefresher.cs
---

# How an inverter is polled

`Gen24DataCollector` is the only thing that talks to a Gen24 inverter on a schedule, and everything the clients
know about one comes from it: it writes into `IDataControlService`, which raises `DeviceUpdate`, which
`SignalRDispatcher` sends on. Nothing else reads an inverter periodically.

## Two loops, one service, one inverter

Per inverter the collector starts **two** self-rescheduling tasks that share one `IGen24Service` and one
`Gen24System`:

| loop | reads | interval |
|---|---|---|
| `Update` | the sensors, and the config once if it has none yet | `RefreshRate` |
| `UpdateConfig` | `api/status/version`, `api/components/`, `api/config/` | `ConfigRefreshRate`, 5 minutes |

They overlap. A `SemaphoreSlim` per inverter keeps the two *config* reads apart, but a sensor read and a config
read do run at the same time, on the same service.

## Setting Connection is not free, and it is not a way to store state

`Gen24Service.Connection` used to dispose its `DigestAuthHttp` on every assignment. The property is assigned far
more often than the inverter changes:

- `Update` assigns a fresh **clone of the same connection** at the end of every round, and
- a controller assigns the connection it just looked up on **every request**.

So the client was pulled out from under whatever call was in flight on that service, and the config read - which
runs beside the sensor loop - died with
`ObjectDisposedException: De.Hochstaetter.Fronius.Services.DigestAuthHttp` every few minutes.

**The setter now only disposes when the endpoint really changes** - a different address, user or password. Setting
the same inverter again keeps the client, which also stops the digest handshake being redone every few seconds
for nothing. `DigestAuthHttp` looks after itself: it redoes the handshake on a 401 and replaces its own socket
after a timeout, so it does not need throwing away to stay healthy.

A genuine change of address still disposes a client that is in use, and that is left alone: a call on its way to
an inverter we no longer talk to has nowhere to arrive.

`Gen24ServiceConnectionTests` pins this down. It reads the private client field by reflection, because the field
is nulled whenever it is disposed - which makes "the same instance is still there" the same statement as "it was
not disposed" - and everything else that would show the client needs a real inverter.

## A write asks for the next read

`ConfigRefreshRate` is five minutes, so without this a setting written through the API would not reach any client
for minutes. `Gen24DataCollector` therefore implements `IGen24ConfigRefresher`, is registered under it as the same
singleton, and `Gen24SystemController` calls `ReadConfigNow(id)` after a write it accepted. See
[[SettingsDialogs.Lifecycle]] for the write itself.

The wait in `UpdateConfig` is a `ReadNowRequest` rather than a `Task.Delay`, which is what makes it interruptible:
a semaphore with one place in it, so a request made while the collector is asleep survives until it wakes, one
made *during* a read is answered by the next read, and asking twice is the same as asking once. The requests are
keyed by the `Gen24System` and **not** by its `WebConnection` - see below.

Only the settings writes ask. Standby lives in the sensors, which are polled every few seconds anyway.

## WebConnection does not compare by value

It is a `BindableBase` with no `Equals`, so two connections to the same inverter are two different keys. Anything
that has to find an inverter again must key on something else - the `Gen24System`, which is created once in
`StartAsync` and lives as long as the collector, or the `Id` from `IHaveUniqueId` (which is a default interface
member, so it needs a cast, and is empty until the first config read has happened).

## Still open

- **`runningSensorTasks` and `runningConfigTasks` grow without bound.** They are keyed by `WebConnection`, and
  `Update` puts a fresh clone in as the key on every round, so each round leaves a dictionary entry holding a
  completed `Task` behind it - for as long as the server runs. `StopAsync` then awaits every one of them. The
  clone itself came in with "Fixed Modbus stuff" and its reason is not recorded; `DigestAuthHttp` does not need it.
- `UpdateConfig` catches `Exception` and logs it, then reschedules. An inverter that is off overnight therefore
  fills the log with one stack trace per interval.
