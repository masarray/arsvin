# Roadmap

This roadmap keeps ARSVIN focused as a professional public IEC 61850 Sampled Values Publisher.

## Done / in progress

- Apache-2.0 public repository foundation.
- SCL-driven SV publishing workflow.
- COMTRADE replay workflow.
- P0 protocol-readiness pass:
  - `nofASDU=1/2/4/8`,
  - sequential per-ASDU sample counter,
  - quality-bit foundation,
  - generated PCAP export,
  - TX Timing Health.
- P1 publisher evidence workflow:
  - Markdown TX evidence report,
  - preflight and stream summary,
  - generated evidence scope wording.
- P2 full publisher scenario engine:
  - protection fault,
  - CT saturation stress approximation,
  - undervoltage approximation,
  - frequency steps,
  - phase jump,
  - load reversal.
- P3 public presentation pack:
  - professional README,
  - SEO landing page,
  - social preview image,
  - documentation index,
  - public launch checklist.

## Near term

- Keep build/test green after every UI and protocol patch.
- Add more golden packet fixtures for SV, VLAN, quality, and nofASDU.
- Add screenshot assets captured from the real Windows app after build verification.
- Add a compatibility matrix based on lab validation with Wireshark and relay/subscriber tools.
- Improve SCL error messages and dataset mapping diagnostics.

## Medium term

- Add per-phase scenario model:
  - single-phase fault,
  - phase-to-phase fault,
  - negative sequence,
  - zero sequence,
  - per-phase undervoltage / VT fuse fail.
- Add harmonic and DC-offset approximation for lab streams.
- Improve scenario timeline editing while keeping ARSVIN publisher-focused.
- Add CLI/headless dry-run and evidence generation mode.
- Split pure IEC 61850 engine code into a class library when the public API stabilizes.

## Public credibility goals

- Add real screenshots from the compiled Windows app.
- Add sample generated PCAP file and Wireshark field screenshots.
- Add release notes with known limitations and safety wording.
- Keep landing page and README aligned with actual capabilities.

## Out of scope

- Certified protection testing.
- Calibrated analog source replacement.
- Production substation network operation.
- General-purpose IEC 61850 analyzer scope creep.
- Live subscriber detection from SV publisher alone.
- Certified IEC 61850-9-3 PTP grandmaster behavior.

## P2 full completed

- Per-phase A-G and B-C fault presets.
- Negative-sequence and zero-sequence presets.
- Per-phase VT fuse fail preset.
- CT saturation stress with DC offset, harmonic, and clipping approximation.
- Harmonic injection and DC offset transient presets.

