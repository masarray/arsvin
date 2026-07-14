# Public Wording and Claim Review — 2026-07-14

This review defines the wording used in the README, documentation, landing pages, release notes, installer, and issue templates.

## Approved positioning

ARSVIN is a Windows IEC 61850 Sampled Values engineering suite for authorized laboratory, development, FAT/SAT preparation, troubleshooting, education, and commissioning-support workflows.

The Publisher generates streams and records transmitter-side evidence. ArSubsv receives, decodes, visualizes, and records receiver-side evidence from the selected computer and network adapter.

## Evidence boundary

Public wording must distinguish:

- configured SCL expectations from observed Ethernet traffic;
- transmitter evidence from receiver evidence;
- software observations from protection-system behavior;
- test or laboratory evidence from formal conformance; and
- protocol readiness from network authorization and operational authority.

ARSVIN cannot infer application-layer acknowledgement for multicast Sampled Values and does not prove that another IED consumed, trusted, or acted on a stream.

## Terms to avoid as unqualified claims

Avoid unqualified use of:

- certified, compliant, conformant, universal, deterministic, calibrated, production-ready;
- safe, secure, trusted, correct, guaranteed, field-proven;
- official, approved, endorsed, or equivalent; and
- wording that implies protection performance, functional safety, cybersecurity approval, switching authority, or equipment isolation.

Use scoped evidence terms instead, such as:

- implemented;
- covered by deterministic tests;
- validated by the stated automated check;
- exercised in a simulator, loopback, or identified laboratory environment;
- observed by the selected computer and adapter;
- provisional, partial, unsupported, unknown, or not yet verified.

## Operational wording

Prefer “guarded live workflow” or “authorized isolated test link” over “safe live mode.” Software checks cannot establish site safety, isolation, authorization, or the absence of process consequences.

Raw Ethernet transmission and capture must be described as active network operations requiring explicit authorization and an appropriate test boundary.

## External products and comparisons

Public documentation should describe required engineering outcomes directly. It must not use unrelated product branding as a feature target, imply affiliation, or copy another product’s UI, wording, reports, screenshots, artwork, internal structure, or documentation.

Lawfully licensed black-box interoperability testing may support neutral protocol observations. It does not authorize copying protected expression or internal implementation material.

## Licensing wording

Current public source and release packages are `GPL-3.0-or-later`. Historical Apache-2.0 revisions are identified only as history and remain on `archive/apache-2.0-final`. The two licenses must not be presented as a current user choice.

Commercial licensing is a separate negotiated path. A repository notice inviting commercial discussion is not itself a commercial license.

## Review result

The licensing migration updates public metadata, structured data, README, generated documentation, release packages, installer content, project metadata, contribution records, and automated checks to follow these boundaries.