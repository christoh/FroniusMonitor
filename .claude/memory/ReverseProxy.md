---
paths:
  - HomeAutomationServer/Misc/ReverseProxies.cs
  - HomeAutomationServer/Models/Settings/WebServerSettings.cs
  - HomeAutomationServer/Misc/BrowserTabSessions.cs
  - HomeAutomationServer/Settings.xml.example
  - HomeAutomationServerTests/UnitTests/Hosted/ReverseProxyTests.cs
  - docker-compose.yml
---

# The server behind a reverse proxy

## How it is deployed

Told by the developer on 2026-09-23: the server **never runs https itself**. It runs http in a Docker container
(Kestrel on 8080, `ASPNETCORE_HTTP_PORTS`) on a **Raspberry Pi at home**, `192.168.44.50`. It sits there, next to the
inverters, because they talk to its Modbus server, and that has to go on working when the internet connection
fails. The https is done by an **nginx on an AWS server**, which reaches the Raspberry Pi's LAN address from outside
(so over some tunnel or VPN) and forwards by path:

| nginx location | goes to |
|---|---|
| `/` | the client container on AWS (`home-automation-client`), which serves the browser client's download |
| `/api/`, `/openapi/`, `/hub` | the Raspberry Pi, `http://192.168.44.50:8080`, with `X-Forwarded-For` and `X-Forwarded-Proto` |

`/` going to its own container is **deliberate and stays** - do not suggest moving it to the server, although the
server can host the client itself: the download is served from AWS, not over the home connection. The complete
nginx configuration the developer was given on 2026-09-23 forwards all three server paths with the same headers
(`/openapi/` was missing before, so the OpenAPI tab could not work through nginx; `/hub` had no `X-Forwarded-*`).

Consequences that are easy to forget:

- **Only `/api/`, `/openapi/` and `/hub` reach the server through nginx.** Anything else the server serves - the
  browser client it hosts at `/`, the `/api`/`/hub` 404 fallbacks of `BrowserClientHosting` - only matters for
  direct access. **That direct access is used:** at home the developer opens `http://192.168.44.50:8080`, the
  client the server hosts itself, as a shortcut past AWS (told on 2026-09-23). It is plain http on the LAN, with
  no forwarded headers, and it has to keep working - **except the OpenAPI tab**, whose always-`Secure` cookie the
  browser drops there. The developer accepted that on 2026-09-23 ("OpenAPI is expendable when I use it from
  home") and declined `Secure = IsHttps || X-Forwarded-Proto present`, which would have made it work locally. Do not
  change the cookie for that again unless asked. A new server path needs a `location` in nginx as well, or it answers from the client container.
- `docker-compose.yml` publishes 8080 on the Raspberry Pi, so the server is also reachable over plain http on the
  home LAN, past nginx. That is why the forwarded headers are believed from listed proxies only (below), and why
  the `tab` cookie is always `Secure` (a browser drops it on a plain http page, `http://localhost` excepted).

## `X-Forwarded-For` / `X-Forwarded-Proto`: `ReverseProxies`

`Program.cs` calls `UseForwardedHeaders(ReverseProxies.CreateOptions(...))` **first**, before compression, CORS and
authentication, so every `HttpContext.Connection.RemoteIpAddress` after it - 22 "from {Ip}" log lines in the
controllers, the authentication and `BrowserTabSessions` - is the client's, and `Request.Scheme` is `https`.

- Believed only from `WebServerSettings/TrustedProxies` in Settings.xml (`<Proxy>` entries, an address or a CIDR
  network), plus loopback, which ASP.NET trusts by default and `CreateOptions` leaves in. Anything else could put
  an `X-Forwarded-For` in its request and choose the address it is logged under.
- One hop (`ForwardLimit` default 1): the last entry, which nginx appends with `$proxy_add_x_forwarded_for`. What
  the client itself sent in `X-Forwarded-For` stands before it and is ignored, so it cannot be spoofed through
  nginx either.
- The container listens dual stack, so an IPv4 proxy arrives as `::ffff:a.b.c.d`. ASP.NET's middleware maps it back
  before comparing, so the plain IPv4 notation in Settings.xml is enough - `ReverseProxyTests` checks that against
  the real middleware without a socket, because the machines the tests run on (this cloud) may have no IPv6.
- An entry that is neither an address nor a network is logged as a warning and left out. At startup the server
  logs which proxies it trusts, or that it trusts none and where to add one.
- `X-Real-IP` is not read; nginx sends `X-Forwarded-For` as well, which is the standard one.

The address to list is the one the server logs for requests that came through nginx while nothing is listed.
With nginx on AWS that is the address its requests arrive from at home - the tunnel's or VPN's end, not AWS's
public address.
