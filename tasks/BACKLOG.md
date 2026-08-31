# Trail Server Backlog

Items TS-005 and later describe the product roadmap. Production and commercial
implementation will be tracked in a separate private repository; this public
repository will retain only sanitized interfaces, prototype code, tooling, and
evidence appropriate for public collaboration.

| ID | Status | Task | Acceptance boundary |
| --- | --- | --- | --- |
| TS-001 | Done | Establish standalone project foundation | Organized local repository, public GitHub repository, canonical architecture/status/decision records, validation, and Limited Underground public project page |
| TS-002 | In progress | Freeze first functional Linux host profile | Exact distribution, version, installation procedure, minimal GUI/kiosk, LAN addressing, firewall, and recovery procedure selected and reproduced |
| TS-003 | Done | Define dedicated server-radio contract | Versioned USB transport, identity, packet, queue, retry, disconnect, restart, privacy, and error semantics accepted without hardware claims |
| TS-004 | In progress | Scaffold ASP.NET Core service | Build-tested service with health endpoint, configuration boundary, background radio abstraction, and no fake hardware success |
| TS-005 | Planned | Add PostgreSQL/PostGIS persistence | Migrated schema for queues, radio observations, Trail records, geographic data, and audit history with restore evidence |
| TS-006 | Planned | Add LAN administration shell | Local kiosk and second LAN computer reach the same dashboard by IP; internal database ports remain unexposed |
| TS-007 | Planned | Add bounded large-file service | Digest-bound manifest and resumable/range-capable delivery proven without transporting file bytes over LoRa |
| TS-008 | Planned | Integrate one dedicated server-radio device | Exact hardware/firmware binding, receive/transmit evidence, restart recovery, and safe abort behavior |

## Dated progress

- **2026-08-31 — Repository boundary:** Following its move to the Limited
  Underground GitHub organization, the public repository received a
  professional GitHub community profile, consolidated validation workflow,
  documentation index, explicit public/private scope decision, and clearer
  non-operational demonstration labels. Production and commercial source is
  reserved for a separate private repository.

- **2026-08-28 — TS-002:** First Hyper-V VM provisioning, post-reboot
  verification, local kiosk, host-side SSH/HTTP, temporary-NAT firewall probes,
  and the individual Caddy, LightDM, Docker, nftables, and SSH
  restart-and-reverify recovery level passed. Reserved external-LAN acceptance
  remained open.
- **2026-08-28 — TS-002:** A second clean Hyper-V VM was installed from the
  frozen Debian image without a desktop task, provisioned from the public
  source, rebooted, displayed the bounded host-ready kiosk, and returned
  `HOST_PROFILE_RESULT=PASS`. Clean software-host reproduction is now proven;
  router-reserved external-LAN and second-machine acceptance remain open.
- **2026-08-29 — TS-004 Phase A:** A .NET 8 API scaffold, validated
  configuration boundary, explicitly unavailable radio status, loopback-only
  Compose publication, Caddy health route, and Docker/nftables recovery
  coupling passed local build/static checks and first-VM deployment, firewall
  restart, host re-verification, reboot, Windows HTTP/SSH reachability, and
  blocked direct-container-port checks. TS-004 remains in progress; no radio
  transport, background bridge, database, queue, or hardware success exists.
- **2026-08-29 — TS-003:** LUSR/1 freezes the dedicated server-radio local
  byte-stream, identity, framing, credit, idempotency, receive-before-ack,
  reconnect, reboot-uncertainty, error, and privacy boundaries while keeping
  the on-air payload opaque. Twelve compact .NET 8 simulator cases passed. This is
  contract and host-simulator evidence only; no hardware or firmware was used.
- **2026-08-29 — TS-004 Phase B:** A disabled-by-default, transport-neutral
  LUSR/1 BackgroundService passed eleven host-simulator cases for negotiation,
  session refusal, corruption recovery, EOF, and receive-without-ACK behavior.
  No serial/USB implementation, device mount, transmit queue, persistence,
  hardware, RF, or operational claim exists. TS-004 remains in progress.

Planning and prototype work do not advance a completion percentage or establish support, production, or field readiness.
