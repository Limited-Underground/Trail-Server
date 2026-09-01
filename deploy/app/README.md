# Trail Server application deployment

This directory deploys the bounded TS-004 Phase A ASP.NET Core service to a
provisioned TS-002 host. It does not provide a packet database, live queue, or
operational Trail Server. Phase B includes a transport-neutral LUSR/1
background worker. Phase C adds an opt-in Linux serial implementation, but the
published deployment keeps it explicitly disabled and provides no device
mount, device identity, host group, or elevated authority. Production device
binding and permissions remain TS-008 work.

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

## Linux pseudo-terminal preflight

The repository includes a Linux-only integration gate for the serial bridge.
It builds and runs in a non-root, network-disabled, read-only Docker container
and uses kernel pseudo-terminals rather than a physical device:

    sudo ./tools/test-radio-bridge-linux.sh

A successful run ends with `RADIO_BRIDGE_LINUX_PTY_RESULT=PASS`. This checks
Linux `System.IO.Ports`, fragmented LUSR/1 negotiation, disconnect/reconnect,
fresh sessions, bounded blocked-read shutdown, disposal, and redacted errors.
It does not configure the production service or prove USB hardware, firmware,
RF, udev, electrical serial settings, or container device mapping.

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
