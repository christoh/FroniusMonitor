# FritzBoxUspProbe

Phase 1.0 of `.claude/plans/FritzBoxUsp-plan.md`: finds out what the FRITZ!Box offers over USP (TR-369), and
whether it pushes changes of the smart-home devices. Delete this tool once its findings are in
`.claude/memory/FritzBox.Usp.md`.

The probe registers itself on the box as a USP controller (through TR-064) and then talks to the box over the MQTT
broker. It needs the house LAN, or the WireGuard tunnel into it.

## 1. The broker

On the docker host (192.168.44.50), with the volume `/home/christoh/docker/home-automation/mosquitto`:

```sh
V=/home/christoh/docker/home-automation/mosquitto
mkdir -p $V/config $V/data $V/log
cp mosquitto/mosquitto.conf mosquitto/acl $V/config/
# Asks for each password. The second call has no -c, or it would start the file anew.
docker run --rm -it -v $V:/mosquitto eclipse-mosquitto mosquitto_passwd -c /mosquitto/config/passwd fritzbox-neufahrn
docker run --rm -it -v $V:/mosquitto eclipse-mosquitto mosquitto_passwd /mosquitto/config/passwd usp-probe
sudo chown -R 1883:1883 $V && sudo chmod 0700 $V/config/passwd $V/config/acl
```

Then uncomment the `mosquitto` service in the docker-compose.yml, `docker compose up -d mosquitto`, and keep
`docker logs -f mosquitto` open: it shows the box connecting and what it subscribes to.

## 2. The box

- TR-064 on: "Zugriff für Apps erlauben" in the network settings (older FRITZ!OS: "Zugriff für Anwendungen
  zulassen").
- A box user with the right "FRITZ!Box Einstellungen". The probe uses it for TR-064.

## 3. The probe

```powershell
cd Tools\FritzBoxUspProbe
copy probe.sample.json probe.json   # and fill in Box.UserName
dotnet run -c Debug -- info
```

Passwords come from `FRITZBOX_PASSWORD` (box user), `MQTT_PASSWORD` (`usp-probe`) and `MQTT_BOX_PASSWORD`
(`fritzbox-neufahrn`), or are asked for. Asking needs a real console (PowerShell, Windows Terminal); in Git Bash set
the variables.

| Step | Command | What to look for |
|---|---|---|
| 1 | `info` | FRITZ!OS 8.01 or later, `X_AVM-DE_USPController` offered, the actions of `AddUSPController` |
| 2 | `register` | "Registered as controller n". The broker log shows `fritzbox-neufahrn` connecting and subscribing. |
| 3 | `listen 60` | Anything the box publishes |
| 4 | `explore` | Switches the controller off and on, waits for the box to introduce itself, reads the whole data model |
| 5 | `subscribe 120 <path>` | Switch an outlet by hand meanwhile. Each change should print a line. |
| 6 | `get <path>`, `set <path> <value>` | Reading and switching one value |
| 7 | `unregister` | When done, unless the controller is kept for the next round |

`explore` writes `summary.md` and the raw answers to `probe-output/<time>-explore/`. `probe-output/agent.json`
remembers the box's Endpoint ID and topic for the later commands; delete it if the box changed them.

`--verbose` shows the TR-064 calls and full stack traces.
