---
paths:
  - HomeAutomationServer/Hubs/**
  - HomeAutomationServer/Program.cs
  - HomeAutomationServer/Services/SignalRDispatcher.cs
  - HomeAutomationServerTests/**
  - HomeAutomationClient/HomeAutomationClient/Services/UpdateService.cs
---

# Which way a SignalR message may travel

Applies to `HomeAutomationHub` and to everything that pushes over it.

## Rule

- **Server to client: unrestricted.** The server may address any client, all clients, a group or a user.
- **Client to server: allowed.** A client may invoke a hub method, and the server may answer that caller.
- **Client to client: never.** Anything a client-invoked hub method tries to send to somebody other than the
  caller is discarded.

SignalR has no client-to-client transport of its own - the only way one client can reach another is by talking the
server into relaying. So the rule is enforced exactly where that relay would happen: inside a hub method invoked by
a client.

## How it is enforced

`ClientToServerOnlyHubFilter` is registered as a hub filter by
`SignalRRegistration.AddHomeAutomationSignalR`, which `Program.cs` calls:

```csharp
builder.Services.AddHomeAutomationSignalR();
```

That extension holds the whole SignalR setup - payload serialization and the filter - in one place, so the tests
can build a host with the real registration instead of a copy of it that could drift out of step. Never call
`AddSignalR` directly; the filter would be missing.

For the duration of a client-invoked hub method it swaps `Hub.Clients` for a `CallerOnlyHubCallerClients`. That
wrapper passes `Caller` (and `Client(<the caller's own connection id>)`) through to the real proxy and returns a
`DiscardingClientProxy` for `All`, `Others`, `AllExcept`, `Clients`, `Group`, `Groups`, `GroupExcept`,
`OthersInGroup`, `User` and `Users`. A discarded send is logged as a warning and returns a completed task; a
discarded `InvokeCoreAsync` throws, because no client will ever produce the answer it waits for.

The guard is deliberately structural rather than a review rule: a hub method added later cannot leak to other
clients even if it tries.

## What the filter does *not* touch

- **`IHubContext<HomeAutomationHub>`** - `SignalRDispatcher` pushes device updates through
  `hubContext.Clients.All` and stays unrestricted. That is the server talking, not a client.
- **Hub lifetime methods.** The filter implements only `InvokeMethodAsync`, so `OnConnectedAsync` keeps the real
  `Clients` and can still replay the current device list to `Clients.Caller`.
- **`Hub.Groups`.** Joining or leaving a group sends nothing to anybody, so group membership is not restricted.

## Writing a new hub method

Put the server-side work in the method body and, if the caller needs an answer, send it to `Clients.Caller` or
return a value. Never reach for `Clients.All` or a group - it will silently do nothing and log a warning.
`HomeAutomationHub.SendGen24Message` is the pattern: it used to relay to `Clients.All`, which let any client push
arbitrary data to every other client, and now only hands the message to the server.

To fan something out *in response* to a client's message, do it from the server side - raise the state change that
`SignalRDispatcher` already listens to, or take `IHubContext<HomeAutomationHub>` and send from there. That is a
deliberate server decision rather than a client relay, so it is allowed; make sure the payload is the server's own
view of the data and not something the client handed you unchecked.

## Tests

`HomeAutomationServerTests` (xUnit) covers the rule on two levels, and both must stay green:

- `UnitTests/ClientToServerOnlyHubFilterTests` drives the filter directly against a `RecordingHubCallerClients`
  that writes down every send. A `[Theory]` walks all eleven ways a hub method can address somebody else and
  asserts nothing was recorded; further facts cover the caller still being answerable, the invoke that throws,
  `Hub.Clients` being restored (also when the hub method throws) and lifetime methods keeping the real `Clients`.
- `UnitTests/Hosted/HubMessageDirectionTests` starts a real Kestrel host on `127.0.0.1:0` using
  `AddHomeAutomationSignalR` and connects two real SignalR clients to a `DirectionProbeHub`. These are end to end
  tests, but they live under `UnitTests` on purpose: they are hermetic - loopback only, no devices, no
  configuration - so they must run with the ordinary unit tests rather than be skipped as environment dependent.

The negative assertions are "B never received it", which would also hold if nothing worked at all. They are kept
honest by never sleeping: after the hub method the test pushes a message from the server and waits for *that* to
arrive at every client. SignalR preserves order per connection, so once the later message is there, anything the
hub method sent has had its chance. Both levels were checked by breaking the guard on purpose - dropping the filter
from the registration fails exactly the three integration tests, and letting `All` through fails the `All` theory
case and the broadcast test.

`ProbeClient` also registers a working handler for the client-result call, so "the invocation never arrived" cannot
be confused with "the client had no handler".

## Still open

The hub is **unauthenticated**: `Program.cs` maps it as
`app.MapHub<HomeAutomationHub>("/hub");//.RequireAuthorization(r=>r.RequireRole("User"))`. Anyone who can reach the
endpoint gets the full device stream. The direction rule above is about routing only and does not address that.
