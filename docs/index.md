# ARSVIN Documentation

ARSVIN is an IEC 61850 Sampled Values engineering suite for Windows. It includes ARSVIN Publisher for stream generation and ArSubsv Subscriber for live or PCAP reception, decoding, visualization, and receiver-side evidence.

## Start here

- [Quick Start](quick-start.md) — install the suite, choose Publisher or Subscriber, and begin with a guarded offline or authorized test workflow.
- [Safety Boundaries](safety-boundaries.md) — laboratory assumptions, authorization, and responsible use.
- [Known Limitations](known-limitations.md) — current protocol, timing, evidence, and runtime boundaries.
- [Licensing](LICENSING.md) — current GPL community edition, historical Apache boundary, and commercial licensing path.

## Sampled Values standards and interoperability

- [SV Standards and Evidence Research Gate](sv-research-gate.md) — authoritative source hierarchy, claim levels, evidence requirements, and research acceptance rules.
- [SV Conformance and Interoperability Matrix](sv-evidence-matrix.md) — implemented, provisional, unknown, and out-of-scope requirements for 9-2LE-style, IEC 61869-9, and generic Layer-2 SV.
- [SV Profile Infrastructure](sv-profile-infrastructure.md) — transport-independent observed facts, evidence-aware definitions, confidence detection, and configuration-versus-wire comparison.
- [Profile Detection Output Contract](profile-detection-output.md) — confidence, evidence, and mismatch presentation rules.
- [Public Terminology Policy](public-terminology-policy.md) — vendor-neutral public wording and automated validation.
- `samples/sv-evidence` — synthetic or authorized evidence intake, anonymization rules, metadata, and regression cases.

## Publisher workflows

- [SV Profile Support](sv-profile-support.md) — current publishing support and limits.
- [Multi-Stream SV Publishing](multi-stream.md) — multiple publisher slots.
- [Publisher Session Engine](publisher-session-engine.md) — runtime session model.
- [Live Preflight Diagnostics](live-preflight-diagnostics.md) — checks before authorized live transmission.
- [P0 Publisher Protocol Roadmap](p0-publisher-protocol-roadmap.md) — `nofASDU`, quality, timing, and PCAP foundation.
- [P1 Publisher Evidence Workflow](p1-publisher-evidence-workflow.md) — generated PCAP and Markdown evidence.
- [P2 Full Publisher Scenario Engine](p2-full-publisher-scenarios.md) — repeatable protection and process-bus scenarios.
- [Waveform Shape Panel](waveform-shape-panel.md) — per-phase and waveform-shaped scenarios.

## Subscriber workflows

- [Subscriber Verification App](subscriber-verification-app.md) — receiver-side stream binding, validation, health, and evidence.
- [ArSubsv SV Scout Companion](arsubsv-sv-scout-companion.md) — discovery, waveform, phasor, RMS, and PCAP analysis.

## Data sources and timing

- [COMTRADE Replay](comtrade-replay.md) — replaying analog records as Sampled Values.
- [PTP and `smpSynch` Compatibility](ptp-and-smpsynch.md) — timing and synchronization boundaries.
- [Sync Compatibility Mode](sync-compatibility-mode.md) — laboratory `smpSynch` behavior.

## Build, release, and repository governance

- [Architecture](architecture.md)
- [Build and Release](build-and-release.md)
- [Public Release Checklist](public-release-checklist.md)
- [SEO and Public Launch Checklist](seo-public-launch-checklist.md)
- [GitHub Repository Settings](github-repository-settings.md)
- [Repository Audit](repository-audit.md)
- [External IP and Provenance Review — 2026-07-14](EXTERNAL_IP_AND_PROVENANCE_REVIEW_2026-07-14.md)
- [Public Wording and Claim Review — 2026-07-14](WORDING_AND_CLAIM_REVIEW_2026-07-14.md)

## Project policies

- [Contributing](../CONTRIBUTING.md)
- [Contributor License Agreement](../CONTRIBUTOR-LICENSE-AGREEMENT.md)
- [Developer Certificate of Origin](../DCO.txt)
- [Security](../SECURITY.md)
- [Support](../SUPPORT.md)
- [Community conduct](../CODE_OF_CONDUCT.md)
- [GPL license](../LICENSE)
- [Commercial licensing](../COMMERCIAL-LICENSE.md)
- [Copyright](../COPYRIGHT.md)
- [Trademark and branding](../TRADEMARK.md)
- [Third-party notices](../THIRD_PARTY_NOTICES.md)

## Documentation principles

Public documentation should:

- distinguish configured SCL expectations from observed wire traffic;
- distinguish transmitter evidence from receiver evidence and IED behavior;
- identify whether evidence comes from tests, loopback, laboratory equipment, or an approved commissioning environment;
- avoid universal interoperability, formal conformance, functional-safety, cybersecurity, and deterministic-timing claims;
- use synthetic or contributor-owned examples; and
- exclude confidential customer, employer, station, credential, and restricted network material.