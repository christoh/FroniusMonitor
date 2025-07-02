# ----- Builder -----

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS builder

WORKDIR /build

COPY . .

WORKDIR /build/HomeAutomationServer

RUN apk --no-cache update && \
    apk --no-cache upgrade && \
    #dotnet workload update && \
    rm -fr /build/Fronius/bin /build/Fronius/obj bin obj && \
    mkdir -p /runner/app && \
    mkdir -p /runner/root && \
    mkdir -p /runner/home/app

RUN dotnet publish -c Release -v=m -o /runner/app

WORKDIR /runner

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


# ----- Runner -----

FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine
COPY --from=builder /runner/ /

RUN apk --no-cache update &&\
    apk --no-cache upgrade &&\
    apk --no-cache add doas fastfetch alpine-release icu-libs curl &&\
    unset DOTNET_SYSTEM_GLOBALIZATION_INVARIANT &&\
    ln -s /root/.ashrc /home/app/.ashrc &&\
    ln -s /root/.profile /home/app/.profile &&\
    chmod 755 /root /startup.sh /root/.ashrc /root/.profile &&\
    chown -R app:app /home/app

WORKDIR /home/app
USER app
EXPOSE 1502/tcp

ENTRYPOINT [ "/bin/ash", "-c" ]
CMD [ "/startup.sh" ]

