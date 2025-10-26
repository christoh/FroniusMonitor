# Builder (cannot use Alpine)
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0.100-rc.1 AS builder

WORKDIR /build/FroniusMonitor

RUN apt-get update -y
RUN apt-get upgrade -y
RUN apt-get install -y --no-install-recommends python3
RUN dotnet workload install wasm-tools

COPY . .
RUN ls -laF .
WORKDIR ./HomeAutomationClient/HomeAutomationClient.Browser
RUN rm -fr app bin obj
RUN mkdir -p /build/root/app
RUN dotnet publish -c Release -o /build/root/app

WORKDIR /build/root
RUN chmod -R 755 app/wwwroot

COPY <<"EOF" ./root/.profile
#!/bin/ash
echo ""
echo Alpine Linux $(cat /etc/alpine-release)
test -f /etc/motd && cat /etc/motd
fastfetch
echo ""
cd
export ENV=~/.ashrc
EOF

COPY <<"EOF" ./root/.ashrc
#!/bin/ash

alias ll='ls -laF'
alias ps='ps -o pid,ppid,user,tty,nice,vsz,rss,time,args -T'

if [ `id -u` == 0 ]; then
    export PS1='[\[\033[01;31m\]\u@docker:home-automation-client\[\033[00m\] \[\033[01;34m\]\w\[\033[0m\]] \$ '
else
    export PS1='[\[\033[01;32m\]\u@docker:home-automation-client\[\033[00m\] \[\033[01;34m\]\w\[\033[0m\]] \$ '
fi

case "$TERM" in
xterm*|rxvt*)
    export PS1="\[\e]0;\u@\docker:home-automation-client: \w\a\]$PS1"
    ;;
*)
    ;;
esac
EOF

COPY <<"EOF" ./etc/motd
 _   _                           _         _                        _   _
| | | | ___  _ __ ___   ___     / \  _   _| |_ ___  _ __ ___   __ _| |_(_) ___  _ __
| |_| |/ _ \| '_ ` _ \ / _ \   / _ \| | | | __/ _ \| '_ ` _ \ / _` | __| |/ _ \| '_ \
|  _  | (_) | | | | | |  __/  / ___ \ |_| | || (_) | | | | | | (_| | |_| | (_) | | | |
|_| |_|\___/|_| |_| |_|\___| /_/   \_\__,_|\__\___/|_| |_| |_|\__,_|\__|_|\___/|_| |_|
  ____            _             _    ____           _
 / ___|___  _ __ | |_ _ __ ___ | |  / ___|___ _ __ | |_ ___ _ __
| |   / _ \| '_ \| __| '__/ _ \| | | |   / _ \ '_ \| __/ _ \ '__|
| |__| (_) | | | | |_| | | (_) | | | |__|  __/ | | | ||  __/ |
 \____\___/|_| |_|\__|_|  \___/|_|  \____\___|_| |_|\__\___|_|


Welcome to the Home Automation Control Center Browser Client
============================================================

EOF

COPY <<"EOF" ./etc/nginx/http.d/default.conf
server {

    error_log /dev/stdout warn;
    access_log /dev/stdout main;
    root /app/wwwroot;
    index index.html;

    location / {
        try_files $uri $uri/ =404;
        error_page 404 =200 /;
        brotli_static on;
        gzip_static on;
    }

    listen 42741;
    listen [::]:42741;
}
EOF

COPY <<"EOF" ./startup.sh
#!/bin/ash
test -f /etc/motd && cat /etc/motd
exec nginx -g 'daemon off;'
EOF

RUN chmod 744 ./startup.sh

COPY <<"EOF" ./root/.config/fastfetch/config.jsonc
{
  "$schema": "https://github.com/fastfetch-cli/fastfetch/raw/dev/doc/json_schema.json",
  "modules": [
    "title",
    "separator",
    "os",
    "host",
    "kernel",
    "uptime",
    "packages",
    "shell",
    "display",
    "wmtheme",
    "theme",
    "font",
    {
       "type": "cpu",
       "showPeCoreCount": true,
       "temp": true
    },
    "CPUCache",
    "gpu",
        "OpenGL",
        "OpenCl",
    "memory",
        "PhysicalMemory",
    "swap",
    "disk",
        "PhysicalDisk",
        "Uptime",
        "LoadAvg",
        "TPM",
        {
            "type": "localip",
            "showAllIps": false,
            "showIpv6": true,
            "format": "{ipv6} [v6] {ipv4} [v4]"
        },
        "PublicIp",
    "battery",
    "poweradapter",
    "locale",
    "break",
    "colors"
  ]
}
EOF

# Runner

FROM alpine:latest

RUN apk --no-cache update && \
    apk --no-cache upgrade

RUN apk --no-cache add nginx nginx-mod-http-brotli
RUN apk --no-cache add fastfetch curl

WORKDIR /
COPY --from=builder /build/root /
EXPOSE 42741

CMD ["/startup.sh"]
HEALTHCHECK CMD curl http://127.0.0.1:42741 -sD - | fgrep "<title>Home Automation Control Center</title>" || exit 1

