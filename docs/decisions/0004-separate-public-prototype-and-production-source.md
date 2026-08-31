# Decision 0004: Separate public prototype and production source

- **Status:** Accepted
- **Date:** 2026-08-29

## Context

This repository was established publicly under Apache License 2.0 and now
contains useful architecture, host provisioning, interface prototypes,
contracts, simulator code, and reproducibility evidence. The owner has selected
a future commercial licensing direction for the production Trail Server while
keeping the device-side public project boundaries separate.

Apache-licensed versions already published must remain usable under their
granted terms. Continuing production server implementation in the same public
source boundary would make the intended future commercial permission-to-use
model ambiguous.

## Decision

The current repository remains the public Apache-2.0 prototype and evidence
boundary. Future production Trail Server source will use a separate private
repository and separate commercial licensing documents.

This public repository may receive:

- documentation and status corrections;
- public prototype maintenance;
- interface-contract clarification;
- reproducibility and sanitized evidence updates.

It must not receive future production-only source, private commercial terms,
eligibility records, pricing, credentials, or device-specific evidence.

## Consequences

- Existing public commits and their Apache-2.0 rights are preserved.
- The public repository can remain useful and professionally maintained.
- Production implementation and licensing can evolve without implying that
  unpublished production software is Apache licensed.
- License enforcement remains outside the first public prototype and requires
  a later independently reviewed production decision.
- Any future transfer between the boundaries must be explicit, reviewed, and
  legally compatible.
