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

## Who may connect at all

The direction rule above is about routing. Getting onto the hub in the first place is a separate gate:
`app.MapHub<HomeAutomationHub>("/hub").RequireAuthorization(policy => policy.RequireHubTicket())`.

`HubAuthentication` holds both halves of that decision - `AddHubTicketAuthentication` for the scheme,
`RequireHubTicket` for the policy (ticket scheme plus `Roles.User`) - so `Program.cs` and the tests cannot drift
apart. Never write the scheme name or the role at a call site.

**The hub does not take the Basic credentials the rest of the API uses.** A browser cannot set an `Authorization`
header on a WebSocket handshake, so SignalR passes the credential as the `access_token` query parameter, and query
strings land in the access log of every server and proxy on the way. Instead `GET api/Identity/hubTicket` (Basic
authenticated) hands out a short lived ticket, and the client feeds it to `AccessTokenProvider`:

- `HubTicketService` signs `v1.<user>.<expiry>` with a key derived from `IAesKeyProvider`, and folds the user's
  password hash and salt into the signature without putting them in the ticket. So a password change invalidates
  every outstanding ticket, and a ticket recovered from a log is worthless within `HubTicketService.Lifetime`.
- The signature is checked **before** the expiry: the claimed expiry of a ticket nobody signed means nothing.
- `HubTicketAuthenticationService` reads the ticket from `access_token` (WebSockets) or from a `Bearer` header
  (negotiate and long polling), and hands back the same principal the Basic handler builds -
  `AuthorizationExtensions.CreateAuthenticationTicket` is the one place that turns a `User` into role claims.
- The client fetches a ticket per connection attempt rather than keeping one; SignalR asks `AccessTokenProvider`
  again on every reconnect.

`Roles` is a `[Flags]` enum and `RequireHubTicket` asks for `Roles.User` specifically, so a user who holds only
`Administrator` or only `Guest` cannot connect. That is deliberate, but it means the User bit has to be set on
anyone who should see the device stream.

Tests: `UnitTests/HubTicketServiceTests` covers forging, tampering, expiry, an unknown user and a changed password
(including flipping every single bit of the signature); `UnitTests/Hosted/HubAuthenticationTests` proves over a real
connection that no ticket, a foreign ticket, an expired ticket and a user without the role are all turned away,
while a valid ticket gets in. Weakening `RequireHubTicket` fails exactly those four.
