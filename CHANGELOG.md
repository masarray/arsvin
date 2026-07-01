# Changelog


### P2.1 Buildfix - Waveform Preview Shape Rendering

- Updated the main waveform preview to render Manual harmonic injection, DC offset, and clipping.
- Kept the advanced controls in the dedicated Waveform dialog so the main window remains clean.


### P2 Full Publisher Scenario Engine

- Expanded State Sequencer scenarios from balanced presets to per-phase publisher scenarios.
- Added A-G fault, B-C fault, negative sequence, zero sequence, per-phase VT fuse fail, harmonic injection, and DC offset transient presets.
- Added publisher-side waveform shaping metadata: DC offset, harmonic percentage/order, and clipping percentage.
- Preserved backward compatibility with existing `.svpub.json` sequence snapshots.
- Added `docs/p2-full-publisher-scenarios.md` and `samples/scenarios/p2-full-scenario-matrix.md`.

All notable changes to ARSVIN should be documented in this file.

This project follows a simple public-release style:

- `Added` for new features
- `Changed` for changes in existing functionality
- `Fixed` for bug fixes
- `Security` for vulnerability-related changes

### P2.1 - Dedicated Waveform Shape Panel

- Added a dedicated `Waveform...` dialog instead of placing harmonic/DC/clipping controls on the main window.
- Added Manual output shape editing for per-channel DC offset, harmonic, harmonic order, and clipping.
- Added selected State Sequencer advanced shape editing for per-phase multipliers, angle offsets, DC offset, harmonic, clipping, and scenario tag.
- Extended channel snapshots so Manual waveform shape is saved with `.svpub.json` plans and publisher slots.


## Unreleased

### Added

- P3 public presentation pack:
  - rewritten top-level README for professional public release positioning,
  - SEO-optimized GitHub Pages landing page,
  - Open Graph / social preview image assets,
  - documentation index,
  - SEO and public launch checklist,
  - GitHub repository settings guide,
  - FAQ structured data on the landing page.
- P2 publisher scenario presets in the State Sequencer: protection fault, CT saturation stress approximation, undervoltage approximation, frequency steps, phase jump, and load reversal.
- Scenario preset key is saved in `.svpub.json` publish plans for traceability.
- Documentation for the P2 scenario preset workflow.
- P1 Markdown publisher evidence report export.
- P1 TX-side SCL validation and stream summary in evidence reports.
- P1 report button beside generated PCAP export.
- P0.2 TX Timing Health metrics: target/actual FPS, jitter, late frames, missed schedules, and send duration.
- P0.1 selected-slot SV quality presets and runtime quality-bit injection.
- P0.1 generated PCAP export for offline Wireshark/tool verification.
- P0 SV publisher protocol upgrade: `nofASDU=1/2/4/8` support.
- Multi-ASDU SavPdu generation with sequential per-ASDU `smpCnt`.
- Second-aligned sample counter initialization for lab publishing.
- Publisher SCL validation and frame preview helpers.
- Golden-style unit tests for multi-ASDU frame round-trip, quality bits, sample counter, timing health, report writer, and PCAP output.
- Sample SCL file for `nofASDU=8`.

### Changed

- README now presents ARSVIN as a focused IEC 61850 Sampled Values Publisher for Windows instead of a broad process-bus tester.
- Landing page now emphasizes honest publisher scope, generated evidence, and safety boundaries.

## 0.1.0

### Added

- Initial public Apache-2.0 repository structure.
- WPF desktop app for IEC 61850 Sampled Values publishing.
- SCL-based SV stream setup.
- Manual, ramp, and state-sequenced publishing workflows.
- COMTRADE replay support.
- Lab PTP and `smpSynch` compatibility controls.
- GitHub Actions CI, CodeQL, Pages, and release packaging.
