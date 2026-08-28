# Decision 0002: Select the first functional Linux host profile

- **Date:** 2026-08-28
- **Status:** Accepted selection; clean software-host reproduction passed, final reserved-LAN acceptance pending

## Decision

Use the Debian 13.6.0 `amd64` netinst image as the immutable installation
seed for the first functional Trail Server host:

- image: `debian-13.6.0-amd64-netinst.iso`;
- SHA-256: `65273beed27b2df543b68b65630ba525cfbad8df2b12035732b2dff87d6664e7`;
- fixed release directory:
  <https://cdimage.debian.org/debian-cd/13.6.0/amd64/iso-cd/>.

The first reproduction target is a Generation 2 Hyper-V virtual machine with
UEFI Secure Boot using the Microsoft UEFI Certificate Authority template, two
virtual CPUs, 4 GiB startup memory, and a dynamically expanding 40 GiB VHDX.
This proves the software host profile only; it does not select or support final
physical server hardware.

Install Debian without a desktop environment. Select only SSH server and
standard system utilities, use the hostname `trail-server`, and create a
non-root administrator during installation. The provisioning procedure then
installs the maintained Debian packages for Caddy, Chromium, Docker and Docker
Compose, LightDM, nftables, Openbox, OpenSSH, and Xorg.

The local operator session autologs into a locked, non-administrative
`trailkiosk` account. Openbox launches Chromium in kiosk mode at
`http://127.0.0.1/`. Until the real server exists, Caddy serves an explicit
host-profile-ready page that makes no radio, database, or operational claim.

Use IPv4 DHCP on the host with a router-side reservation. The exact assigned
address and network adapter identity are deployment-private and must not be
committed. LAN acceptance uses the recorded reserved IPv4 address, not mDNS or
an invented public name.

Use a default-deny nftables input policy. Permit loopback, established traffic,
the DHCP client exchange, ICMP/ICMPv6, and new TCP connections to ports 22 and
80 only from the explicitly configured IPv4 LAN prefix. PostgreSQL and later
application containers must bind to loopback or private container networks;
publishing container ports to `0.0.0.0` or `[::]` is outside this profile.

Pin the installer image, not vulnerable package revisions. Provisioning applies
current Debian stable/security updates and records the installed package
manifest as reproduction evidence.

## Recovery boundary

The first recovery procedure has three levels:

1. restart and verify the affected Caddy, kiosk, Docker, firewall, or SSH
   service from the local console or authorized LAN SSH session;
2. reboot and repeat the host verification with no manual repair;
3. reinstall from the verified 13.6.0 image, re-run the versioned provisioner,
   reboot, and repeat verification.

Application-data and PostgreSQL restoration are not claimed here because those
systems do not exist yet. Their backup and restoration evidence belongs to the
service and persistence tasks.

## Reproduction gate

This selection does not finish TS-002. The clean VM run now records:

- installer-image verification and exact installation selections;
- provisioner and verifier results;
- local kiosk display after reboot;
- service restart, host reboot, and clean reinstall recovery outcomes; and
- a sanitized installed-package manifest with no MAC address, private IP,
  credential, or other device-specific identifier.

Final TS-002 acceptance still requires HTTP and SSH access from a second LAN
machine by the router-reserved IPv4 address and denial of non-permitted inbound
ports from that final LAN path.

## First VM evidence

On 2026-08-28, the first Hyper-V VM was provisioned and passed the post-reboot
root verifier. The local kiosk displayed the explicit non-operational
host-ready page, required services were active, the loopback health endpoint
passed, and the sanitized package manifest was captured.

That run used temporary Hyper-V NAT after the available USB Wi-Fi external
switch did not provide guest DHCPv4. It therefore does not satisfy the
router-reserved external-LAN or second-machine gate. The individual Caddy,
LightDM, Docker, nftables, and SSH restart-and-reverify recovery level
subsequently passed. See
[the first-VM evidence record](../evidence/2026-08-28-ts002-first-vm-reproduction.md).

## Clean reproduction evidence

Later on 2026-08-28, a second clean Generation 2 Hyper-V VM was installed from
the frozen Debian image without a desktop task, provisioned from the public
source, rebooted, and verified. The locked kiosk displayed the bounded
host-ready page and the complete verifier returned
`HOST_PROFILE_RESULT=PASS`. This proves the software-host clean reinstall,
reprovision, and reboot recovery level. It does not prove the final
router-reserved LAN or second-machine boundary. See
[the clean-VM evidence record](../evidence/2026-08-28-ts002-clean-vm-reproduction.md).

## Sources

- Debian stable release information: <https://www.debian.org/releases/>
- Debian 13 installation guide:
  <https://www.debian.org/releases/trixie/amd64/>
- Debian 13.6.0 checksum list:
  <https://cdimage.debian.org/debian-cd/13.6.0/amd64/iso-cd/SHA256SUMS>
- Microsoft Hyper-V Generation 2 guidance:
  <https://learn.microsoft.com/windows-server/virtualization/hyper-v/plan/should-i-create-a-generation-1-or-2-virtual-machine-in-hyper-v>
