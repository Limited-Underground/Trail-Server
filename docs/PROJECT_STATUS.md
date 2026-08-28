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
- The TS-002 host profile has passed repository static checks only. It has not
  yet been reproduced in a clean Linux VM, reached from a second LAN machine,
  reboot-tested, or reinstall-tested.

## Not yet implemented or proven

- clean Debian host installation and exact-profile reproduction;
- local kiosk display, second-machine LAN access, and firewall acceptance;
- service restart, host reboot, and clean reinstall recovery evidence;
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
