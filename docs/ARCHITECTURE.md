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

## Linux appliance

The planned base is a minimal Linux installation. A lightweight graphical session launches the same browser-based administration interface that authorized machines can reach over the local network by server IP. The first functional server may use plain HTTP on the trusted LAN. Publicly trusted HTTPS is not an initial acceptance gate.

PostgreSQL and internal service ports remain local to the server. Only the administration and explicitly permitted file-delivery surfaces are exposed to the LAN.

## Reused components

| Concern | Current option | Reuse boundary |
| --- | --- | --- |
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

- exact Linux distribution and pinned version;
- exact server-radio hardware after supported-device evidence exists;
- USB framing and recovery contract;
- containerized versus native radio bridge after Linux USB reliability testing;
- local-disk versus S3-compatible file storage after actual scale is known;
- trusted LAN HTTPS;
- multiple geographically separated server-radio gateways and any resulting internal message bus.
