# PolyRisk Lobby Server

Minimal ASP.NET Core lobby service for the first online multiplayer slice. It provides:

- six-character private game codes;
- two to six player seats;
- reconnect credentials;
- ready state and host-only start;
- host migration when the host leaves;
- lobby snapshots with monotonic versions;
- authenticated, ordered game commands with optimistic state-version checks;
- host-only command consumption and authoritative state publication;
- reconnectable game-state snapshots with bounded payload sizes;
- atomic on-disk persistence for lobbies, pending commands and game snapshots;
- SHA-256 storage of reconnect credentials (the bearer token is never written to disk);
- protocol command allow-list and JSON structure validation;
- local-only CORS by default, configurable production origins and request rate limiting;
- structured ASP.NET logs without credentials;
- automatic cleanup of lobbies inactive for two hours.

## Run locally

```bash
dotnet run --configuration Release
```

The default address is `http://127.0.0.1:5080`. Override it with `POLYRISK_URLS`.
Runtime state is written to `bin/.../data/lobbies.json` by default. Override this with
`POLYRISK_DATA_PATH`, pointing it at a persistent volume in production.

Production configuration:

```bash
POLYRISK_ALLOWED_ORIGINS=https://play.example.com
POLYRISK_REQUIRE_HTTPS=true
POLYRISK_DATA_PATH=/var/lib/polyrisk/lobbies.json
AllowedHosts=lobby.example.com
```

`POLYRISK_ALLOWED_ORIGINS` accepts a comma-separated list. When omitted, only localhost
and loopback browser origins are accepted. TLS can terminate in ASP.NET or at a trusted
reverse proxy; set `POLYRISK_REQUIRE_HTTPS=true` when ASP.NET is responsible for redirecting
plain HTTP.

## Validate

Build once, then run the six-player smoke test on its own isolated server and
temporary data by default. Pass a URL only when intentionally testing an
already-running local or remote service:

```bash
dotnet build --configuration Release
python3 smoke_test.py
python3 smoke_test.py https://lobby.example.com
python3 persistence_test.py
python3 six_player_reconnect_test.py
```

The smoke test creates a full six-player lobby and verifies capacity, authentication,
ready state, host permissions, launch, late-join rejection and host migration. It then
submits a player command, consumes it as the host, publishes the next authoritative
snapshot, reads that snapshot from another seat and rejects a stale command.
The restart test stops the server twice and proves that credentials still authenticate,
the pending command queue survives, acknowledged commands disappear and the authoritative
game state is restored. It also verifies that raw reconnect tokens are absent from disk.
The six-player reconnect test repeats recovery with a full lobby, checks that all six
sessions receive the restored snapshot after a process restart, then verifies pending
command delivery and state publication after host handoff. These scripts exercise the
HTTP protocol and persistence layer; they do not replace a six-browser Unity/WebGL test.

## Current boundary

The included atomic JSON store is appropriate for the initial private-game service on one
server instance. A multi-instance deployment should replace it with shared transactional
storage. Production must also supply its real origin, hostname, persistent volume and TLS
termination through the environment variables above.
The host currently remains the gameplay authority so PolyRisk can reuse its tested Unity
rules engine. Server-side rule validation is intentionally deferred until the complete
turn command set and snapshot schema are stable.
