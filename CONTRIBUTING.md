# Contributing

Thank you for helping improve the Limited Underground Trail Server public
prototype. This repository values small, evidence-backed contributions over
unsupported feature breadth.

## Before opening an issue

1. Read the [repository scope](docs/REPOSITORY_SCOPE.md) and
   [current project status](docs/PROJECT_STATUS.md).
2. Search existing issues and the [backlog](tasks/BACKLOG.md).
3. Remove credentials, private radio material, device identifiers, private
   network details, and raw field captures.
4. Do not report a planned, simulated, or host-tested result as hardware-tested
   or production-ready.

Security vulnerabilities must follow [SECURITY.md](SECURITY.md), not a public
issue.

## Proposing a change

- Keep public-prototype, firmware, Android, website, and future production
  server boundaries separate.
- Open an issue before a substantial architecture, protocol, deployment, or
  compatibility change.
- Preserve explicit limits, refusal behavior, privacy boundaries, and
  non-operational states.
- Include tests or reproducible validation proportional to the change.
- Update architecture, decisions, status, backlog, or evidence records when an
  accepted result changes them.

## Local validation

Install Node.js 22 or newer and the .NET 8 SDK, then run:

```powershell
npm ci
npm test
```

The complete command validates lint, documentation links, host/deployment
scripts, radio-contract and radio-bridge simulator suites, and the web build.

## Pull requests

- Use a focused title and explain the evidence boundary.
- Link the relevant issue or backlog item when one exists.
- List exact validation commands and results.
- Identify any hardware, network, privacy, or safety claim explicitly; use
  `none` when no such claim is made.
- Do not include unrelated generated files or local artifacts.

By contributing, you agree that your contribution is licensed under the
repository's Apache License 2.0.
