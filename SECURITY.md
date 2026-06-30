# Security Policy

ARSVIN can transmit raw Ethernet frames. Security and safety reports are taken seriously because misuse on a live process-bus or substation network can create operational risk.

## Supported versions

Only the current `main` branch and the latest public release are supported for security review.

## Reporting a vulnerability

Please report security issues privately by opening a GitHub Security Advisory on the repository. If that is not possible, contact the maintainer through GitHub.

Please include:

- A clear description of the issue
- A minimal reproduction case, if safe to share
- Affected version or commit
- Whether live Ethernet transmission is involved
- Any relevant packet details, with confidential information removed

## Responsible disclosure expectations

- Do not test against networks or devices without authorization.
- Do not publish exploit details before the maintainer has had a reasonable opportunity to respond.
- Do not include real substation SCL files, relay credentials, internal IP plans, or production packet captures in public issues.

## Safety boundary

ARSVIN is intended for isolated lab networks, point-to-point test links, and engineering education. It is not designed for production substations, closed-loop protection validation, or calibrated protection testing.
