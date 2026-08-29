# Server-radio USB contract v1

- **Contract:** LUSR/1
- **Status:** Accepted for host simulation; hardware not evaluated
- **Transport boundary:** Local ordered byte stream between one Linux host and
  one dedicated Trail-compatible server-radio device

## Scope

LUSR/1 defines the local host/device link only. The radio payload is opaque
bytes. This contract does not define the OpenTrail on-air packet, cryptography,
acknowledgement, LoRa settings, supported hardware, USB class, VID/PID, stable
Linux device path, or whether the eventual bridge runs natively or in a
container.

## Record framing

Each record is COBS encoded and terminated by one zero byte. The decoded record
is:

| Offset | Size | Field |
| --- | ---: | --- |
| 0 | 2 | Magic bytes 0x54 0x52 (TR) |
| 2 | 1 | Protocol major |
| 3 | 1 | Protocol minor |
| 4 | 1 | Message type |
| 5 | 1 | Flags |
| 6 | 2 | Payload length, unsigned little-endian |
| 8 | N | Canonical CBOR payload |
| 8 + N | 4 | CRC32C of header and payload, little-endian |

The maximum v1 CBOR payload is 4096 bytes. A receiver rejects an empty,
truncated, oversized, length-inconsistent, malformed-COBS, bad-magic, or
bad-CRC record and resumes at the next zero delimiter. Flags bit 0 is
MUST_UNDERSTAND; all other v1 flag bits must be zero.

CBOR maps use unsigned integer keys, definite lengths, shortest integer
encoding, and canonical key order. UUID values are 16-byte byte strings in
network byte order. Unknown optional messages may be ignored. An unknown
MUST_UNDERSTAND message ends the session with an unsupported error.

## Session and identity

The device sends HELLO after every byte-stream open or device reset. The host
answers HELLO_ACK and assigns a random session_id. No transmit request is valid
before this handshake.

- radio_id is an operator-assigned logical UUID. It is not a MAC address,
  eFuse value, USB serial, or other hardware identity.
- boot_id is a fresh random UUID for each device boot.
- session_id is a fresh random UUID assigned by the host for each handshake.
- Sequence values are monotonic only within radio_id plus boot_id.
- Model and firmware strings are informational and never compatibility
  authority.

A protocol-major mismatch refuses the session. Peers negotiate the lower minor
version and the intersection of advertised capabilities. HELLO also negotiates
the decoded-record and opaque-radio-payload limits.

## Message types

| Value | Name | Direction | Purpose |
| ---: | --- | --- | --- |
| 0x01 | HELLO | Device to host | Identity, boot, version, limits, capabilities |
| 0x02 | HELLO_ACK | Host to device | Session and negotiated limits/capabilities |
| 0x03 | HEARTBEAT | Either | Liveness only |
| 0x04 | STATUS | Device to host | Credits, counters, and bounded radio state |
| 0x10 | TX_SUBMIT | Host to device | Submit one opaque radio payload |
| 0x11 | TX_ACCEPTED | Device to host | Accepted into the volatile device queue |
| 0x12 | TX_RESULT | Device to host | Local emitted, failed, expired, cancelled, or outcome_unknown result |
| 0x20 | RX_PACKET | Device to host | One received opaque radio payload and optional metadata |
| 0x21 | RX_ACK | Host to device | Host durably recorded the receive event |
| 0x7f | ERROR | Either | Stable error code and privacy-safe correlation |

Every request and event carries a UUID correlation identifier. TX_SUBMIT uses
the host-created tx_id as its idempotency key. RX_PACKET uses radio_id, boot_id,
and rx_seq as its deduplication identity.

### Canonical CBOR maps

HELLO uses keys 0 radio_id, 1 boot_id, 2 protocol major, 3 maximum supported
minor, 4 maximum decoded-record bytes, 5 maximum opaque-payload bytes,
6 capability identifiers, 7 optional firmware text, and 8 optional model text.

HELLO_ACK uses keys 0 session_id, 1 negotiated major, 2 negotiated minor,
3 negotiated decoded-record bytes, 4 negotiated opaque-payload bytes, and
5 negotiated capability identifiers.

Every post-handshake map uses key 0 session_id and key 1 correlation_id.
Additional keys are:

| Message | Additional keys |
| --- | --- |
| HEARTBEAT | 2 monotonic heartbeat counter |
| STATUS | 2 TX credits, 3 RX credits, 4 radio state, 5 RX overflow count, 6 TX failure count |
| TX_SUBMIT | 2 opaque payload bytes, 3 optional host expiry as Unix milliseconds |
| TX_ACCEPTED | No additional keys; correlation_id is tx_id |
| TX_RESULT | 2 result, 3 optional stable error code; correlation_id is tx_id |
| RX_PACKET | 2 boot_id, 3 rx_seq, 4 opaque payload, 5 optional RSSI dBm, 6 optional SNR quarter-dB, 7 optional channel |
| RX_ACK | 2 boot_id, 3 rx_seq |
| ERROR | 2 stable error code, 3 optional allowlisted safe detail code |

Radio states are 0 unavailable, 1 idle, 2 receive_only, and 3 busy. TX results
are 0 emitted, 1 failed, 2 expired, 3 cancelled, and 4 outcome_unknown.
Capability 1 enables RSSI, 2 enables quarter-dB SNR, and 3 enables channel
metadata. LUSR/1 defines no compression or USB-link encryption capability.

## Queue and result authority

The host database is the durable queue authority. The radio exposes bounded,
volatile TX and RX capacity through STATUS credits. The host must not exceed
advertised credits; queue_full or backpressure is a normal bounded response.

TX_ACCEPTED means only that the device placed the request in volatile memory.
It does not mean RF emission, remote receipt, acknowledgement, or delivery.
TX_RESULT emitted means only that the local radio operation completed without
a locally observed error.

The host durably records and deduplicates RX_PACKET before sending RX_ACK. The
device may resend an unacknowledged receive event only during the same boot.
Overflow is reported through an error and counter; it is never silently
represented as complete.

## Retry, disconnect, and restart

- Before TX_ACCEPTED, the host may retry the same tx_id after its configured
  timeout.
- During the same boot lineage, a duplicate TX_SUBMIT returns the previously
  known state and must never create a second RF emission.
- On USB loss, the device stops accepting requests and cancels queued work that
  has not started. An already-started RF operation may finish.
- Every reconnect performs a new handshake.
- If boot_id is unchanged, the host reconciles outstanding identifiers and may
  safely repeat the same tx_id.
- If boot_id changed, any accepted request without a final result becomes
  outcome_unknown and is not automatically resubmitted.
- A device boot begins with empty volatile queues and remains RF idle or
  receive-only until handshake.
- A host restart applies the same reconciliation rules.

Timeout values, retry counts, and expiry policies are host configuration, not
wire constants.

## Errors and privacy

Stable v1 error codes are protocol_version, malformed_frame, checksum,
oversize, unsupported, invalid_state, queue_full, backpressure, busy, expired,
radio_unavailable, tx_failed, and internal.

Logs and errors may contain logical identifiers, counters, stable codes, and
an allowlisted safe detail code. Arbitrary free text is forbidden. Logs and
errors must not contain opaque payload bytes, keys, MAC or
eFuse values, USB serials, private paths, or private network configuration.
Payload logging is disabled by default.

LUSR/1 assumes a trusted local physical host and least-privilege Linux device
access. CRC32C detects corruption; it is not authentication or confidentiality.
The contract does not protect a compromised host or device.

## Explicit nonclaims

This contract does not establish exact hardware support, electrical or USB
compatibility, RF or regulatory compliance, remote delivery, on-air security,
persistent device queues, performance, endurance, field readiness,
multi-gateway behavior, or an operational Trail Server. TS-008 remains the
first dedicated server-radio hardware evidence gate.
