# ----- Builder -----

FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS builder

WORKDIR /build

COPY . .

WORKDIR /build/HomeAutomationServer

RUN apk --no-cache update && apk --no-cache upgrade && dotnet workload update && \
rm -fr /build/Fronius/bin /build/Fronius/obj bin obj && \
mkdir -p /runner/app && \
mkdir -p /runner/root && \
mkdir -p /runner/home/app

RUN dotnet publish -c Release -v=m -o /runner/app

WORKDIR /runner

COPY <<"EOF" ./root/.profile
#!/bin/ash
export ENV=~/.ashrc
EOF

COPY <<"EOF" ./root/.ashrc
#!/bin/ash
alias ll='ls -laF'
alias ps='ps -o pid,ppid,user,tty,nice,vsz,rss,time,args'
test -f /etc/motd && cat /etc/motd

if [ `id -u` == 0 ]; then
    export PS1='[\[\033[01;31m\]\u@docker:home-automation\[\033[00m\] \[\033[01;34m\]\w\[\033[0m\]] \$ '
else
    export PS1='[\[\033[01;32m\]\u@docker:home-automation\[\033[00m\] \[\033[01;34m\]\w\[\033[0m\]] \$ '
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

Welcome to Bettina and Christoph's Home Automation Server
=========================================================

EOF

COPY <<"EOF" ./startup.sh
#!/bin/ash
cat /etc/motd
cd /app
exec dotnet ./HomeAutomationServer.dll
EOF

# ----- Runner -----

FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine
COPY --from=builder /runner/ /

RUN <<"EOF"
apk --no-cache update && apk --no-cache upgrade
ln -s /root/.ashrc /home/app/.ashrc
ln -s /root/.profile /home/app/.profile
chmod 755 /root /startup.sh /root/.ashrc /root/.profile
chown -R app:app /home/app
#chown -R app:app /app
EOF

WORKDIR /home/app
USER app
EXPOSE 1502/tcp

ENTRYPOINT [ "/bin/ash", "-lc" ]
CMD [ "/startup.sh" ]

