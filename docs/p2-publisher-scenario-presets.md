# P2 Publisher Scenario Presets

ARSVIN P2 adds a publisher-side **Scenario Preset** workflow for the existing State Sequencer workspace.

This is intentionally **not an analyzer** and not a certified transient simulator. It is a fast way to generate repeatable Sampled Values output patterns from the publisher for lab checks, relay behavior trials, classroom demos, and offline evidence generation.

## UI workflow

1. Open ARSVIN.
2. Select or import an SCL stream.
3. Open the **Sequence** workspace.
4. Choose a scenario from the new scenario preset drop-down.
5. Click **Scenario**.
6. Review or edit the generated state cards.
7. Run dry/live publish, export PCAP, or export publisher evidence report.

## Included presets

| Preset | Purpose | Notes |
|---|---|---|
| Protection fault | Prefault / fault / recovery | Balanced 3-phase protection scenario. |
| CT saturation stress | High-current stress approximation | Does not model clipped CT waveform distortion. |
| VT fuse / undervoltage | 3-phase undervoltage approximation | Current State Sequencer does not model per-phase voltage collapse yet. |
| Frequency steps | 49 to 51 Hz | Discrete steps, not a continuous frequency ramp. |
| Phase jump | ±20 degrees | Balanced phasor phase-jump scenario. |
| Load reversal | 180 degree shift | Directional element lab approximation. |

## Safety boundary

The presets are publisher-side state patterns. They do not prove that a subscriber received the stream, and they do not replace a calibrated relay test set, conformance test, or IEC/IEEE 61850-9-3 synchronized merging unit.

## Why this belongs in ARSVIN publisher

A publisher should help the operator create credible output streams quickly. Scenario presets keep the product focused on publishing while avoiding analyzer scope creep.


## P2 full update

This older P2 preset note is superseded by the full scenario engine documentation:

- [P2 Full Publisher Scenario Engine](p2-full-publisher-scenarios.md)

The full P2 engine adds per-phase multipliers, per-phase angle offsets, A-G/B-C fault presets, negative/zero-sequence presets, per-phase VT fuse fail, harmonic injection, DC offset transient, and CT saturation stress using publisher-side DC/harmonic/clipping approximations.
