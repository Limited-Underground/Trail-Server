# Limited Underground Trail Server

Limited Underground Trail Server is the planned Linux-based server and operator interface for a Trail LoRa network. The project is in architecture and interface-prototype stage; no functional server, supported server-radio hardware, production deployment, or field acceptance exists yet.

## Current provisional direction

Server Option V0 uses:

- a dedicated Trail-compatible LoRa device connected to the Linux server through USB;
- a modular ASP.NET Core server application;
- PostgreSQL with PostGIS for persistent and geographic data;
- Caddy for LAN web access and large-file delivery;
- MapLibre and PMTiles for maps;
- a minimal Linux graphical session for the server's local browser, with the same administration interface available to other machines on the LAN by IP;
- plain HTTP on the trusted LAN for the first functional server, with trusted HTTPS deferred unless it can be added without delaying functionality.

The dedicated server-radio device is the server's Trail-network interface. Large files that do not belong on the LoRa network may be delivered separately through the server's IP file service.

## Repository state

The current source is an interactive interface prototype backed by demonstration data. It does not connect to a radio, database, filesystem authority, or live Trail deployment.

- [Architecture](docs/ARCHITECTURE.md)
- [Current status](docs/PROJECT_STATUS.md)
- [Decision 0001: Server Option V0](docs/decisions/0001-accept-server-option-v0.md)
- [Backlog](tasks/BACKLOG.md)

## Local prototype

```powershell
npm install
npm run dev
```

The prototype must continue to identify demonstration data and must not simulate successful hardware or operational actions as real results.

## License

Apache License 2.0. See [LICENSE](LICENSE).
