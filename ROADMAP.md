# Roadmap

This roadmap keeps ARSVIN focused as an IEC 61850 Sampled Values Publisher, Subscriber, and evidence workbench.

Completed work belongs in [CHANGELOG.md](CHANGELOG.md). This document lists future priorities and claim boundaries.

## Near term

- Keep current-license, provenance, terminology, site, build, test, packaging, and release gates green.
- Expand malformed-frame, VLAN, quality, `nofASDU`, sample-counter, SCL, COMTRADE, and PCAP regression fixtures.
- Add more independently sourced or synthetic evidence packages with documented provenance.
- Improve SCL error messages, dataset mapping diagnostics, and configuration-versus-wire findings.
- Expand keyboard, scaling, accessibility, and long-session UI validation.
- Add clearer runtime evidence labels for automated tests, loopback, laboratory equipment, and approved commissioning environments.
- Audit release archives for required GPL, commercial, copyright, trademark, third-party, and historical-boundary documents.

## Publisher priorities

- Extend per-phase scenario modelling and reusable timeline editing.
- Add additional harmonic, DC-offset, clipping, and transient approximations for controlled laboratory streams.
- Improve timing-health evidence while preserving the explicit non-deterministic Windows scheduling boundary.
- Add headless dry-run, validation, PCAP generation, and report-generation workflows.
- Improve multi-stream resource and adapter diagnostics.

## Subscriber priorities

- Add PCAPNG import.
- Add manual stream definitions when SCL is unavailable.
- Improve cursor and measurement interaction in waveform views.
- Add per-stream CSV waveform export.
- Expand IEC 61869-9-oriented dataset presentation only where requirements and evidence are documented.
- Improve traffic continuity, duplicate, reordering, rate-change, and payload-layout diagnostics.

## Shared-engine priorities

- Maintain protocol, SCL, profile, comparison, capture, and transport logic in `ARSVIN.Engine`.
- Expand deterministic tests for live-capture abstractions, scheduling boundaries, and transport failure recovery.
- Keep profile detection evidence-aware and prevent sparse observations from producing unqualified confirmation.
- Preserve unknown and unsupported traffic as observable evidence rather than silently normalizing it.
- Maintain locked dependencies, SBOM generation, and third-party notices.

## Public credibility priorities

- Keep README, landing page, generated documentation, release notes, and application wording aligned with implemented behavior.
- Maintain actual application screenshots and project-owned artwork.
- Publish known limitations and evidence scope beside download links.
- Add Authenticode signing when a trusted certificate and operational signing process are available.
- Maintain GPL-3.0-or-later as the only current community license and clearly separate historical grants from later releases.
- Review contributor rights, employment obligations, private artifacts, and trademarks before high-value commercial or OEM transactions.

## Out of scope without a separately evidenced project decision

- Formal IEC 61850 conformance certification.
- Calibrated analog-source or protection-test-set replacement.
- Deterministic real-time or functional-safety claims.
- Cybersecurity certification.
- Production substation network operation without explicit authority and an approved operational process.
- Proof that an IED consumed or acted on a multicast stream.
- Unqualified universal interoperability claims.
- Feature parity, branding, UI imitation, or report imitation based on unrelated proprietary products.