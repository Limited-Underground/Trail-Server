# Decision 0001: Accept provisional Server Option V0

- **Date:** 2026-08-27
- **Status:** Accepted provisional direction

## Decision

Use a minimal Linux host with a lightweight local browser and a LAN-accessible administration website. The first functional server may be reached by IP over plain HTTP on the trusted local network; a publicly trusted HTTPS certificate is not an initial requirement.

Connect one dedicated Trail-compatible LoRa device to the server through USB. That device is the server's interface to the Trail radio network.

Use ASP.NET Core, PostgreSQL/PostGIS, Caddy, MapLibre, PMTiles, and Docker Compose as the initial reusable infrastructure. Keep the radio bridge separable so later Linux USB evidence can determine whether it runs inside a container or as a native supervised service.

Use a separate IP file-delivery surface for large files that do not belong on the LoRa network.

## Alternatives not selected as the controlling base

- Full Supabase: more client synchronization and platform surface than the current server role requires.
- Traccar or OwnTracks: assume IP-reporting clients and do not match the dedicated Trail LoRa gateway boundary.
- ThingsBoard or OpenRemote: impose broader IoT platform and device models.
- ChirpStack: implements LoRaWAN rather than the Trail direct-LoRa protocol.
- MQTT broker: not required for one locally attached server-radio device; reconsider only if multiple remote gateways create a demonstrated need.

## Change policy

This decision intentionally remains provisional. Individual components may be replaced by later evidence-backed decisions without treating Option V0 as permanent product compatibility or release authority.
