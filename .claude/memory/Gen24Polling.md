---
paths:
  - Fronius/Services/DataCollectors/Gen24DataCollector.cs
  - Fronius/Services/DataCollectors/ReadNowRequest.cs
  - Fronius/Services/DataCollectors/RepeatedFailure.cs
  - Fronius/Services/Gen24Service.cs
  - Fronius/Services/DigestAuthHttp.cs
  - Fronius/Contracts/IGen24ConfigRefresher.cs
---

# How an inverter is polled

`Gen24DataCollector` is the only thing that talks to a Gen24 inverter on a schedule, and everything the clients
know about one comes from it: it writes into `IDataControlService`, which raises `DeviceUpdate`, which
`SignalRDispatcher` sends on. Nothing else reads an inverter periodically. What the JSON it reads is parsed
with, and where that differs from the Newtonsoft it used to be, is [[DeviceJson]].

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

**All three dictionaries of the collector learned this the hard way.** `runningSensorTasks` and
`runningConfigTasks` were keyed by the connection while `Update` puts a fresh clone in as the key on every round,
so no round ever replaced the entry of the one before it: each left another entry behind holding a task that had
already finished, for as long as the server ran, and `StopAsync` then waited on every one of them.
`StopAsync` also clears all three, or a restart - which builds new inverters, and therefore new keys - would leave
the entries of the previous run behind for good.

`Gen24DataCollectorTests` starts a real collector for a second to show it, because there is no round without one.
It points the connection at `http://` - not a URL an `HttpClient` can be built for - so every read fails at once,
offline and without a socket. A closed port does not work for that: connecting to one takes the full 30 second
timeout of the client and then a retry, so the first round never finishes.

## A stop is not a failure

`UpdateConfig` spends nearly all its time in that wait, so nearly every stop arrives during it - and the wait used
to sit *outside* the `try`, which faulted the task and made `StopAsync` throw while waiting for it. The wait is
inside now, and cancellation is caught as `OperationCanceledException` rather than `TaskCanceledException`: a
delay throws the latter but a semaphore throws the former, and `catch (TaskCanceledException)` does not see it.

## An inverter that is switched off says so once

Both loops fail on every round while an inverter is off, and both used to write a stack trace for it - one per
`RefreshRate` from the sensors and one per `ConfigRefreshRate` from the config, which is a few hundred identical
traces overnight. They go through `Report` now, which asks a `RepeatedFailure`:

| | |
|---|---|
| the first failure of its kind | `LogError` with the exception |
| the same thing again | `LogDebug`, so it is there if you ask for it |
| the same thing an hour later | `LogWarning`, once, with the count |
| the inverter answering again | `LogInformation` with how many attempts it took and how long |

"The same thing" is the type and the message of the exception, so a *different* fault is news and gets its own
line whatever has been failing so far. One `RepeatedFailure` per loop, handed down through the rounds the way the
semaphore is - no fourth dictionary.

The two loops used to log the same sentence, `Could not read config`, for three different things; the sensor loop
now says what it was actually doing.

## Still open

- `lastLogTime`, which throttles the energy history file, is a field of the collector rather than of an inverter.
  With more than one inverter they share it, so one can keep the other out of its own history file.
