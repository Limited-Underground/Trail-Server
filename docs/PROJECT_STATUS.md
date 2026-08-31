# Trail Server Project Status

**As of:** 2026-08-31

## Current state

- Server Option V0 is accepted as the provisional architecture direction.
- An interactive web-interface prototype exists and uses demonstration data only.
- The standalone public repository is established at
  [GitHub](https://github.com/Limited-Underground/Trail-Server) under the
  Limited Underground organization.
- Decision 0004 defines this repository as the polished, non-operational public
  prototype and reference boundary. Future production and commercial source
  will be developed in a separate private repository.
- The public [Limited Underground Trail Server project page](https://limitedunderground.com/projects/trail-server)
  links to the repository's canonical architecture, status, and backlog records.
- TS-001 foundation publication is complete. This is a project-foundation and
  interface-prototype milestone, not a functional-server or readiness claim.
- TS-002 is in progress. Decision 0002 selects Debian 13.6.0 `amd64`, the
  minimal kiosk, DHCP-reservation addressing, bounded firewall, provisioning,
  verification, and recovery procedure.
- On 2026-08-28, the first Generation 2 Hyper-V VM was provisioned and passed
  the post-reboot host verifier. The local Chromium kiosk displayed the
  explicit non-operational host-ready page; Caddy, Docker, LightDM, nftables,
  and SSH were enabled and active; the loopback health response passed; and
  the bounded listener and firewall checks passed.
- The same VM then passed a sequential restart-and-active check for Caddy,
  LightDM, Docker, nftables, and SSH, followed by a full
  `HOST_PROFILE_RESULT=PASS` verifier run.
- A second clean Generation 2 Hyper-V VM was then installed from the frozen
  Debian image without a desktop task, provisioned from the public source,
  rebooted, displayed the non-operational kiosk page, and returned
  `HOST_PROFILE_RESULT=PASS`. This completes the clean
  reinstall/reprovision/reboot software-host reproduction level.
- The first VM used Hyper-V Default Switch NAT after the available USB Wi-Fi
  external-switch path did not provide guest DHCPv4. Windows host checks reached
  SSH and HTTP and denied a bounded sample of non-permitted ports, but this is
  not the selected router-reserved LAN path or second-machine acceptance.
- TS-004 Phase A is accepted on the first VM. The .NET 8 service builds, exposes
  a configuration-bound /api/health endpoint through Caddy, and explicitly
  reports operational false with radio unavailable. Docker publishes the
  service only to host loopback. Firewall restart recovery, full host
  re-verification, VM reboot recovery, Windows SSH/HTTP access, and denial of
  direct container-port access passed.
- TS-004 Phase B is host-simulator tested. A transport-neutral LUSR/1
  BackgroundService is registered but disabled by default. Eleven cases passed
  disabled-mode authority, fragmented HELLO negotiation, corruption recovery,
  major/session/mandatory-message refusal, clean EOF, and receive-without-ACK
  behavior. No serial/USB transport or hardware was used; TS-004 remains in
  progress.
- TS-003 is complete at the contract and host-simulator level. Decision 0003
  accepts LUSR/1 for the local server-radio byte stream. Its deterministic
  framing, version refusal, required-message handling, credits, duplicate
  suppression, durable receive-before-ack, disconnect/restart reconciliation,
  device-reboot uncertainty, and log redaction passed twelve compact .NET 8
  simulator cases. The on-air payload remains opaque and no hardware was used.

## Not yet implemented or proven

- final external-LAN DHCP reservation and second-machine HTTP/SSH acceptance;
- repeated blocked-port checks from the final reserved-LAN path;
- a real TS-004 serial/USB transport and completed service scaffold;
- PostgreSQL/PostGIS schema;
- dedicated server-radio firmware role;
- USB hardware connection or firmware implementation of LUSR/1;
- live receive or transmit queues;
- large-file delivery;
- map ingestion or rendering from live data;
- application-data backup, restoration, upgrade, endurance, or field acceptance;
- supported hardware, production readiness, or safety assurance.

No completion percentage is assigned.
