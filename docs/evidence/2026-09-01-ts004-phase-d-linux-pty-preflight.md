# TS-004 Phase D Linux PTY preflight evidence

**Date:** 2026-09-01
**Result:** Linux container preflight passed; Debian 13 host reproduction open

## Exact evidence

GitHub Actions validated public commit `6eed8eb9c857ed671671ccd03af77408f4153d43`
in [workflow run 33545451208](https://github.com/Limited-Underground/Trail-Server/actions/runs/33545451208).
The hardened integration step ended with:

```text
RADIO_BRIDGE_LINUX_PTY_RESULT=PASS
```

The gate builds the integration executable in the .NET 8 SDK Debian Bookworm
image and runs it from the matching ASP.NET 8 Debian Bookworm image. Runtime
controls are a numeric non-root user, no network, a read-only root filesystem,
all Linux capabilities dropped, `no-new-privileges`, bounded memory/CPU/PIDs,
an external 30-second timeout, and no physical device mapping.

## Accepted preflight boundary

The test creates two Linux kernel pseudo-terminals and one unmistakably
synthetic stable-path link in a disposable `/dev/serial` tmpfs. It uses the
actual `ConfiguredRadioByteTransport`, `System.IO.Ports`, and
`ServerRadioBridge` to prove:

- binary fragmented LUSR/1 HELLO input produces a valid HELLO_ACK;
- closing the first peer causes a delayed fresh transport open;
- the second connection negotiates a different session identity;
- stopping while the second serial read is blocked completes within five
  seconds and disposes both serial owners;
- no third open occurs after shutdown;
- the final state is the bounded `service-stopped` state;
- missing-endpoint and worker logs do not expose configured paths, PTY paths,
  exception detail, or the synthetic identity.

The existing full repository suite also passed in the same workflow. Published
Compose configuration remains explicitly disabled, loopback-only, non-root,
read-only, capability-free, and without `/dev` or physical-device authority.

## Explicit nonclaims and remaining gate

This result is Linux container/runtime evidence, not the exact Debian 13.6
Trail Server host reproduction. The same public command still must return PASS
on that VM before Phase D receives target-host acceptance:

```bash
sudo ./tools/test-radio-bridge-linux.sh
```

Pseudo-terminals do not prove physical USB CDC behavior, baud accuracy,
electrical framing, modem-control behavior, udev identity or permissions,
exclusive access, physical unplug/re-enumeration, firmware compatibility, or
RF behavior. A blocked synchronous `SerialPort.Open()` remains uncancellable;
the gate proves bounded shutdown of a blocked active read, not a blocked open.
No RX durability/acknowledgement, TX path, database, supported hardware, field
readiness, safety assurance, production readiness, or operational Trail Server
is claimed.
