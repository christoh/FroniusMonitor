---
paths:
  - HomeAutomationServer/Services/ApiAuthenticationService.cs
  - HomeAutomationServer/Services/ApiAuthentication.cs
  - HomeAutomationServer/Services/BearerTokenService.cs
  - HomeAutomationServer/Services/HubTicketAuthenticationService.cs
  - HomeAutomationServer/Models/Settings/AuthenticationSettings.cs
  - HomeAutomationServer/Models/Settings/Settings.cs
  - HomeAutomationServer/Settings.xml.example
  - HomeAutomationServer/Models/Authorization/ApiAuthorizeAttribute.cs
  - HomeAutomationServer/Models/Authorization/UserList.cs
  - HomeAutomationServer/Models/Authorization/User.cs
  - HomeAutomationServer/Controllers/IdentityController.cs
  - HomeAutomationServer/Program.cs
  - Fronius/Models/WebApi/LoginRequest.cs
  - Fronius/Models/WebApi/LoginResponse.cs
  - HomeAutomationClient/HomeAutomationClient/Contracts/IWebClientService.cs
  - HomeAutomationClient/HomeAutomationClient/Services/WebClientService.cs
  - HomeAutomationServerTests/UnitTests/BearerTokenServiceTests.cs
  - HomeAutomationServerTests/UnitTests/Hosted/ApiAuthenticationTests.cs
  - HomeAutomationServerTests/UnitTests/Fakes/TestDoubles.cs
---

# How the API authenticates: bearer tokens, with Basic and cookie for debugging

Since 2026-09-23 (branch `features/bearer-authentication`) every call of the API is authenticated by a bearer
token. The hub is not: it keeps its own short-lived ticket, see `SignalR.MessageDirection.md`.

## The flow

1. `POST api/Identity/login` with a `LoginRequest` (`UserName`, `Password`) in the body answers a `LoginResponse`:
   the `UserInfo` (name, roles) plus `AccessToken` and `ExpiresInSeconds`. The body, not the query string, because
   a query string lands in every access log on the way.
2. Every later request carries `Authorization: Bearer <token>`.
3. `POST api/Identity/token` (`[ApiAuthorize]`) swaps a valid token for a new one. The old one **stays valid until
   it expires** - a request already on its way while the client renewed is not refused. Each renewal therefore
   leaves one token behind per client; `BearerTokenService.Issue` removes the expired ones every time it runs.
4. `GET api/Identity/logout` (no authorization) revokes the token the request carries - header or cookie - and
   deletes the cookie.

`GET api/Identity/login?user=&password=` still exists and answers the same `LoginResponse`. It is the browser
address-bar login for debugging together with the cookie; it puts the password into the query string, which is
why the client no longer uses it.

## The server keeps the tokens in memory, and only there

`BearerTokenService` is a singleton holding a `ConcurrentDictionary` from the token (32 random bytes, base64url) to
the `User` object, the password hash and salt of the moment it was issued, and the expiry. Asked for by the
developer: a server has fewer than ten users, so there is no database and no signature. **A restart of the server
forgets every token** - that is the accepted price, and the client is built to absorb it (below).

`Validate` looks the user up again by name through `UserList.Find` and requires the **same object** back, plus an
unchanged `PasswordHash` and `Salt`. So:

- a deleted user's tokens stop working at once,
- a user deleted and re-created under the same name does not inherit the old sessions,
- a changed password ends every session of that user (as with hub tickets),
- a **rename keeps the session**, because `UpdateUser` renames the very `User` object.
- the guest works like anyone else while `EnableGuestAccount` is on (`Find` answers the static `User.Guest`).

Unknown tokens are logged at Information, not Warning: every client presents one of those once after each restart.

## Settings.xml: the `Authentication` element

`AuthenticationSettings`, reached as `Settings.Authentication` and handed on as `UserList.Authentication` (the very
object - `Program.cs` configures `UserList` with it, the way it does `Users`). All three elements are written
whatever they hold, no `[DefaultValue]`, like `EnableGuestAccount`:

| Element | Default | Meaning |
|---|---|---|
| `BearerTokenLifetimeMinutes` | 30 | Anything below 1 is taken as 1 (`BearerTokenLifetime`). |
| `EnableBasicAuthentication` | false | `Authorization: Basic` accepted on every request. |
| `EnableCookieAuthentication` | false | Login also sets the `auth` cookie; the cookie is accepted. |

A `Settings.xml` from before this element gets the defaults, i.e. **Basic is off after the upgrade**. Anything
that still sends Basic - an old client, `curl`, a script - gets 401 until someone switches it on.
`Program.LogAuthentication` logs the lifetime and warns at every start about each debugging scheme that is on.

`BearerTokenLifetime` is a get-only property, which `XmlSerializer` skips; only the minutes are in the file.

`Settings.xml.example` shows the element with its defaults and a comment explaining it.
`BearerTokenServiceTests.The_example_settings_file_shows_the_defaults` loads the example with the real
`Settings.Load` and compares it with `new AuthenticationSettings()`: change a default and the example has to follow.

## `ApiAuthenticationService`: one scheme, `"Api"`

Replaced the old `AuthenticationService` ("Basic" scheme); `[ApiAuthorize]` replaced `[BasicAuthorize]` on every
controller. Registered through `ApiAuthentication.AddApiAuthentication()`, which the tests call as well, so they host
the scheme the server runs. There is still **no default scheme** - two are registered, the API's and the hub's - so
every policy names its scheme; `MapOpenApi` got `AddAuthenticationSchemes(ApiAuthenticationService.SchemeName)` for
that reason (without it the Developer role could never be satisfied).

Order inside `HandleAuthenticateAsync`: the `Authorization` header; only if there is none, and cookies are on, the
`auth` cookie. The value is then parsed the same way whichever it came from:

- `Bearer x` -> `BearerTokenService.Validate`.
- `Basic x` -> refused with a warning unless `EnableBasicAuthentication`; a malformed base64 value is a 401, not the
  500 the old handler produced.

The challenge (`HandleChallengeAsync`, not in authenticate as before) always offers `Bearer` and offers `Basic` only
while Basic is on: a browser answers a Basic challenge with a login box of its own, which must not pop up in front
of the browser client.

`ApiAuthenticationService.GetCredentials(header, scheme)` is the one parser of `Authorization` values;
`HubTicketAuthenticationService` uses it too for the ticket SignalR sends as `Bearer` on negotiate/long polling.

## The cookie holds the bearer token, not the password

It used to hold the Basic header, i.e. the password, for seven days. Now `IdentityController.IssueToken` sets
`auth=Bearer <token>` with `MaxAge` = token lifetime, `Path=/api`, **`HttpOnly`** and **`SameSite=Strict`**. SameSite
matters: the CORS policy allows every origin with credentials, so without it any page the logged-in browser opens
could call the API. A token that expires, or a server restart, means logging in again in the browser - acceptable
for a debugging aid.

## The client: `WebClientService`

- `Login` posts the `LoginRequest` and keeps token, expiry by the device's clock (`ExpiresInSeconds` is relative
  on purpose - a wrong client clock does not matter) and the `LoginRequest` itself. **The password stays in memory
  for the session**: that is what makes a server restart invisible. It returns a plain `UserInfo`; the token never
  leaves the service.
- The token is put on each request by a `DelegatingHandler` (`BearerTokenHandler`), **not** by
  `DefaultRequestHeaders` - those must not change while requests are in flight, and renewal runs off a timer.
- Renewal: a `TimeProvider` timer due `RenewalLead` (2 minutes) before expiry; a lifetime shorter than twice that is
  renewed half way instead. `RenewTokenAsync` never throws (it runs off a timer). On 401 it logs in again with the
  password; on anything else (server unreachable) it retries every `RenewalRetryInterval` (30 s) while the token is
  still valid, and after that leaves it to the next request.
- Every request goes through `SendAuthenticated`: on 401, `LoginAgain` logs in with the stored password and the
  request is sent **once** more. `sessionLock` (a `SemaphoreSlim`) serialises it; a request whose refused token has
  already been replaced by a concurrent re-login just retries with the new one instead of logging in again. Because
  of the repetition, every `send` lambda must build its request anew on each call - `PutAsJsonAsync` & co. do.
- `GetHubTicket` goes through `SendAuthenticated` too, so SignalR reconnecting after a server restart gets its ticket.
- **A login the server refuses (401) ends the session** (token, password, timer): otherwise a password changed on
  another device would be retried forever. A login that fails for any other reason - the server is still restarting
  - leaves the session as it was, so the password is still there once the server is back.
- `Logout` sends the request with the token (so the server can revoke it), then ends the session regardless of the
  answer. `Initialize` (a new server address) ends it too.

The constructor takes an optional `ILogger<WebClientService>` and `TimeProvider`; DI supplies the logger, the tests
pass a `ManualTimeProvider` (`Fakes/TestDoubles.cs`), whose timers never fire but record their due time, and call
the internal `RenewTokenAsync` themselves. `AccessToken` is internal for the tests.

## Tests

`BearerTokenServiceTests` - the store and the XML element. `Hosted/ApiAuthenticationTests` - end to end, including a
**real restart** (`RestartServer` disposes the host and starts a new one on the same address with the same users),
an expired token, renewal while the server is down, a password changed elsewhere, logout revocation, and Basic and
the cookie before and after being switched on.
