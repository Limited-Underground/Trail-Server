# Documentation

The documentation is organized by authority rather than by marketing topic.

## Start here

- [Architecture](ARCHITECTURE.md) — provisional system boundaries and selected components
- [Current status](PROJECT_STATUS.md) — accepted evidence and unimplemented work
- [Repository scope](REPOSITORY_SCOPE.md) — public prototype and private production boundary
- [Backlog](../tasks/BACKLOG.md) — canonical task state and dated progress

## Decisions

- [0001 — Server Option V0](decisions/0001-accept-server-option-v0.md)
- [0002 — First Linux host profile](decisions/0002-select-first-linux-host-profile.md)
- [0003 — Server-radio USB contract v1](decisions/0003-accept-server-radio-usb-v1.md)
- [0004 — Separate public prototype and production source](decisions/0004-separate-public-prototype-and-production-source.md)

## Contracts

- [LUSR/1 server-radio USB contract](contracts/server-radio-usb-v1.md)

## Evidence

- [TS-002 first VM reproduction](evidence/2026-08-28-ts002-first-vm-reproduction.md)
- [TS-002 clean VM reproduction](evidence/2026-08-28-ts002-clean-vm-reproduction.md)
- [TS-003 contract simulator](evidence/2026-08-29-ts003-contract-simulator.md)
- [TS-004 Phase A service scaffold](evidence/2026-08-29-ts004-phase-a.md)
- [TS-004 Phase B radio bridge](evidence/2026-08-29-ts004-phase-b-radio-bridge.md)
- [TS-004 Phase C Linux serial transport](evidence/2026-09-01-ts004-phase-c-linux-serial-transport.md)
- [TS-004 Phase D Linux PTY preflight](evidence/2026-09-01-ts004-phase-d-linux-pty-preflight.md)

Evidence documents describe only their explicit test boundary. Host simulation
does not establish USB hardware, RF, deployment, field, or production evidence.
