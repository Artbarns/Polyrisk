# Oracle Always Free — single-instance PolyRisk

Prepared configuration, not an existing deployment. No Oracle account or VM is connected yet.
The bundle preserves the reviewed WebGL export and the existing .NET10 server unchanged.

## Capacity and cost boundary

Target one `VM.Standard.A1.Flex` ARM64 machine, **2 OCPU / 12GB RAM**, Ubuntu24.04 LTS,
50GB boot storage, in the account's home region. Verify the actual Always Free label and
account allowance before provisioning. The currently documented monthly allowance is
1,500 OCPU-hours and 9,000GB-hours, across all A1 instances in the account. Two OCPU and
12GB continuously for a 31-day month use 1,488 OCPU-hours / 8,928GB-hours.
The boot disk counts toward the account's 200GB combined free block storage allowance.

Choose a nearby European home region if available. A1 capacity is not guaranteed;
do not silently select a paid shape, change to Pay As You Go, or create a paid load balancer.
Oracle may reclaim idle free machines. Do not generate artificial activity to avoid that policy.
Backups on the same VM do not protect against loss of the VM: export private backups off-host.

The rendering and most game rules run on the players' devices. This VM serves assets and
six-player HTTP sessions; its size is a reasonable initial target, not a measured guarantee.
One app instance is mandatory for the JSON store. No second replica or database migration.

## Account and network prerequisites

Create/sign in to the account yourself. Keep passwords, MFA, card details and private SSH keys
out of chat and Git. Account terms/payment verification require your own completion.
No extra Oracle integration was found in the available plugin search; access can use an
authorized SSH connection after the machine exists.

Use the SSH public key intended for this deployment and a public subnet with Internet routing.
Allow inbound TCP22 **only from your administration IP**, and TCP80/443 publicly in the OCI
security rules and the VM firewall. Keep backend8080 closed. Do not disable the firewall.
Use a DNS hostname whose A record points to the VM's actual public IP; leave no stale AAAA.
A DNS name is separate from the free VM. If no domain is owned, select an appropriate free
DNS hostname before attempting public TLS; no hostname has been registered or promised here.

Install Docker Engine and Compose from the official Ubuntu repository procedure, checking
the actual distribution/architecture first. Do not install a Mac Docker VM for this workflow.
The Dockerfile builds natively on the Oracle machine; the macOS .NET binary is not uploaded.

## Prepare and transfer

From the Unity repository on the development Mac:

```sh
python3 Server/Deployment/prepare_release.py --webgl Builds/PremiumMasterpieceRelease --output Builds/OracleReleaseInput_20261024
python3 Server/Deployment/Oracle/prepare.py --bundle Builds/OracleReleaseInput_20261024 --output Builds/OraclePremiumMasterpiece
```

The output must not already exist. Every release hash is verified before copying; extra files
and symlinks are rejected. Generated `backend/obj` and `backend/bin` caches are ignored and
never copied. Only manifest-listed files enter the release context. The original reviewed release
and its archive remain intact.
Transfer the prepared directory to a private application directory on the VM, e.g.
`/opt/polyrisk`. Do not serve that directory directly with a web server.

On the Oracle VM, from that directory:

```sh
cp .env.example .env
# Set the actual POLYRISK_DOMAIN in .env before continuing.
chmod 600 .env
sudo docker compose config --quiet
sudo docker compose build --pull
sudo docker compose up -d
sudo docker compose ps
```

The app is reachable only by Caddy on the Docker network. Caddy handles public HTTPS and
renews certificates; its certificate volumes persist across restarts. The game and API share
the same origin. Do not enable backend `POLYRISK_REQUIRE_HTTPS` behind this proxy.
The release tag must be new on each upgrade so that a previous local image remains available.
Record the resolved base-image/proxy digests and `docker compose images` during the first build;
the prepared tags alone are not a reproducibility guarantee.

Bounded application/proxy logs use Docker's json-file rotation. Request access logging is
not enabled; reconnect credentials are never added to deployment logs. Logs and state live
on the VM. No credentials, certificates or runtime state belong in the public source repo.

## Backup and recovery

The app's bind mount `./data` survives container replacement. From the VM deployment directory:

```sh
sudo python3 backup.py
```

This copies an intact JSON snapshot into a new owner-private archive under `./backups`.
Transfer selected backups to a private off-host destination. No recurring job is installed
by the preparation script; select backup cadence after the real deployment is verified.
The server intentionally expires rooms inactive for two hours; persistence does not change that rule.

For recovery, stop the app before restoring a selected verified snapshot to
`data/lobbies.json`; retain the replaced file and confirm container-user ownership.
Never restore over an active writer. Never remove data or Caddy volumes as part of an upgrade.

## Required live verification

1. Confirm the image builds/runs on ARM64 and the actual OCI resources are free-eligible.
2. Verify trusted HTTPS, playable menu, MIME/compression/cache for the actual Unity export,
   and denial of source/private state paths. Test the backend only via the public origin.
3. Run the existing `smoke_test.py` against that HTTPS origin in an isolated QA room.
4. Use two rendered browser clients, then six protocol clients at the real polling cadence.
   Record HTTP latency/errors, VM CPU/memory, and actual device frame times separately.
5. Verify reconnect and server restart retain a non-expired QA room. Verify backup recovery
   on an isolated copy before relying on it.
6. Test iPhone Safari, touch and a remote Internet connection. A local Mac test is not proof
   of ARM64 hosting, Internet latency, or iPhone graphics performance.

After a verified remote deployment, the development Mac can be off. Until then, only the
gallery is public; the playable multiplayer origin remains local.

References: [Oracle free resources](https://docs.oracle.com/en-us/iaas/Content/FreeTier/freetier_topic-Always_Free_Resources.htm),
[Docker Ubuntu installation](https://docs.docker.com/engine/install/ubuntu/),
[.NET ARM64 images](https://github.com/dotnet/dotnet-docker/blob/main/README.aspnet.md),
[Caddy HTTPS](https://caddyserver.com/docs/automatic-https).
