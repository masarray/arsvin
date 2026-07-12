# ARSVIN Documentation

ARSVIN is a focused IEC 61850 Sampled Values engineering suite for Windows. It includes ARSVIN Publisher for stream generation and ArSubsv Subscriber for live/PCAP reception, decoding, visualization, and receiver-side evidence.

## Start here

- [Quick Start](quick-start.md) — install the suite, choose Publisher or Subscriber, and begin with a safe workflow.
- [Safety Boundaries](safety-boundaries.md) — laboratory assumptions and responsible use.
- [Known Limitations](known-limitations.md) — current protocol and runtime boundaries.

## Sampled Values standards and interoperability

- [SV Standards and Evidence Research Gate](sv-research-gate.md) — authoritative source hierarchy, OMICRON benchmark findings, claim levels, and research acceptance rules.
- [SV Conformance and Interoperability Matrix](sv-evidence-matrix.md) — verified, provisional, unknown, and out-of-scope requirements for 9-2LE-style, IEC 61869-9, and generic Layer-2 SV.
- `samples/sv-evidence` — safe evidence intake, anonymization rules, metadata, and minimum regression cases for real merging-unit captures.

## Publisher workflows

- [SV Profile Support](sv-profile-support.md) — current IEC 61850 SV publishing support and limits.
- [Multi-Stream SV Publishing](multi-stream.md) — using multiple publisher slots.
- [Publisher Session Engine](publisher-session-engine.md) — runtime session model.
- [Live Preflight Diagnostics](live-preflight-diagnostics.md) — preflight checks before live TX.
- [P0 Publisher Protocol Roadmap](p0-publisher-protocol-roadmap.md) — nofASDU, quality, timing, and PCAP foundation.
- [P1 Publisher Evidence Workflow](p1-publisher-evidence-workflow.md) — generated PCAP and Markdown evidence export.
- [P2 Full Publisher Scenario Engine](p2-full-publisher-scenarios.md) — repeatable protection and process-bus scenarios.
- [Waveform Shape Panel](waveform-shape-panel.md) — per-phase and waveform-shaped publisher scenarios.

## Subscriber workflows

- [Subscriber Verification App](subscriber-verification-app.md) — receiver-side stream binding, validation, health, and evidence.
- [ArSubsv SV Scout Companion](arsubsv-sv-scout-companion.md) — discovery, waveform, phasor, RMS, and PCAP analysis.

## Data sources and timing

- [COMTRADE Replay](comtrade-replay.md) — replaying analog records as Sampled Values.
- [PTP and `smpSynch` Compatibility](ptp-and-smpsynch.md) — timing and synchronization boundaries.
- [Sync Compatibility Mode](sync-compatibility-mode.md) — laboratory `smpSynch` behavior.

## Build, release, and public repository

- [Architecture](architecture.md)
- [Build and Release](build-and-release.md)
- [Public Release Checklist](public-release-checklist.md)
- [SEO and Public Launch Checklist](seo-public-launch-checklist.md)
- [GitHub Repository Settings](github-repository-settings.md)
- [Repository Audit](repository-audit.md)

## Samples

- `samples/scl` — minimal and `nofASDU` sample SCL files.
- `samples/comtrade` — COMTRADE replay files.
- `samples/evidence` — sample Markdown evidence reports.
- `samples/scenarios` — publisher scenario notes and matrices.
- `samples/sv-evidence` — anonymized or synthetic profile-validation evidence packages.