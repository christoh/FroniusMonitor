# ----- Builder -----

FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS builder

RUN apk --no-cache update && apk --no-cache upgrade && dotnet workload update

WORKDIR /build

COPY . .

WORKDIR /build/HomeAutomationServer

RUN <<"EOF"
rm -fr /build/Fronius/bin /build/Fronius/obj bin obj
mkdir -p /runner/app
dotnet publish -c Release -v=m -o /runner/app
mkdir -p /runner/root
mkdir -p /runner/home/app
mkdir -p /runner
EOF

WORKDIR /runner

COPY <<"EOF" ./root/.profile
#!/bin/ash
export ENV=~/.ashrc
EOF

COPY <<"EOF" ./root/.ashrc
#!/bin/ash
alias ll='ls -laF'
test -f /etc/motd && cat /etc/motd
EOF

COPY <<"EOF" ./etc/motd

                                _         _                        _   _             
  /\  /\___  _ __ ___   ___    /_\  _   _| |_ ___  _ __ ___   __ _| |_(_) ___  _ __  
 / /_/ / _ \| '_ ` _ \ / _ \  //_\\| | | | __/ _ \| '_ ` _ \ / _` | __| |/ _ \| '_ \ 
/ __  / (_) | | | | | |  __/ /  _  \ |_| | || (_) | | | | | | (_| | |_| | (_) | | | |
\/ /_/ \___/|_| |_| |_|\___| \_/ \_/\__,_|\__\___/|_| |_| |_|\__,_|\__|_|\___/|_| |_|

Welcome to Christoph's Home Automation Server
=============================================

EOF

COPY <<"EOF" ./startup.sh
#!/bin/ash
cat /etc/motd
cd /app
exec /app/HomeAutomationServer
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

