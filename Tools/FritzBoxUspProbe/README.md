# FritzBoxUspProbe

Phase 1.0 of `.claude/plans/FritzBoxUsp-plan.md`: finds out what the FRITZ!Box offers over USP (TR-369), and
whether it pushes changes of the smart-home devices. Delete this tool once its findings are in
`.claude/memory/FritzBox.Usp.md`.

The probe registers a USP controller on the box (through TR-064) and then talks to the box over an MQTT broker.

## Where it runs

Everything runs on `amazon2.hochstaetter.de` (AWS, Alpine, reachable by SSM). It reaches the box in Neufahrn
(192.168.44.11) through the WireGuard tunnel. The files of `amazon2/` are what is installed there:

| On amazon2 | From | What |
|---|---|---|
| `/opt/mosquitto/docker-compose.yml` | `amazon2/docker-compose.yml` | Mosquitto 2: port 8883 with TLS from the internet, port 1883 without TLS on 127.0.0.1 only |
| `/opt/mosquitto/config/mosquitto.conf`, `acl` | `amazon2/config/` | No anonymous clients, a password per client, an ACL |
| `/opt/mosquitto/config/passwd`, `/opt/mosquitto/secrets.env` | made on amazon2 | Random passwords of `usp-probe` and `fritzbox-neufahrn`, readable by root only |
| `/opt/mosquitto/update-cert.sh`, `/etc/periodic/daily/mosquitto-cert` | `amazon2/update-cert.sh` | Copies the Let's Encrypt certificate of `amazon2.hochstaetter.de`, which certbot in `nginx.kartellvz` renews, and restarts Mosquitto when it changed |
| `/opt/fritzbox-usp-probe/probe.sh` | `amazon2/probe.sh` | Runs the probe in `mcr.microsoft.com/dotnet/runtime:10.0-alpine` |
| `/opt/fritzbox-usp-probe/app/` | `dotnet publish` | The probe |
| `/opt/fritzbox-usp-probe/probe.json`, `secrets.env` | written on amazon2 | Settings (see `probe.sample.json`) and the password of the box user, readable by root only |

The security group `mosquitto-mqtts` opens port 8883 for IPv4 and IPv6. The box's public address in Neufahrn
changes daily, so the port cannot be limited to it.

**The box takes a controller only with TLS.** Without it, `AddUSPController` fails with UPnP error 600 "Argument
Value Invalid", whatever else is right.

## Running it

On amazon2 (as root, for example through SSM):

```sh
/opt/fritzbox-usp-probe/probe.sh info
```

| Step | Command | What to look for |
|---|---|---|
| 1 | `info` | FRITZ!OS 8.01 or later, `X_AVM-DE_USPController` offered |
| 2 | `register` | Needs somebody at the box: it asks for a button or for a code dialed on a phone connected to the box, within 3 minutes. Then "Registered as controller n", and `docker logs mosquitto` shows `fritzbox-neufahrn` connecting. |
| 3 | `listen 60` | Anything the box publishes |
| 4 | `explore` | Switches the controller off and on, waits for the box to introduce itself, reads the whole data model |
| 5 | `subscribe 120 <path>` | Switch an outlet by hand meanwhile. Each change should print a line. |
| 6 | `get <path>`, `set <path> <value>` | Reading and switching one value |
| 7 | `unregister` | Only to remove the controller: the HA server is meant to take it over |

The results go to `/opt/fritzbox-usp-probe/probe-output/<time>-<command>/`. `probe-output/agent.json` remembers the
box's Endpoint ID and topic for the later commands; delete it if the box changed them.

## Updating the probe on amazon2

```sh
dotnet publish Tools/FritzBoxUspProbe -c Release -o publish -p:InvariantGlobalization=true -p:UseAppHost=false
```

Copy the contents of `publish/` to `/opt/fritzbox-usp-probe/app/`. There is no direct way from Claude Code on the
web to amazon2; a short-lived private S3 bucket with presigned URLs works.

## Running it elsewhere

The probe needs the box's TR-064 and the broker. `probe.sample.json` shows the settings: `Broker` is how the probe
reaches the broker, `Controller.BoxBroker*` how the box reaches it. Passwords come from `FRITZBOX_PASSWORD`,
`MQTT_PASSWORD` and `MQTT_BOX_PASSWORD`, or are asked for on a real console.
