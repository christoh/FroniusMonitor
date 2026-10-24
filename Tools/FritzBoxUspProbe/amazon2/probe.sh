#!/bin/sh
# /opt/fritzbox-usp-probe/probe.sh on amazon2.hochstaetter.de. Runs the probe (published to ./app) next to
# Mosquitto. Passwords come from /opt/mosquitto/secrets.env and ./secrets.env, both readable by root only.
cd /opt/fritzbox-usp-probe
set -a; . /opt/mosquitto/secrets.env; . ./secrets.env; set +a
exec docker run --rm --network host -e FRITZBOX_PASSWORD -e MQTT_PASSWORD -e MQTT_BOX_PASSWORD \
  -v /opt/fritzbox-usp-probe:/work -w /work mcr.microsoft.com/dotnet/runtime:10.0-alpine dotnet /work/app/FritzBoxUspProbe.dll "$@"
