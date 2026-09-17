---
paths:
  - HomeAutomationServer/Hubs/**
  - HomeAutomationServer/Program.cs
  - HomeAutomationServer/Services/SignalRDispatcher.cs
  - HomeAutomationServer/Models/Authorization/DeviceVisibility.cs
  - HomeAutomationServer/Models/Authorization/AuthorizationExtensions.cs
  - HomeAutomationServer/Controllers/Gen24Controller.cs
  - Fronius/Models/WebApi/Roles.cs
  - Fronius/Models/WebApi/RolesExtensions.cs
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

- **`IHubContext<HomeAutomationHub>`** - `SignalRDispatcher` pushes device updates through `hubContext.Clients`
  (`All` or the `AllDevicesGroup`, see "What a guest sees") and stays unrestricted. That is the server talking,
  not a client.
- **Hub lifetime methods.** The filter implements only `InvokeMethodAsync`, so `OnConnectedAsync` keeps the real
  `Clients` and can still replay the current device list to `Clients.Caller`.
- **`Hub.Groups`.** Joining or leaving a group sends nothing to anybody, so group membership is not restricted.

## Writing a new hub method

Put the server-side work in the method body and, if the caller needs an answer, send it to `Clients.Caller` or
return a value. Never reach for `Clients.All` or a group - it will silently do nothing and log a warning.
`HomeAutomationHub.SendGen24Message` is the pattern: it used to relay to `Clients.All`, which let any client push
arbitrary data to every other client, and now only hands the message to the server.

**A hub method that changes a device needs its own role.** The ticket that opened the connection proves `User`
(or `Administrator`) and nothing more, and every write to a device in this system asks for `Operator`. So
`SetWattPilotSettings` and `RebootWattPilot` carry
`[Authorize(AuthenticationSchemes = HubTicketAuthenticationService.SchemeName, Roles = nameof(Roles.Operator))]`
- the scheme named, as in `RequireHubTicket`, so the policy does not fall back to Basic. The ticket principal
carries the same role claims Basic authentication builds (`CreateAuthenticationTicket`), so it works; a caller
without the role gets a `HubException`. `UnitTests/Hosted/HubWattPilotSettingsTests` proves it over a real
connection against the **real** `HomeAutomationHub` - it needs only `IDataControlService` and an
`IWattPilotServices`, both easily faked - and is the place to add the next one. What the method does with the
settings is in [[WattPilot]].

**The role is the device's, not a fixed one.** `SendToshibaHvacCommand` carries `Roles = nameof(Roles.PowerUser)`
- switching an air conditioner is what `DevicesController` lets power users do with a Fritz!Box outlet, not a
configuration change - and because `Roles` are flags, an operator *without* the PowerUser bit is refused there
while a power user is refused on the Wattpilot methods. `UnitTests/Hosted/HubToshibaHvacTests` pins both refusals
the same way. The hub constructor now also takes the `IToshibaHvacService` singleton, so every host that maps the
real hub registers one (`FakeToshibaHvacService` in the tests). What the command does is in the Toshiba HVAC memory, which lives in the AI's session memory, not in the repo.

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
`RequireHubTicket` for the policy (ticket scheme plus `Roles.User`, `Roles.Administrator` or `Roles.Guest`) - so
`Program.cs` and the tests cannot drift apart. Never write the scheme name or the role at a call site.

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
  again on every reconnect, so the renewal costs nothing to arrange.
- **`Lifetime` has to outlast a connection, not a handshake.** It was two minutes on the assumption that a ticket
  is only needed while connecting; in fact it goes on being presented for as long as the connection lives - every
  long polling request carries it - so a connection that outlived its ticket lost its authentication rather than
  its network. Ten minutes now. Shortening it again means making the client renew on a timer, not just on
  reconnect.

`Roles` is a `[Flags]` enum, so holding one role says nothing about the others - an administrator does not carry
the User bit unless somebody set it. `RequireHubTicket` therefore names `Roles.User`, `Roles.Administrator` and
`Roles.Guest`, and `RequireRole` lets **any** of the roles it is given through, never all of them. PowerUser,
Operator and Developer still do not get a connection on their own; add the role to that list rather than setting
the User bit on such a user by hand.

Tests: `UnitTests/HubTicketServiceTests` covers forging, tampering, expiry, an unknown user and a changed password
(including flipping every single bit of the signature); `UnitTests/Hosted/HubAuthenticationTests` proves over a real
connection that no ticket, a foreign ticket, an expired ticket and a user without the role are all turned away,
while a valid ticket - a user's, an administrator's, a guest's - gets in. Weakening `RequireHubTicket` fails exactly
those four.

### What a guest sees

Since 2026-09-14 a user with `Roles.Guest` and nothing more gets onto the hub and sees **the inverters, with the
smart meter and the battery a Gen24 carries inside, and nothing else** - no Fritz!Box outlet, no air conditioner,
no Wattpilot, no price data, no settings. The rule lives in two places that must agree:

- **Who sees everything** is `RolesExtensions.SeesAllDevices` in `Fronius` (User or Administrator), shared with
  the client. `AuthorizationExtensions.GetRoles` turns a `ClaimsPrincipal`'s role claims back into the flags so the
  server can ask the same question of a connection.
- **Which devices a guest sees** is `DeviceVisibility.IsVisibleToGuests` in the server - an allow list
  (`Gen24System`, `SunSpecInverter`, `SunSpecMeter`), so a device type added later is hidden until listed.

Enforcement is server side, in three places: `HomeAutomationHub.OnConnectedAsync` puts a user's connection into
the group `HubAuthentication.AllDevicesGroup` and greets a guest only with the visible entities;
`SignalRDispatcher` sends a visible device to `Clients.All` and everything else to that group alone; and the read
endpoints of `Gen24Controller` (`GetInverters`, `GetInverter`, `GetStandbyStatus`, `GetInverterLocalization` - the
last one the client needs at startup) carry `Roles = "User,Guest"`, while every other controller, the energy data
included, stays with `User`. The client only spares itself the refused calls: `UpdateService.StartAsync(Roles)`
skips the consumers and the price data for a guest, and `MainViewModel.ShowSettingsMenu` hides the Settings menu
(see the user management memory for why that is not the forbidden "clean up" of `SettingsItems`).

Since 2026-09-16 the role also has a built-in account behind it: `User.Guest`, named `guest`, which is in no user
list at all. Anything that resolves a user name must go through `AuthorizationExtensions.Find` or it will not find
it - see "The built-in guest is in no user list" in the user management memory.

`UnitTests/Hosted/HubGuestVisibilityTests` proves the greeting and the pushes over real connections against the
real hub and the real dispatcher; `UnitTests/RolesAndVisibilityTests` pins the two rules and the claims round trip.
The negative assertions there are kept honest the same way as in the direction tests: by waiting for a later
message the guest does receive.

### CORS has to be let through before authorization, or the hub is unreachable from a foreign origin

A client served from somewhere other than the server - the browser head run from a development server against a
real one, any future client on another host - sends a **preflight** before the negotiate, and a preflight carries
no credentials by design. SignalR maps its negotiate endpoint with `acceptCorsPreflight: true`, so that `OPTIONS`
**matches the endpoint** and picks up its `RequireAuthorization` metadata, unlike the controllers, whose endpoints
carry no authorization metadata at all and whose preflight therefore sails past.

So the order of the middleware decides whether a browser can reach the hub: with the authorization middleware
ahead of CORS, the preflight is answered with a bare 401 that has no `Access-Control-*` headers, and a browser
reports that as **`TypeError: Failed to fetch`** - a message that names neither authorization nor CORS, and looks
like the server is down. The symptom is unmistakable once seen: every REST call works and only
`HubConnection.StartAsync` fails.

**`WebApplication` puts `UseAuthentication` and `UseAuthorization` in front of every middleware `Main` registers**
when it adds them itself, so `app.UseCors()` alone can never be early enough. Naming all three explicitly, in the
order `UseCors` → `UseAuthentication` → `UseAuthorization`, suppresses the automatic ones and is the only thing
that fixes it. It does not weaken the gate: the negotiate itself is still refused without a valid ticket, it now
merely carries the CORS headers that let the client see the 401 for what it is.
