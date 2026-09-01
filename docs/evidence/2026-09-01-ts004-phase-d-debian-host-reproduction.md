# TS-004 Phase D Debian host reproduction evidence

**Date:** 2026-09-01
**Result:** Target Debian host containerized PTY reproduction passed

## Frozen source and observed host boundary

The selected Trail Server host ran the sanitized verifier from exact clean
public commit `756937d4b14eb078f1ef96285d36950db8fe8336` with its local Docker
daemon. The observed platform was Debian 13.6 `amd64`, kernel
`6.12.105+deb13-amd64`, Docker Server `26.1.5+dfsg1`, and Docker Compose
`2.26.1-4`.

The built test image was observed as
`sha256:2d1af4817448699353459ca6d2c2aa12b1d7715b531be19ddc4d79081e79368d`
and reported these path-stripped runtimes:

- Microsoft.AspNetCore.App 8.0.30
- Microsoft.NETCore.App 8.0.30

The container base tags and NuGet restore are not digest/lock-file pinned, so
this is observed-version evidence rather than bit-reproducible image evidence.

## Accepted evidence

Before the PTY gate, the installed host verifier returned
`HOST_PROFILE_RESULT=PASS` and the deployed application verifier returned
`TRAIL_SERVER_APP_RESULT=PASS`. The host retained its frozen identity,
required packages and services, locked kiosk, loopback host-ready page,
default-deny firewall, and bounded listeners. The application retained its
non-operational health response, loopback-only publication, and Docker/nftables
recovery coupling.

The hardened PTY container then returned:

```text
RADIO_BRIDGE_LINUX_PTY_RESULT=PASS
```

Afterward, both the application and host verifiers passed again, and the
sanitized wrapper ended with:

```text
TARGET_HOST_LINUX_PTY_RESULT=PASS
```

This accepts target Debian host reproduction of the hardened containerized
Linux PTY preflight. It demonstrates that the exact public harness builds and
runs against the selected host's Linux kernel and local Docker runtime without
weakening the standing application or host profile.

## Sanitized package manifest

```text
caddy             2.6.2-12+deb13u1
chromium          151.0.7922.173-1~deb13u1
docker-compose    2.26.1-4
docker.io         26.1.5+dfsg1-9+deb13u1
lightdm           1.32.0-6+b2
nftables          1.1.3-1
openbox           3.6.1-12+b2
openssh-server    1:10.0p1-7+deb13u4
unclutter-xfixes  1.6-1+b1
xserver-xorg      1:7.7+24+deb13u1
```

## Explicit nonclaims

The gate ran inside a disposable hardened container. It is not native-host
serial validation and grants neither the production container nor the host
application a device mapping. It does not prove physical USB/CDC, real device
identity, udev or group permissions, exclusive access, hotplug or
re-enumeration, baud/electrical/control-line accuracy, blocked synchronous-open
cancellation, server-radio firmware, RF behavior, RX durability or
acknowledgement, TX, database behavior, final LAN acceptance, field/endurance
testing, safety assurance, production readiness, or an operational Trail
Server. TS-004 remains in progress; TS-005 owns durability and TS-008 owns
physical radio integration.
