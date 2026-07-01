# Changelog

## Unreleased

### Added
- P0 SV publisher protocol upgrade: `nofASDU=1/2/4/8` support.
- Multi-ASDU SavPdu generation with sequential per-ASDU `smpCnt`.
- Second-aligned sample counter initialization for lab publishing.
- Publisher SCL validation and frame preview helpers.
- Sampled Value quality-bit helper and generated PCAP export helper.
- Golden-style unit tests for multi-ASDU frame round-trip, quality bits, sample counter, and PCAP output.
- Sample SCL file for `nofASDU=8`.
- P0.2 TX Timing Health metrics: target/actual FPS, jitter, late frames, missed schedules, and send duration.


All notable changes to ARSVIN should be documented in this file.

This project follows a simple public-release style:

- `Added` for new features
- `Changed` for changes in existing functionality
- `Fixed` for bug fixes
- `Security` for vulnerability-related changes

## 0.1.0

### Added

- Initial public Apache-2.0 repository structure
- WPF desktop app for IEC 61850 Sampled Values publishing
- SCL-based SV stream setup
- Manual, ramp, and state-sequenced publishing workflows
- COMTRADE replay support
- Lab PTP and `smpSynch` compatibility controls
- GitHub Actions CI, CodeQL, Pages, and release packaging

## P0.1 - Publisher evidence polish

- Added selected-slot SV quality presets and runtime quality-bit injection.
- Added generated PCAP export for offline Wireshark/tool verification.
- Added quality warnings to live preflight.
- Improved publish status with per-publisher quality labels.

## P1 - Publisher evidence workflow

- Added Markdown publisher evidence report export.
- Added TX-side SCL validation and stream summary into the report.
- Added report button in the ribbon beside generated PCAP export.
- Added report writer unit test coverage.
- Added `docs/p1-publisher-evidence-workflow.md`.

