# Security Policy

ARSVIN can capture and transmit raw Ethernet frames. Cybersecurity vulnerabilities and operational-risk defects are reviewed separately but both require careful handling because live use may affect a test network or connected equipment.

## Supported versions

Only the current `main` branch and latest public release are supported for security review. Historical archive branches preserve earlier licensing and source history but are not maintained security release lines.

## Report a cybersecurity vulnerability

Use GitHub private vulnerability reporting or a private Security Advisory. When that is unavailable, contact the maintainer through the `masarray` GitHub profile without posting exploit details publicly.

Include:

- a clear description and affected version or commit;
- the security boundary or trust assumption that fails;
- the smallest reproducible case that can be shared lawfully;
- whether packet capture, transmission, file parsing, installer, update, or privilege behavior is involved; and
- sanitized evidence with credentials, customer information, station identifiers, restricted addressing, and proprietary material removed.

## Report an operational-risk defect

Issues such as unintended transmission, incorrect adapter selection, missing live-mode confirmation, misleading evidence, unexpected stream persistence, timing-health misreporting, or failure to stop output may be reported as operational defects. Use a public issue only when all details are sanitized; otherwise contact the maintainer privately.

Software checks do not establish network authorization, equipment isolation, switching authority, functional safety, or process safety.

## Responsible disclosure

- Do not test networks, devices, drivers, or installers without authorization.
- Do not publish exploit or hazardous reproduction details before reasonable coordinated review.
- Do not upload real station SCL, credentials, production captures, internal network plans, customer identity, or proprietary third-party material to public issues.
- Use synthetic or contributor-owned fixtures whenever practical.

## Product boundary

ARSVIN is intended for authorized laboratory, development, troubleshooting, commissioning-support, and education workflows. It is not calibrated deterministic real-time equipment, a formal conformance platform, a functional-safety system, a cybersecurity-certified product, or proof that an IED consumed or acted on a multicast stream.

The software is provided under GPL-3.0-or-later without warranty, except where a separate written agreement expressly states otherwise.