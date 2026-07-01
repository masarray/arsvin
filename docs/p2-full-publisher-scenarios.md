# P2 Full Publisher Scenario Engine

ARSVIN P2 keeps the product scope as an **IEC 61850 Sampled Values publisher**. It does not turn ARSVIN into an analyzer, relay model, or certified transient test set.

This stage expands the State Sequencer from balanced 3-phase presets into a richer publisher scenario engine for lab-oriented SV output.

## What P2 full adds

- Per-phase magnitude control for Ia/Ib/Ic/In and Va/Vb/Vc/Vn.
- Per-phase angle offsets for A/B/C/N.
- Single-phase A-G fault preset.
- Phase-to-phase B-C fault preset.
- Negative-sequence / unbalance preset.
- Zero-sequence / residual-current preset.
- Per-phase VT fuse-fail preset.
- CT saturation stress preset with publisher-side DC offset, harmonic, and clipping approximations.
- Harmonic injection preset.
- DC offset transient preset.
- Backward-compatible `.svpub.json` state snapshots.
- Dedicated **Waveform...** panel for manual output shaping and selected sequencer-state shaping, keeping the main window clean.

## Important boundary

The P2 scenario engine generates **publisher-side waveform approximations**. It is useful for lab readability checks, relay engineering workflows, subscriber behavior experiments, and PCAP evidence generation.

It is not a calibrated CT/VT transient model, real-time digital simulator, or conformance-certified relay test set.

## Per-phase sequencer fields

Each sequencer state still has legacy balanced fields:

- `CurrentScale`
- `VoltageScale`
- `AngleShiftDegrees`
- `FrequencyHz`

P2 adds per-phase multipliers:

- `CurrentScaleA`, `CurrentScaleB`, `CurrentScaleC`, `CurrentScaleN`
- `VoltageScaleA`, `VoltageScaleB`, `VoltageScaleC`, `VoltageScaleN`
- `AngleOffsetA`, `AngleOffsetB`, `AngleOffsetC`, `AngleOffsetN`

The actual per-channel RMS value is:

```text
current channel magnitude = CurrentScale × CurrentScaleX
voltage channel magnitude = 57.735 V × VoltageScale × VoltageScaleX
```

The actual per-channel angle is:

```text
AngleShiftDegrees + phase angle + AngleOffsetX
```

where the default phase angles are A = 0°, B = -120°, C = +120°.

## Dedicated Waveform panel

Advanced shaping is intentionally **not** placed directly on the main window. The main window keeps quick manual values, phasor view, State Sequencer, PCAP, report, and publish controls clean.

Use the ribbon **Waveform...** button to open the dedicated panel:

- **Manual output shape** edits per-channel steady-state shaping for Manual mode.
- **Selected sequencer state** edits per-phase multipliers, angle offsets, DC offset, harmonic, clipping, and scenario tag for the currently selected State Sequencer row.
- Reset buttons restore clean Manual shape or clean selected-state shape.

## Waveform shaping fields

P2 also adds lightweight publisher-side shaping:

- `CurrentDcOffsetPercent`
- `VoltageDcOffsetPercent`
- `CurrentHarmonicPercent`
- `VoltageHarmonicPercent`
- `HarmonicOrder`
- `CurrentClipPercent`
- `VoltageClipPercent`

These fields are applied during instantaneous sample generation before the SV payload is encoded.

## Presets

### 3P fault
Balanced three-phase fault: high current, low voltage, recovery.

### A-G fault
Phase-A current rises, phase-A voltage collapses, B/C remain near nominal.

### B-C fault
Phase-B and phase-C currents rise; B/C voltages depress; angle offsets emulate directional stress.

### Negative sequence
Per-phase magnitude and angle imbalance to exercise subscriber negative-sequence behavior.

### Zero sequence
Residual/neutral scenario. Neutral publishing requires In/Vn to exist in the dataset and be enabled.

### CT saturation
Publisher-side approximation using high current, DC offset, 2nd harmonic, and clipping.

### VT fuse A
Per-phase voltage collapse on phase A only.

### Harmonic injection
Adds harmonic content while preserving the fundamental phasor.

### DC offset transient
Stepwise decaying DC offset approximation.

## Evidence workflow

The P2 full scenarios work with:

- dry-run publish,
- live publish,
- generated PCAP export,
- publisher evidence report,
- TX Timing Health.

Use generated PCAP export to inspect the resulting waveform in Wireshark or offline scripts.
