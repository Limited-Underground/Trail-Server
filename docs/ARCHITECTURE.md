# Trail Server Architecture

## Status

This document records provisional Server Option V0. It is an implementation direction, not production or compatibility evidence.

## System boundary

```text
Trail radio network
        |
Dedicated Trail-compatible server-radio device
        | USB
        v
Linux Trail Server
        |
        +-- ASP.NET Core server and administration UI
        +-- PostgreSQL / PostGIS
        +-- persistent receive and transmit queues
        +-- Caddy LAN web and large-file service
        +-- MapLibre / PMTiles presentation
```

The server host does not implement the LoRa physical interface itself. Its dedicated Trail-compatible radio device participates in the radio network and exposes a bounded local USB contract.

That local boundary is now frozen as
[LUSR/1](contracts/server-radio-usb-v1.md). LUSR/1 treats the on-air payload as
opaque and defines only host/device framing, identity, credits, idempotency,
receive acknowledgement, restart reconciliation, and privacy-safe errors.

## Linux appliance

The first functional host profile uses Debian 13.6.0 `amd64`, installed from
the exact netinst image recorded in Decision 0002 and then updated from Debian
stable/security repositories. The installation selects no desktop environment.

LightDM autologs into a locked `trailkiosk` account running Openbox and
Chromium at the loopback Caddy entry point. The same browser-based
administration interface is available to authorized machines over the local
network by the server's reserved IPv4 address. The first functional server may
use plain HTTP on the trusted LAN. Publicly trusted HTTPS is not an initial
acceptance gate.

The host uses DHCP with a router-side reservation. Its default-deny firewall
permits SSH and HTTP from only the configured trusted IPv4 LAN. PostgreSQL and
internal service ports remain local to the server. Containers must not publish
internal ports to all host interfaces.

TS-004 Phase A runs the ASP.NET Core service in a private Docker network and
publishes it only on host loopback. Caddy is the sole accepted LAN entry point.
The application deployment couples Docker restart ordering to nftables restart
so Docker reconstructs its private network rules after the firewall is rebuilt.
The API rejects an operational claim. TS-004 Phase B adds a transport-neutral
LUSR/1 background worker that is disabled in deployed configuration. Phase C
adds an opt-in Linux serial byte-stream implementation with explicit baud-rate
configuration, fixed 8-N-1 framing, no flow control, and an ownership wrapper
that closes the serial connection with the stream. Configuration fails closed
unless the process-visible path uses the stable `/dev/serial/by-id/` namespace;
the actual device identity remains private deployment configuration. The
published Compose profile still disables the bridge and grants no device
mapping or elevated authority. The worker sends no transmit requests and
refuses to acknowledge received packets before TS-005 supplies durable storage.

## Reused components

| Concern | Current option | Reuse boundary |
| --- | --- | --- |
| Host operating system | Debian 13.6.0 `amd64` installation seed | Minimal maintained base, systemd supervision, security updates, recovery seed, and local kiosk packages |
| Server application | ASP.NET Core | Web APIs, background services, health checks, configuration, authentication, and administration UI foundation |
| Persistent data | PostgreSQL | Relational records, queues, audit history, and migrations |
| Geographic data | PostGIS | Locations, tracks, proximity, spatial indexing, and later geofencing |
| Web entry point | Caddy | LAN reverse proxy, static files, HTTP range requests, and later TLS |
| Maps | MapLibre and PMTiles | Browser rendering and packaged basemaps |
| Deployment | Docker Compose plus Linux service supervision | Repeatable services while preserving reliable USB-device handling |

## OpenTrail-specific work

The project must implement the parts that cannot be safely delegated to a generic platform:

- the server-radio firmware role;
- the bounded USB host/device contract;
- radio receive and transmit scheduling;
- persistent delivery, acknowledgement, retry, expiry, and recovery behavior;
- Trail-specific message, location, alert, device, and server semantics;
- large-file manifests and compatibility rules;
- operator workflows and evidence-backed acceptance.

## Large files

The server may announce a compact file manifest through an applicable Trail message. The manifest can identify a file, version, size, digest, compatibility boundary, and download location. The file bytes are delivered by an explicitly supported IP path rather than transported through the LoRa network.

## Deferred choices

- exact server-radio hardware after supported-device evidence exists;
- exact USB class, device discovery, and production device identity;
- whether a future container mapping retains the host stable path or exposes a
  neutral process-visible alias after Linux USB reliability testing;
- containerized versus native radio bridge after Linux USB reliability testing;
- local-disk versus S3-compatible file storage after actual scale is known;
- trusted LAN HTTPS;
- multiple geographically separated server-radio gateways and any resulting internal message bus.
