FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS builder

WORKDIR /FroniusMonitor

RUN dotnet workload update

COPY . .
WORKDIR /FroniusMonitor/HomeAutomationServer

RUN rm -fr /FroniusMonitor/Fronius/bin /FroniusMonitor/Fronius/obj bin obj
RUN dotnet build -c Release -v=m


FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine

RUN apk --no-cache update && apk --no-cache upgrade
RUN echo "export ENV=~/.ashrc" >> /etc/profile

RUN echo "#!/bin/ash" > /root/.ashrc
RUN echo "alias ll='ls -laF'" >> /root/.ashrc
RUN ln -s /root/.ashrc /home/app/.ashrc
RUN chmod 755 /root

WORKDIR /app

COPY --from=builder /FroniusMonitor/HomeAutomationServer/bin/Release/net8.0/ .

USER root

EXPOSE 1502/tcp

ENTRYPOINT [ "/bin/ash", "-lc" ]
CMD [ "/app/HomeAutomationServer" ]
