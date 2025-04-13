FROM alpine:latest

RUN apk --no-cache update && \
    apk --no-cache upgrade && \
    apk --no-cache add nginx nginx-mod-http-brotli fastfetch 

WORKDIR /

COPY app/wwwroot /app

RUN chmod -R 755 /app && \
    mkdir -p ./root

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
        root /app;
        index index.html;

        location / {
                try_files $uri $uri/ =404;
                error_page 404 =200 /;
                brotli_static on;
        }

	listen 42741;
	listen [::]:42741;
}
EOF

EXPOSE 42741

CMD ["nginx", "-g", "daemon off;"]
