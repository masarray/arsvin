## 2026-07-02 — Publisher close + subscriber phasor/header fix

- Fixed publisher shutdown dispatch so late background UI events do not throw `NullReferenceException` after `Application.Current` starts closing.
- Normalized ArSubsv phasor angles to `Va = 0°` so the subscriber phasor matches the publisher reference direction.
- Cleaned the ArSubsv header: removed the duplicate header icon and made toolbar alignment more compact and modern.


## 2026-07-02 — ArSubsv lean UI buildfix 3

- Fixed ArSubsv auto-payload decoding to use `byte[].AsSpan()` instead of the non-existent `byte[].Span` property.
- Added the shared in-memory process-bus transport sources to the test project so publisher session tests compile with the linked engine source.
- Added an explicit `assembly=ARSVIN.Subscriber` hint for the ArSubsv custom controls namespace to reduce WPF designer resolution issues.

## ARSVIN Subscriber companion app

- Added `src/ARSVIN.Subscriber`, a separate WPF IEC 61850 Sampled Values subscriber and verification companion.
- Added Npcap capture, SV APDU decode, SCL binding, stream health, sample-counter continuity checks, decoded value display, and Markdown verification report export.
- Kept publisher and subscriber as separate applications so ARSVIN Publisher remains focused on SV generation.

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


## ArSubsv SV Scout Companion

- Added ArSubsv branding for the subscriber companion application.
- Added live SV discovery workflow, Scope/Phasor/Values/Frame/Diagnostics tabs.
- Added oscilloscope-style waveform preview for SCL-decoded current/voltage samples.
- Added phasor/RMS/peak/angle calculation from decoded waveform history.
- Added classic PCAP import for offline stream verification.
- Expanded receiver-side Markdown evidence report with phasor and quality summary.

### ArSubsv buildfix 2
- Fixed Subscriber frame parser call to pass `ReadOnlyMemory<byte>` directly instead of `ReadOnlySpan<byte>`.
- Keeps custom oscilloscope/phasor controls compiled in the Subscriber project; remaining XAML designer warnings should clear after clean/rebuild.

## Unreleased — ArSubsv DigSubAnalyzer-style receiver UI hotfix

- Reworked ArSubsv into a dark, engineering workspace inspired by DigSubAnalyzer's process-bus analyzer shell.
- Replaced the light prototype layout with a compact stream explorer, selected stream inspector, scope, phasor, values, frame, and diagnostics workspace.
- Added auto fixed payload decoding for 9-2LE/UCA-style 4I+4V streams and common value+quality sample layouts so waveform/phasor views work without SCL when the payload profile is recognizable.
- Updated oscilloscope and phasor controls to use a dark process-bus visual style and live traces instead of the previous blank prototype scope.

## Unreleased — ArSubsv Lean Engineering UI

- Reworked ArSubsv into a lean Linear/shadcn-inspired engineering workspace.
- Removed the dashboard metric card row; live counts now live in a compact status bar.
- Made SV Explorer the persistent left rail.
- Made waveform the dominant center workspace and phasor the persistent right rail.
- Kept values, frame details, diagnostics, and cursor compare as secondary tabs.
- Refined oscilloscope and phasor colors to match the lean dark shell.

## Publisher slot selection hotfix

- Fixed live auto-apply routing so editing SV2/SV3 while publishing updates the currently selected publisher slot instead of the slot that was selected when publishing started.
- Manual edits are now saved to the selected slot and the live loop resolves the selected slot dynamically per frame.
