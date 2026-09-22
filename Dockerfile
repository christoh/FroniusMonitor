# Both images, the server (target "server") and the stand-alone browser client (target "client"), in one file, so
# the browser client is published once and used by both: the server hosts it at "/", the client image serves it
# with nginx. docker-bake.hcl builds the two targets from this file; BuildKit solves the stage "client-builder",
# which is the same in both of them, only once. Build a single image with
#
#   docker buildx bake server --load          (or client), see docker-bake.hcl
#   docker build --target client .            a plain "docker build ." builds the server, the last stage
#
# The context is the repository root. .dockerignore keeps the bin and obj folders of a local build out of it.


# ----- Shell files -----

# What both runtime images have in common for someone who opens a shell in them. Per image: /root/.ashrc (prompt,
# aliases) and /etc/motd, which name the image.
FROM scratch AS shell-files

COPY <<"EOF" /root/.profile
#!/bin/ash
echo ""
echo Alpine Linux $(cat /etc/alpine-release)
test -f /etc/motd && cat /etc/motd
fastfetch
echo ""
cd
export ENV=~/.ashrc
EOF

COPY <<"EOF" /root/.config/fastfetch/config.jsonc
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


# ----- Client builder -----

# Not Alpine here (unlike the runtime stages): publishing the browser client needs the wasm-tools workload, which
# wants python3 and does not target musl. --platform=$BUILDPLATFORM makes this the build machine's native platform
# whatever image and platform is being built - the WebAssembly output is the same for all of them - so this stage
# runs once per build, not once per platform, and not once per image either. Keep it that way: an ARG such as
# TARGETPLATFORM used in here would make it differ between platforms and build it once for each.
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS client-builder

# Before COPY, so that a source change does not install the workload again.
RUN apt-get update -y && \
    apt-get upgrade -y && \
    apt-get install -y --no-install-recommends python3 && \
    dotnet workload install wasm-tools

WORKDIR /build
COPY . .

WORKDIR /build/HomeAutomationClient/HomeAutomationClient.Browser
RUN dotnet publish -c Release -o /client


# ----- Client runner -----

FROM alpine:latest AS client

# The OCI image labels GitHub reads: source attaches the package to this repository (and so inherits its
# visibility and README), description and licenses show on the package page. In the runner stage on purpose -
# a label in a builder stage never reaches the pushed image. For a multi-arch push these labels sit on each
# platform's manifest; the index (manifest list) itself gets the same values from docker-bake.hcl, which is how
# the images are built and pushed.
LABEL org.opencontainers.image.title="Home Automation Control Center (browser client)" \
      org.opencontainers.image.description="The browser client of the Home Automation Control Center, an Avalonia WebAssembly app served by nginx." \
      org.opencontainers.image.source="https://github.com/christoh/FroniusMonitor" \
      org.opencontainers.image.url="https://github.com/christoh/FroniusMonitor" \
      org.opencontainers.image.documentation="https://github.com/christoh/FroniusMonitor/blob/master/README.md" \
      org.opencontainers.image.licenses="AGPL-3.0-only" \
      org.opencontainers.image.vendor="Christoph Hochstätter" \
      org.opencontainers.image.authors="Christoph Hochstätter" \
      org.opencontainers.image.base.name="docker.io/library/alpine:latest"

RUN apk --no-cache update && \
    apk --no-cache upgrade

RUN apk --no-cache add nginx nginx-mod-http-brotli fastfetch curl

WORKDIR /
COPY --from=shell-files / /
# The same line as in the server runner, character for character, which makes it one and the same layer in both
# images: --link builds the layer on its own rather than on top of this image, so what lies below it does not
# enter its content. Change one, change the other. Only wwwroot is copied; nginx serves nothing else of the client.
COPY --link --from=client-builder /client/wwwroot /app/wwwroot

COPY <<"EOF" /root/.ashrc
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

COPY <<"EOF" /etc/motd
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

COPY <<"EOF" /etc/nginx/http.d/default.conf
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

COPY --chmod=744 <<"EOF" /startup.sh
#!/bin/ash
test -f /etc/motd && cat /etc/motd
exec nginx -g 'daemon off;'
EOF

EXPOSE 42741

CMD ["/startup.sh"]
HEALTHCHECK CMD curl http://127.0.0.1:42741 -sD - | fgrep "<title>Home Automation Control Center</title>" || exit 1


# ----- Server builder -----

# The SDK image for the same reason as the client builder, but without the wasm-tools workload: the browser client
# is not built here, it is taken ready-made from the client builder.
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS server-builder

WORKDIR /build
COPY . .

WORKDIR /build/HomeAutomationServer

RUN mkdir -p /runner/app && \
    # The price and weather history and the Solar.web chart cache; mount this folder to keep them across containers (see docker-compose.yml).
    mkdir -p /runner/app/history && \
    mkdir -p /runner/root && \
    mkdir -p /runner/home/app

# Compiled before the client is copied in, so that this runs while the client builder is still busy.
RUN dotnet build -c Release -v=m

# PublishClientApp=true copies the client's static web assets manifest in here, renamed so that Program.cs finds it
# and serves the client at "/" - see the matching targets in HomeAutomationServer.csproj. ClientAppPrePublished=true
# tells them the client is already published in ClientPublishDir, so they do not publish it a second time, and
# CopyClientWwwRoot=false leaves its wwwroot out: the server runner adds that as a layer of its own, shared with the
# client image. A read-only bind mount rather than a COPY: the files are only read here, and a COPY would add a
# layer holding the whole client once more.
RUN --mount=type=bind,from=client-builder,source=/client,target=/client \
    dotnet publish -c Release -v=m --no-build -p:PublishClientApp=true -p:ClientAppPrePublished=true -p:CopyClientWwwRoot=false -p:ClientPublishDir=/client/ -o /runner/app

WORKDIR /runner

COPY <<"EOF" ./root/.ashrc
#!/bin/ash

alias ll='ls -laF'
alias sudo='doas'
alias ps='ps -o pid,ppid,user,tty,nice,vsz,rss,time,args -T'
alias root='exec doas ash -li'

if [ `id -u` == 0 ]; then
    export PS1='[\[\033[01;31m\]\u@docker:home-automation\[\033[00m\] \[\033[01;34m\]\w\[\033[0m\]] \$ '
else
    export PS1='[\[\033[01;32m\]\u@docker:home-automation\[\033[00m\] \[\033[01;34m\]\w\[\033[0m\]] \$ '
    echo -e "\033[01;34mIf you need root access (neccessary for nothing), type 'root'\033[01;0m\n"
fi

case "$TERM" in
xterm*|rxvt*)
    export PS1="\[\e]0;\u@\docker:home-automation: \w\a\]$PS1"
    ;;
*)
    ;;
esac
EOF

COPY <<"EOF" ./etc/motd

                                _         _                        _   _             
  /\  /\___  _ __ ___   ___    /_\  _   _| |_ ___  _ __ ___   __ _| |_(_) ___  _ __  
 / /_/ / _ \| '_ ` _ \ / _ \  //_\\| | | | __/ _ \| '_ ` _ \ / _` | __| |/ _ \| '_ \ 
/ __  / (_) | | | | | |  __/ /  _  \ |_| | || (_) | | | | | | (_| | |_| | (_) | | | |
\/ /_/ \___/|_| |_| |_|\___| \_/ \_/\__,_|\__\___/|_| |_| |_|\__,_|\__|_|\___/|_| |_|

Welcome to Home Automation Server
=================================

EOF

COPY <<"EOF" ./startup.sh
#!/bin/ash
test -f /etc/motd && cat /etc/motd
export DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
cd /app
exec dotnet ./HomeAutomationServer.dll
EOF

#COPY <<"EOF" ./etc/sudoers
#app ALL=(ALL:ALL) NOPASSWD: ALL
#EOF

COPY <<"EOF" ./etc/doas.conf
permit nopass app as root
EOF


# ----- Server runner -----

# Last on purpose, so that a plain "docker build ." builds the server, as it did before the two Dockerfiles merged.
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS server

# The OCI image labels GitHub reads: source attaches the package to this repository (and so inherits its
# visibility and README), description and licenses show on the package page. In the runner stage on purpose -
# a label in a builder stage never reaches the pushed image. For a multi-arch push these labels sit on each
# platform's manifest; the index (manifest list) itself gets the same values from docker-bake.hcl, which is how
# the images are built and pushed.
LABEL org.opencontainers.image.title="Home Automation Server" \
      org.opencontainers.image.description="Collects data from Fronius GEN24 inverters, Fronius Wattpilot chargers, AVM FRITZ! devices and Toshiba air conditioners, and serves it to the Home Automation Control Center clients." \
      org.opencontainers.image.source="https://github.com/christoh/FroniusMonitor" \
      org.opencontainers.image.url="https://github.com/christoh/FroniusMonitor" \
      org.opencontainers.image.documentation="https://github.com/christoh/FroniusMonitor/blob/master/README.md" \
      org.opencontainers.image.licenses="AGPL-3.0-only" \
      org.opencontainers.image.vendor="Christoph Hochstätter" \
      org.opencontainers.image.authors="Christoph Hochstätter" \
      org.opencontainers.image.base.name="mcr.microsoft.com/dotnet/aspnet:10.0-alpine"
COPY --from=shell-files / /
COPY --from=server-builder /runner/ /
# The browser client, as the very same layer the client image has - see the matching line there.
COPY --link --from=client-builder /client/wwwroot /app/wwwroot

RUN apk --no-cache update &&\
    apk --no-cache upgrade &&\
    # tzdata: the EnergyData section of Settings.xml names a time zone such as Europe/Berlin, which needs the zone files.
    apk --no-cache add doas fastfetch alpine-release icu-libs curl tzdata &&\
    unset DOTNET_SYSTEM_GLOBALIZATION_INVARIANT &&\
    ln -s /root/.ashrc /home/app/.ashrc &&\
    ln -s /root/.profile /home/app/.profile &&\
    chmod 755 /root /startup.sh /root/.ashrc /root/.profile &&\
    chown -R app:app /home/app /app/history

WORKDIR /home/app
USER app
EXPOSE 1502/tcp
# The web UI/API, and now the browser client hosted alongside it (see Program.cs and docker-compose.yml). This is
# the base image's own default Kestrel HTTP port (ASPNETCORE_HTTP_PORTS=8080), not something set here.
EXPOSE 8080/tcp

ENTRYPOINT [ "/bin/ash", "-c" ]
CMD [ "/startup.sh" ]

