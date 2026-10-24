#!/bin/sh
# /opt/mosquitto/update-cert.sh on amazon2.hochstaetter.de. Copies the Let's Encrypt certificate of
# amazon2.hochstaetter.de, which certbot in nginx.kartellvz renews, for Mosquitto, and restarts Mosquitto if it
# changed. Run daily from /etc/periodic/daily/mosquitto-cert.
set -e
SRC=/var/lib/docker/volumes/kartellvz_letsencrypt/_data/live/amazon2.hochstaetter.de
DST=/opt/mosquitto/certs
if ! cmp -s "$SRC/fullchain.pem" "$DST/fullchain.pem" || ! cmp -s "$SRC/privkey.pem" "$DST/privkey.pem"; then
  cp -L "$SRC/fullchain.pem" "$DST/fullchain.pem"
  cp -L "$SRC/privkey.pem" "$DST/privkey.pem"
  chown 1883:1883 "$DST/fullchain.pem" "$DST/privkey.pem"
  chmod 0644 "$DST/fullchain.pem"
  chmod 0600 "$DST/privkey.pem"
  [ "$1" = "--no-restart" ] || docker restart mosquitto >/dev/null
fi
