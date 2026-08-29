# TS-003 server-radio contract simulator evidence

- **Date:** 2026-08-29
- **Contract:** LUSR/1
- **Result:** Contract accepted and host-simulator tested
- **Hardware:** Not accessed or evaluated

## Accepted boundary

LUSR/1 defines a COBS-delimited, CRC32C-protected, versioned local byte stream
between the Linux server and one dedicated server-radio device. Canonical CBOR
maps carry logical session, queue, transmit, receive, status, and error
semantics. The on-air radio payload remains opaque.

The host is the durable queue authority. The device queue is bounded and
volatile. A duplicate tx_id cannot create a second simulated emission during
the same boot lineage. An accepted request without a final result becomes
outcome_unknown after a device reboot and is not automatically resent. A
receive event is deduplicated and made durable before acknowledgement.

## Validation

The compact .NET 8 simulator returned:

- PASS: semantic canonical message fixtures
- PASS: corrupt frame resynchronization
- PASS: truncated and oversized frames fail closed
- PASS: identity, limits, and capabilities negotiate
- PASS: version negotiation rejects major mismatch
- PASS: unknown mandatory message terminates session
- PASS: credits and duplicate transmit IDs
- PASS: same-boot host restart reconciles IDs
- PASS: USB loss cancels queued but allows active RF completion
- PASS: device reboot makes active work uncertain
- PASS: receive sequence is monotonic and durable before acknowledgement
- PASS: privacy-safe logger redacts untrusted detail
- RADIO_CONTRACT_SIMULATOR_RESULT=PASS

The deterministic fixture pins the exact encoded LUSR/1 record bytes. No
firmware, USB device, Heltec board, RF link, or OpenTrail on-air implementation
was used. Exact hardware and firmware integration remains TS-008.
