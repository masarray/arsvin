# ARSVIN Documentation

ARSVIN is a focused IEC 61850 Sampled Values Publisher for Windows. This index groups the documentation by workflow so users can find the right page quickly.

## Start here

- [Quick Start](quick-start.md) — install, open SCL, choose a stream, and publish safely.
- [Safety Boundaries](safety-boundaries.md) — lab-only assumptions and responsible use.
- [Known Limitations](known-limitations.md) — current protocol and runtime boundaries.

## Publisher workflows

- [SV Profile Support](sv-profile-support.md) — current IEC 61850 SV publishing support and limits.
- [Multi-Stream SV Publishing](multi-stream.md) — using multiple publisher slots.
- [Publisher Session Engine](publisher-session-engine.md) — runtime session model.
- [Live Preflight Diagnostics](live-preflight-diagnostics.md) — preflight checks before live TX.
- [P0 Publisher Protocol Roadmap](p0-publisher-protocol-roadmap.md) — nofASDU, quality, timing, and PCAP foundation.
- [P1 Publisher Evidence Workflow](p1-publisher-evidence-workflow.md) — generated PCAP and Markdown evidence export.
- [P2 Full Publisher Scenario Engine](p2-full-publisher-scenarios.md)
- [Waveform Shape Panel](waveform-shape-panel.md) — per-phase and waveform-shaped publisher scenario presets.

## Data sources and timing

- [COMTRADE Replay](comtrade-replay.md) — replaying analog records as Sampled Values.
- [PTP and smpSynch Compatibility](ptp-and-smpsynch.md) — timing and synchronization boundaries.
- [Sync Compatibility Mode](sync-compatibility-mode.md) — lab smpSynch behavior.

## Build, release, and public repository

- [Architecture](architecture.md)
- [Build and Release](build-and-release.md)
- [Public Release Checklist](public-release-checklist.md)
- [SEO and Public Launch Checklist](seo-public-launch-checklist.md)
- [GitHub Repository Settings](github-repository-settings.md)
- [Repository Audit](repository-audit.md)

## Samples

- `samples/scl` — minimal and nofASDU sample SCL files.
- `samples/comtrade` — simple COMTRADE replay files.
- `samples/evidence` — sample Markdown evidence report.
- `samples/scenarios` — P2 full scenario preset notes and scenario matrix.
