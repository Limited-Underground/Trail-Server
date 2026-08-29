# Trail Server application deployment

This directory deploys the bounded TS-004 Phase A ASP.NET Core service to a
provisioned TS-002 host. It does not provide a radio transport, packet
database, live queue, or operational Trail Server. Phase B includes a
transport-neutral LUSR/1 background worker, but deployed configuration keeps it
disabled and no serial/USB implementation or device mount exists.

## Install or update

From the repository root on the Debian host, run:

    sudo ./deploy/app/install-app.sh

The installer:

1. validates the versioned Caddy configuration;
2. installs a systemd relationship that restarts Docker after an nftables
   restart rebuilds the host firewall;
3. builds and starts the API with Docker Compose;
4. publishes the container only on host loopback at 127.0.0.1:5080;
5. exposes /api/health through Caddy on the accepted LAN HTTP port; and
6. runs verify-app.sh.

The hardened Caddy configuration disables its admin endpoint, so the installer
uses a validated bounded service restart rather than caddy reload.

## Recovery check

The Docker/nftables lifecycle check is meaningful only with the application
running:

    sudo systemctl restart nftables
    sleep 5
    sudo systemctl is-active nftables docker
    sudo ./deploy/app/verify-app.sh
    sudo /home/trailadmin/ts002-bootstrap/host/verify-host.sh

The application verifier must return TRAIL_SERVER_APP_RESULT=PASS, and the host
verifier must return HOST_PROFILE_RESULT=PASS. From a permitted LAN client,
port 80 must reach Caddy while direct port 5080 remains unreachable.
