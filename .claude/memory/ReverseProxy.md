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
- The server is reachable over plain http on the home LAN, past nginx: on the Pi the container runs with
  `network=host`, so 8080 is the Pi's own port (the `ports:` of `docker-compose.yml` do not apply there). That is why the forwarded headers are believed from listed proxies only (below), and why
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

**Observed on 2026-09-23, and what it turned out to mean:** with the proxy's `172.31.15.1` in `TrustedProxies`, the
server logged `from 172.24.0.1`. The server container on the Pi runs with `network=host`, and the Pi receives the
AWS server's connections from `172.31.15.1` (the developer checked `SSH_CONNECTION` from there). So the trust
**worked**: an untrusted sender would have been logged as `172.31.15.1` itself, and `172.24.0.1` can only have come
out of `X-Forwarded-For`. It is **nginx** that sees `172.24.0.1` as the client: nginx runs in a Docker container on
AWS (compose service `nginx`, published `443`/`80`), attached to two compose networks, one shared with other,
unrelated services (fixed `192.168.71.0/24`) and `home-automation` (no subnet given, so Docker's choice - evidently `172.24.0.0/16`, whose
gateway `172.24.0.1` is). The internet reaches it through Docker's port publishing with that gateway as sender -
IPv6 clients or Docker's userland proxy - which nginx then writes into `X-Forwarded-For` as `$remote_addr`.
Nothing to change on the server or the Pi, and
`172.24.0.1` must **not** be listed. The fix is on AWS, and **`network_mode: host` for nginx is ruled out** - it talks to
many other containers over its Docker network (the developer, 2026-09-23). What was suggested instead: find out
with `curl -4`/`curl -6` which clients nginx logs as `172.24.0.1`. IPv6 only (the likely case: Docker's default
path keeps the IPv4 sender, but hands IPv6 to an IPv4-only container through `docker-proxy`) - `enable_ipv6` and a
ULA subnet on **both** of nginx's compose networks (which of them carries the published ports is not visible from
the compose file), `home-automation`'s IPv4 subnet pinned to `172.24.0.0/16` at the same time, `ip6tables` on
(default since Docker 27), and `docker compose down` / `up -d` so the networks are recreated. IPv4 as well - `"userland-proxy": false`. Or drop the AAAA record.

**What was done, and works** - the Pi's log shows the real client address for IPv4 and IPv6 clients alike, verified
by the developer on 2026-09-24: `enable_ipv6` with a ULA subnet on both compose networks
on AWS, `home-automation` pinned to `172.24.0.0/16`, the networks recreated with `docker compose down` / `up -d`.
An unrelated MariaDB container on the other network authenticates by client address and must only
ever see IPv4, so it got `sysctls: net.ipv6.conf.all.disable_ipv6=1` and `...default.disable_ipv6=1`: it has no
IPv6 address then, Docker's DNS gives other containers only its IPv4 address, and the web container keeps
connecting from its `192.168.71.x`. Anyone changing those networks again has to keep that in mind. (Two
earlier readings - Docker rewriting on the Pi, then a tunnel address - were wrong and are withdrawn. Lesson: when
the logged address is not the connecting proxy's, it came from the header, so look at what the proxy itself sees.)
