# TS-002 first VM reproduction evidence

**Date:** 2026-08-28
**Public source baseline:** `ebb9310e4122a6c1072705e4e84bbad3df04ee03`

## Reproduction target

- Windows 11 Pro Hyper-V Generation 2 VM;
- two virtual CPUs;
- dynamic memory with 2 GiB minimum, 4 GiB startup, and 8 GiB maximum;
- dynamically expanding 40 GiB virtual disk;
- Linux UEFI Secure Boot enabled;
- automatic checkpoints disabled;
- Debian `debian-13.6.0-amd64-netinst.iso`;
- verified image SHA-256
  `65273beed27b2df543b68b65630ba525cfbad8df2b12035732b2dff87d6664e7`.

No password, MAC address, private IP address, physical adapter identity, or
other deployment-private identifier is recorded here.

## Passed evidence

- The versioned provisioner completed.
- The VM rebooted into the locked Chromium kiosk and displayed the explicit
  host-profile-ready, non-operational page.
- The post-reboot root verifier passed the frozen host identity, required
  packages, active and enabled services, locked non-administrative kiosk,
  loopback Caddy health response, default-deny firewall profile, and
  non-loopback listener checks.
- Windows host checks reached SSH and HTTP through the temporary Hyper-V NAT
  path. A bounded sample of non-permitted TCP ports was denied.

Verifier result:

```text
PASS: frozen host identity
PASS: required packages installed
PASS: required services active after boot
PASS: locked non-administrative kiosk
PASS: loopback host-ready page
PASS: default-deny firewall profile
PASS: no unexpected non-loopback TCP listeners

Sanitized package manifest:
caddy                   2.6.2-12+deb13u1
chromium                151.0.7922.173-1~deb13u1
docker-compose          2.26.1-4
docker.io               26.1.5+dfsg1-9+deb13u1
lightdm                 1.32.0-6+b2
nftables                1.1.3-1
openbox                 3.6.1-12+b2
openssh-server          1:10.0p1-7+deb13u4
unclutter-xfixes        1.6-1+b1
xserver-xorg            1:7.7+24+deb13u1

HOST_PROFILE_RESULT=PASS
```

## Deviations and remaining gates

- The first installation needed an ISO-based SSH bootstrap before the network
  repository typo could be corrected.
- The available USB Wi-Fi external-switch path did not provide guest DHCPv4.
  Provisioning and host checks therefore used Hyper-V Default Switch NAT.
- The result does not prove router-reserved addressing or HTTP/SSH access from
  a second machine on the final external LAN.
- The blocked-port checks must be repeated from that final LAN path.
- The individual service restart-and-reverify sequence remains open.
- A clean reinstall, reprovision, reboot, and reverify cycle remains open.

This evidence advances TS-002 but does not complete it and does not establish a
functional, supported, production-ready, or field-tested Trail Server.
