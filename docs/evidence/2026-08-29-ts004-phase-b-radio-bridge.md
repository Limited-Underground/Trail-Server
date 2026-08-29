# TS-004 Phase B radio bridge simulator evidence

- **Date:** 2026-08-29
- **Scope:** Transport-neutral, receive-only LUSR/1 background session worker
- **Result:** Phase B accepted at host-simulator level; TS-004 remains in progress
- **Hardware:** Not accessed or evaluated

## Accepted behavior

The ASP.NET service registers a supervised radio bridge that consumes an
abstract ordered byte stream. Deployed configuration keeps the bridge disabled,
so the existing health response remains operational false with radio
unavailable and reason not-configured.

The simulator path validates a fragmented canonical HELLO, resynchronizes after
a corrupt record, negotiates LUSR/1 identity, limits, and capabilities, assigns
a fresh session, and writes HELLO_ACK. Monitoring accepts only current-session
HEARTBEAT and STATUS traffic. Major mismatch, stale session, and unknown
MUST_UNDERSTAND traffic fail closed.

RX_PACKET is never acknowledged because TS-005 durable persistence does not
exist. The bridge sends no TX_SUBMIT and retains no payload authority.

## Validation

- PASS: disabled worker never opens transport
- PASS: fragmented HELLO resynchronizes and negotiates
- PASS: coalesced frames remain connection-scoped
- PASS: major mismatch fails closed
- PASS: impossible limits fail closed
- PASS: current-session heartbeat reaches clean EOF
- PASS: post-handshake version mismatch fails closed
- PASS: stale session is rejected
- PASS: unknown mandatory frame terminates
- PASS: RX packet is never acknowledged without persistence
- PASS: enabled worker reconnects fresh and disposes streams
- RADIO_BRIDGE_SIMULATOR_RESULT=PASS

## Nonclaims

This is not serial or USB discovery, a device path, container device access,
Heltec compatibility, firmware or RF evidence, transmission, durable receive,
delivery, performance, field readiness, or an operational Trail Server. TS-005
owns persistence and TS-008 owns server-radio hardware integration.
