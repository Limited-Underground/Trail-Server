# TS-004 Phase C Linux serial transport evidence

**Date:** 2026-09-01
**Result:** Host-simulator and static deployment checks passed

## Accepted boundary

Phase C adds one explicit, opt-in Linux serial byte-stream implementation below
the existing `IRadioByteTransport` boundary. It requires an enabled bridge, the
`serial` transport selector, a process-visible path in
`/dev/serial/by-id/`, and a baud rate from 1,200 through 2,000,000. The serial
connection uses 8 data bits, no parity, one stop bit, no hardware or software
flow control, and disabled DTR/RTS control lines.

The returned stream owns the serial connection. Disposal and failed opens
dispose that owner. Open failures are converted to the stable
`transport_unavailable` state without logging the configured path or underlying
exception. No serial-port enumeration or automatic device selection exists.

The application and Compose defaults remain disabled. Compose adds no device
mapping, host networking, privileged mode, group access, or Linux capability.
The API continues to report `operational: false`.

## Checks performed

- Release build of the radio-bridge simulator project;
- fifteen simulator cases, including disabled authority, safe option
  validation, exact path/baud forwarding, pre-open cancellation, redacted open
  failure, owned disposal, fragmented and coalesced LUSR/1 input, protocol and
  session refusal, receive-without-acknowledgement, EOF reconnect, and fresh
  session identity;
- deployment static checks for disabled defaults, absence of device authority,
  fixed serial settings, no enumeration, loopback-only API publication, and
  unchanged container hardening.

## Explicit nonclaims

This is not pseudo-terminal, physical USB, server-radio firmware, RF,
container-device, or field evidence. Synchronous platform `SerialPort.Open()`
cannot be interrupted by the supplied cancellation token while it is blocked;
bounded shutdown during that condition remains an acceptance gap. No exact
device identity is published. No RX persistence or acknowledgement, TX path,
supported hardware, production readiness, or operational Trail Server is
claimed. Those boundaries remain in TS-005 and TS-008.
