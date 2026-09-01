# Limited Underground Trail Server

[![Validation](https://github.com/Limited-Underground/Trail-Server/actions/workflows/validate.yml/badge.svg)](https://github.com/Limited-Underground/Trail-Server/actions/workflows/validate.yml)
[![License: Apache-2.0](https://img.shields.io/badge/license-Apache--2.0-blue.svg)](LICENSE)
![Status: public prototype](https://img.shields.io/badge/status-public%20prototype-9a6700)

Public architecture, interface prototypes, host provisioning, contracts, and
reproducibility evidence for the planned Limited Underground Trail Server.

> [!IMPORTANT]
> This repository is a **non-operational public engineering prototype**. It does
> not contain a production Trail Server, supported server-radio firmware, field
> validation, or safety assurance.

[Architecture](docs/ARCHITECTURE.md) ·
[Current status](docs/PROJECT_STATUS.md) ·
[Backlog](tasks/BACKLOG.md) ·
[Documentation index](docs/README.md) ·
[Limited Underground](https://limitedunderground.com)

## Purpose

Trail Server is planned as a Linux-based server and operator interface for a
Trail LoRa network. The provisional design connects a dedicated
Trail-compatible radio device over USB to a modular ASP.NET Core application.
PostgreSQL/PostGIS, Caddy, MapLibre, and PMTiles form the current persistence,
LAN access, and map direction.

```text
Trail LoRa network
        |
Dedicated server-radio device
        | USB / LUSR/1
        v
Linux Trail Server
        |
        +-- ASP.NET Core API and background services
        +-- PostgreSQL / PostGIS
        +-- Caddy LAN entry point
        +-- Browser-based local and LAN administration
```

Large files are intentionally delivered over a supported IP path rather than
transported through the constrained LoRa network.

## Current evidence

| Area | State | Evidence boundary |
| --- | --- | --- |
| Project foundation | Complete | Public repository, architecture, decisions, backlog, and project page |
| Linux host profile | Reproduced; final LAN gate open | Two Hyper-V VM reproductions and post-reboot host verification |
| Server-radio contract | Host-simulator accepted | LUSR/1 framing, sessions, credits, durability rules, and privacy-safe errors |
| ASP.NET Core service | In progress | Health boundary, disabled-by-default bridge, and simulator-tested opt-in Linux serial transport |
| Database, live radio, and field operation | Not implemented | No production or compatibility claim |

See [Current status](docs/PROJECT_STATUS.md) for the exact accepted and open
boundaries. No completion percentage is assigned.

## Repository scope

This repository preserves the Apache-2.0-licensed public prototype and its
reproducibility evidence. Future commercial production server source is a
separate private boundary and is not licensed or distributed by this
repository. See [Repository scope](docs/REPOSITORY_SCOPE.md) and
[Decision 0004](docs/decisions/0004-separate-public-prototype-and-production-source.md).

## Quick start

Prerequisites:

- Node.js 22 or newer
- .NET 8 SDK

```powershell
npm ci
npm test
npm run dev
```

On Windows, [`Start-TrailServerPrototype.ps1`](Start-TrailServerPrototype.ps1)
provides the stable prototype launcher. Demonstration data must always remain
visibly labeled and must never be presented as live hardware or operational
evidence.

## Repository map

| Path | Purpose |
| --- | --- |
| [`app/`](app/) | Browser interface prototype using demonstration data |
| [`server/`](server/) | ASP.NET Core API, LUSR/1 contract, bridge, and simulator tests |
| [`deploy/host/`](deploy/host/README.md) | Reproducible Debian host profile and verifier |
| [`deploy/app/`](deploy/app/README.md) | Bounded application deployment and verifier |
| [`docs/`](docs/README.md) | Architecture, decisions, contracts, evidence, and scope |
| [`tasks/`](tasks/BACKLOG.md) | Canonical engineering backlog and dated progress |

## Contributing, support, and security

Read [CONTRIBUTING.md](CONTRIBUTING.md) before opening a change. Use the
structured GitHub issue forms for reproducible defects and bounded proposals.
Read [SUPPORT.md](SUPPORT.md) for the non-operational support boundary. Report
security vulnerabilities privately according to [SECURITY.md](SECURITY.md).

## License

The contents currently published in this repository are licensed under the
[Apache License 2.0](LICENSE). That license applies to this public prototype; it
does not grant rights to separate future production server software that is not
published here.
