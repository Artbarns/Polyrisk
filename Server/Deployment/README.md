# Public single-service deployment

This package preserves the existing ASP.NET Core lobby/game HTTP protocol and Unity host authority.
The same service serves the verified WebGL export, so a clean browser automatically uses the game's
HTTPS origin for lobby requests. No tunnel to the developer's Mac is involved.

## Prepare the reviewed release

Run from the Unity repository, using the actual verified **Release** export:

```sh
python3 Server/Deployment/prepare_release.py --webgl /absolute/path/to/verified/WebGL --output /absolute/path/to/new/release-bundle
```

The output contains only backend sources, export directories, Dockerfile, entrypoint and a SHA-256
manifest. It excludes the export's galleries and other root files. Do not stage a development QA build
with validation controls enabled. The script refuses to overwrite a previous bundle.

## Oracle Always Free — selected initial target

The authorized initial target is one free-eligible Oracle A1 VM with a private persistent
data directory and a Caddy HTTPS proxy. See [Oracle preparation and live checks](Oracle/README.md).
The wrapper verifies the reviewed release before staging. It does not create an account,
provision resources, install software on this Mac, or claim an existing public deployment.
Free capacity and actual remote/device performance still require validation.

## Railway — managed paid alternative

Use an explicitly authorized Railway project/service. Deploy the **bundle directory**, not the Unity
repository. Railway detects its root Dockerfile. Attach one persistent volume at `/data`, run **one
replica**, and generate the service's public HTTPS domain. The existing JSON store does not support
concurrent replicas. Set:

```text
PORT=8080
POLYRISK_ALLOWED_ORIGINS=https://ACTUAL-ASSIGNED-PUBLIC-DOMAIN
AllowedHosts=ACTUAL-ASSIGNED-PUBLIC-DOMAIN
```

`POLYRISK_WEBGL_ROOT` and `POLYRISK_DATA_PATH` are supplied by the Docker image. TLS terminates at
Railway; do not enable `POLYRISK_REQUIRE_HTTPS` behind that proxy, which would otherwise redirect its
internal HTTP connection. The browser only receives/uses the public HTTPS origin. No WebSocket is used.
Do not set production allow-origins to `*`.

The deployed global request budget is 3600 requests/minute per connection IP, bounded by the server
configuration to 60–3600. Without that setting the existing default stays 900. Normal six-client idle
polling is approximately 780 requests/minute (host 0.25s, replicas 1s, membership 1.5s); the higher
deployment budget leaves room for actions, first-snapshot polling and proxy-shared connections while
keeping both global limits and the separate 30/minute lobby-entry limit active.

Railway volumes initially belong to root. The entrypoint gives the app ownership of its dedicated data
directory, then runs .NET as the image's unprivileged `app` user. No public endpoint exposes that volume.
There is no container engine on the current development Mac: the Docker image must be built and tested
by the host/CI before the container itself is considered validated.

## Required deployment verification

Verify public `/` renders the playable menu; Unity `.wasm` is `application/wasm`; `.data` is
`application/octet-stream`; explicit `.gz`/`.br` files receive the right Content-Encoding. Hashed
`/Build/` files are immutable, HTML is revalidated, source and `/data` are not public. Run the protocol
smoke against the actual HTTPS origin, then two browser clients, reload/reconnect, and a service restart
with the persistent volume before declaring public multiplayer ready. Keep test rooms isolated.

A successful local protocol test is not proof of iPhone Safari or cross-device multiplayer. Record
actual external/device evidence separately.

## Portability and a later hosting migration

The release has no Oracle-specific dependency: it uses a standard Linux .NET container,
static WebGL files, environment settings, and a private JSON data directory. Another Linux
VM or a container host with persistent storage can run it. Rebuild the image for the destination
architecture when moving between ARM64 and AMD64; do not copy a Mac binary to Linux.

For a planned migration, prepare the destination with the same reviewed release first. Then
pause the game service, take a final private backup, transfer the complete snapshot and restore
it with the correct file ownership. Keep the old server stopped while the new one can write.
The current store supports one writer; two live copies would diverge. Retain the old release
and a private backup until destination persistence and reconnect have been verified.

Move the existing DNS name to the destination if possible and reconfigure TLS, AllowedHosts
and allowed origins. Keeping the same public hostname also preserves the browser origin used
for saved reconnect credentials. Changing domains can require players to join again because
browser storage is origin-specific; a manually saved server URL may also need updating.
Do not promise an uninterrupted migration or session transfer until tested. Room expiry rules
still apply. Never publish snapshots, reconnect data, TLS keys or account credentials in Git.

This migrates the existing single-instance architecture. Running several backend replicas
would require shared transactional persistence and its own validation; changing provider alone
does not require that redesign.

References: [Microsoft static file middleware](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/static-files?view=aspnetcore-10.0),
[Railway Docker deployment](https://docs.railway.com/builds/dockerfiles),
[Railway persistent volumes](https://docs.railway.com/volumes).
