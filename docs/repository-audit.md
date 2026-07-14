# Repository Audit

Audit refreshed: 2026-07-14

## Summary

ARSVIN now has a professional public-repository foundation for an IEC 61850 Sampled Values engineering suite:

- GPL-3.0-or-later current community edition;
- separate negotiated commercial licensing path;
- preserved historical Apache-2.0 boundary;
- Publisher, Subscriber, and shared-engine architecture;
- deterministic tests and coverage gates;
- locked dependencies, vulnerability reporting, SBOM, checksums, and artifact attestations;
- CI, CodeQL, Pages, installer, and immutable release automation;
- structured issue and pull-request workflows;
- SEO-ready product site and searchable documentation; and
- operational, provenance, wording, and external-material controls.

## Current strengths

- Clear scope: Sampled Values generation, reception, decoding, visualization, diagnostics, and evidence.
- Explicit distinction between transmitter evidence, receiver evidence, and IED behavior.
- Current-license and public-wording verification before build, Pages deployment, and release packaging.
- Separate Publisher and Subscriber applications using one shared engine.
- Release artifacts include current license, commercial notice, copyright, trademark, and third-party notices.
- Public fixtures are expected to be synthetic or documented as authorized and sanitized.
- Current claims avoid formal conformance, calibrated measurement, deterministic timing, functional-safety, cybersecurity, and IED-consumption guarantees.

## Remaining priorities

1. Expand deterministic coverage for live transport, scheduling, MMS/SCL edge cases, malformed traffic, and device-interoperability paths.
2. Add additional independently sourced packet fixtures with documented provenance.
3. Add Authenticode signing when a suitable certificate and operational process are available.
4. Maintain evidence labels that distinguish automated tests, loopback, laboratory devices, and approved commissioning environments.
5. Perform a contributor, employment, invention-assignment, confidentiality, trademark, and private-artifact review before a high-value commercial agreement.

## Scope limitations

Repository inspection cannot prove ownership of every off-repository artifact or resolve employment and contractual obligations. Git account attribution is evidence of activity, not by itself conclusive legal ownership.

See [External IP and Provenance Review](EXTERNAL_IP_AND_PROVENANCE_REVIEW_2026-07-14.md), [Public Wording and Claim Review](WORDING_AND_CLAIM_REVIEW_2026-07-14.md), and [Licensing](LICENSING.md).