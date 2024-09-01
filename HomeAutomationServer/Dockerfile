FROM localhost:5000/lse-aspnetcore-sdk-80-bookworm AS builder

WORKDIR /FroniusMonitor

RUN apt-get update && apt-get -y full-upgrade
RUN dotnet workload update

COPY . .
WORKDIR /FroniusMonitor/HomeAutomationServer

RUN rm -fr /FroniusMonitor/Fronius/bin /FroniusMonitor/Fronius/obj bin obj
RUN dotnet build -c Release -v=m


FROM localhost:5000/lse-aspnetcore-runtime-80-bookworm

RUN apt-get update && apt-get install -y procps && apt-get -y full-upgrade
WORKDIR /app

COPY --from=builder /FroniusMonitor/HomeAutomationServer/bin/Release/net8.0/ .

USER root

EXPOSE 502/tcp

ENTRYPOINT [ "/bin/bash", "-c", "/app/HomeAutomationServer" ]
