# Decision 0003: Accept server-radio USB contract v1

- **Date:** 2026-08-29
- **Status:** Accepted for host simulation; hardware not evaluated

## Decision

Accept LUSR/1 in
[server-radio-usb-v1.md](../contracts/server-radio-usb-v1.md) as the first
bounded interface between the Linux Trail Server and one dedicated
Trail-compatible server-radio device.

The Linux side remains the durable queue authority. The radio side has bounded
volatile queues and advertised credits. Logical identifiers, per-boot
reconciliation, idempotent transmit IDs, durable receive-before-ack behavior,
and outcome_unknown handling prevent a reconnect or reboot from being
misrepresented as successful delivery or from automatically duplicating an
uncertain RF transmission.

The carried radio payload remains opaque. This decision therefore does not
select or duplicate the OpenTrail on-air packet, security, or acknowledgement
contract.

## Acceptance boundary

Acceptance requires deterministic host fixtures and compact simulator tests
for framing corruption and resynchronization, version negotiation,
backpressure, duplicate transmit IDs, durable receive-before-ack behavior,
disconnect/reconnect, device reboot uncertainty, host restart reconciliation,
and log redaction.

This decision does not require or authorize firmware flashing or hardware
access. Exact USB and radio hardware evidence remains TS-008.
