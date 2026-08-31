# Trail Server Agent Guide

## Project boundary

This repository is the maintained Apache-2.0 public prototype and reference boundary for Limited Underground Trail Server work. Future production and commercial implementation belongs in a separate private repository; only sanitized public interfaces, documentation, tooling, and evidence belong here. Do not copy private planning, licensing enforcement, device identifiers, credentials, radio keys, raw private captures, or unrelated OpenTrail implementation files into this repository.

The OpenTrail firmware and Android repositories remain separate authorities for their own protocols and release evidence. Reuse their public contracts through explicit versioned dependencies or documented interfaces rather than duplicated source.

## Current architecture authority

Decision 0001 records the provisional Server Option V0. A later evidence-backed decision may replace individual components, but changes must preserve the dedicated Trail-compatible LoRa server-radio boundary unless the owner explicitly changes that product direction.

## Evidence and publication

- Keep planned, prototyped, host-tested, hardware-tested, field-tested, and production-ready states distinct.
- Update `docs/PROJECT_STATUS.md`, the backlog, and applicable decisions when accepted state changes.
- Validate the exact public source before committing.
- Commit and push public-ready changes to the intended GitHub repository.
- Update the public website only when the project status genuinely changes.
- Never publish secrets, private network configuration, device-specific identifiers, or unsupported safety and reliability claims.
