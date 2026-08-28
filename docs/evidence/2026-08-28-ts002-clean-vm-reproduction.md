# TS-002 clean VM reproduction evidence

**Date:** 2026-08-28

## Reproduction boundary

A second clean Generation 2 Hyper-V VM was installed from the frozen Debian
13.6.0 `amd64` netinst image. The installation omitted the desktop task, used
the versioned public TS-002 provisioner, rebooted, and displayed the locked
non-operational host-ready kiosk page.

No password, SSH key, MAC address, private IP address, physical adapter
identity, or other deployment-private identifier is recorded here.

## Post-reboot verifier result

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

Caddy reported that automatic HTTPS is disabled. That warning is expected for
Decision 0002's initial plain-HTTP trusted-LAN profile and does not invalidate
the verifier result.

## Result and remaining gate

This result proves the selected software-host profile can be cleanly installed,
provisioned, rebooted, and verified from the frozen source. It does not select
physical server hardware and does not prove the final network boundary.

TS-002 remains in progress until the VM receives its private router reservation
and a second machine on that LAN proves HTTP and SSH access plus denial of the
bounded non-permitted-port sample.
