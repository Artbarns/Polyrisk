#!/bin/sh
set -eu
if [ "$(id -u)" = "0" ]; then
    mkdir -p /data
    chown app:app /data
    if [ -f /data/lobbies.json ]; then chown app:app /data/lobbies.json; fi
    exec gosu app dotnet /app/PolyRisk.LobbyServer.dll
fi
exec dotnet /app/PolyRisk.LobbyServer.dll
