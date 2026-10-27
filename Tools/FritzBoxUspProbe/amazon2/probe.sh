#!/bin/sh
# /opt/fritzbox-usp-probe/probe.sh on amazon2.hochstaetter.de. Runs the probe (published to ./app). Passwords come
# from ./secrets.env, readable by root only. /opt/mosquitto/secrets.env held the broker's until the broker was removed
# on 2026-10-27, and is read only if it is there.
cd /opt/fritzbox-usp-probe
set -a; [ -f /opt/mosquitto/secrets.env ] && . /opt/mosquitto/secrets.env; . ./secrets.env; set +a
exec docker run --rm --network host -e FRITZBOX_PASSWORD -e MQTT_PASSWORD -e MQTT_BOX_PASSWORD \
  -v /opt/fritzbox-usp-probe:/work -w /work mcr.microsoft.com/dotnet/runtime:10.0-alpine dotnet /work/app/FritzBoxUspProbe.dll "$@"
