# Trail Server Project Status

**As of:** 2026-08-28

## Current state

- Server Option V0 is accepted as the provisional architecture direction.
- An interactive web-interface prototype exists and uses demonstration data only.
- The standalone public repository is established at
  [GitHub](https://github.com/nbjelanovic/Limited-Underground-Trail-Server).
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
- The first VM used Hyper-V Default Switch NAT after the available USB Wi-Fi
  external-switch path did not provide guest DHCPv4. Windows host checks reached
  SSH and HTTP and denied a bounded sample of non-permitted ports, but this is
  not the selected router-reserved LAN path or second-machine acceptance.

## Not yet implemented or proven

- final external-LAN DHCP reservation and second-machine HTTP/SSH acceptance;
- repeated blocked-port checks from the final reserved-LAN path;
- clean reinstall, reprovision, reboot, and reverify recovery evidence;
- ASP.NET Core service;
- PostgreSQL/PostGIS schema;
- dedicated server-radio firmware role;
- USB bridge contract or hardware connection;
- live receive or transmit queues;
- large-file delivery;
- map ingestion or rendering from live data;
- application-data backup, restoration, upgrade, endurance, or field acceptance;
- supported hardware, production readiness, or safety assurance.

No completion percentage is assigned.
