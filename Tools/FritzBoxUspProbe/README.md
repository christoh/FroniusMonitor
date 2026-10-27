# FritzBoxUspProbe

Phase 1.0 of `.claude/plans/FritzBoxUsp-plan.md`: finds out what the FRITZ!Box offers over USP (TR-369), and
whether it pushes changes of the smart-home devices. Since phase 1.1 it also tries the Smart Home REST API and AHA
(`aha`, `rest`, `smarthome`, `routine`). Delete this tool once its findings are in `.claude/memory/FritzBox.Usp.md`
and `.claude/memory/FritzBox.SmartHome.md` and nothing needs it any more.

The probe registers a USP controller on the box (through TR-064) and then talks to the box over an MQTT broker.

**The USP setup was removed on 2026-10-27 (plan, D9):**
- the controller `fqdn:hochstaetter.de:home-automation` on the box in Neufahrn
- Mosquitto on amazon2 with its daily certificate job
- the security group `mosquitto-mqtts`

Their files are in commit `2e576ad`. Without a broker, the USP commands (`register`, `listen`, `explore`,
`subscribe`, `get`, `set`, `delete`) no longer work. `info`, `unregister`, `aha`, `rest`, `smarthome` and `routine`
still do.

## Where it runs

Everything runs on `amazon2.hochstaetter.de` (AWS, Alpine, reachable by SSM). It reaches the box in Neufahrn
(192.168.44.11) and the Swiss box (192.168.3.1) through WireGuard.

| On amazon2 | From | What |
|---|---|---|
| `/opt/fritzbox-usp-probe/probe.sh` | `amazon2/probe.sh` | Runs the probe in `mcr.microsoft.com/dotnet/runtime:10.0-alpine` |
| `/opt/fritzbox-usp-probe/app/` | `dotnet publish` | The probe |
| `/opt/fritzbox-usp-probe/probe.json`, `secrets.env` | written on amazon2 | Settings for the box in Neufahrn (see `probe.sample.json`) and the password of the box user, readable by root only |
| `/opt/fritzbox-usp-probe/probe-ch.json` | written on amazon2 | The Swiss box (`Box.Url`, `Box.WebUrl` 192.168.3.1), used with `--config probe-ch.json`. The same password. Output in `probe-output-ch/`. |
| `/etc/conf.d/amazon-ssm-agent` | written on amazon2 | `supervisor=supervise-daemon`, `respawn_delay=30`, `respawn_max=0`. Restarts the SSM agent when it exits. It exited on 2026-10-26 when it could not get an IMDSv2 token, and stayed away until it was started by hand. Set on 2026-10-27. |

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
| 5 | `subscribe 120 <path>...` | One subscription per path (the box takes one path each, without wildcards, 32 at most). `--type ObjectCreation` and the like for other kinds. Switch an outlet by hand meanwhile: each change prints a line. |
| 6 | `get <path>`, `set <path> <value>` | Reading and switching one value |
| 7 | `delete <path>...` | Removes objects, for example subscriptions a run left behind (`get Device.LocalAgent.Subscription.` lists them) |
| 8 | `unregister` | Only to remove the controller: it stays for now (plan, D9) |

Commands that do not need the broker, only the box's web interface (`Box.WebUrl`, default `http://192.168.44.11`):

| Command | What |
|---|---|
| `aha <query>` | The raw AHA answer, for example `aha switchcmd=getdevicelistinfos` |
| `rest <path>` | The raw REST answer, for example `rest smarthome/overview` |
| `smarthome` | The devices as `FritzBoxSmartHomeService` hands them to the HA server, also as `devices.json` |
| `smarthome <ain> on\|off` | Switches a unit first. `level <percent>`, `kelvin <K>` and `color <hue> <percent>` work the same way. Setting level or color does not switch a lamp on. |
| `routine <uid> on\|off` | Switches a routine of the box on or off |

Every run connects with an MQTT client ID of its own, so runs can overlap. The results go to
`/opt/fritzbox-usp-probe/probe-output/<time>-<command>/`. `probe-output/agent.json` remembers the
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
