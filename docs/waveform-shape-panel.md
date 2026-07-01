# Waveform Shape Panel

The **Waveform...** ribbon button opens a dedicated advanced waveform-shaping window. This keeps ARSVIN's main window focused on publisher setup, manual values, State Sequencer, preflight, PCAP export, evidence report, and live/dry-run publish controls.

## Manual output shape

Manual output shaping is applied only when ARSVIN publishes from Manual mode. Each channel can carry optional publisher-side shaping metadata:

- DC offset percent
- harmonic percent
- harmonic order
- clip percent

Default values are clean:

```text
DC offset = 0%
harmonic = 0%
harmonic order = 2
clip = 100%
```

The main-window waveform plot now uses the same manual shaping formula as the publisher output, so harmonic injection, DC offset, and clipping are visible in the preview instead of being hidden behind a clean sine-wave display.

## Selected sequencer state

The second tab edits the currently selected State Sequencer row. It exposes the deeper P2 scenario fields without adding bulky controls to the main window:

- per-phase current multipliers
- per-phase voltage multipliers
- per-phase angle offsets
- current/voltage DC offset
- current/voltage harmonic percent
- harmonic order
- current/voltage clipping
- scenario tag

## Boundary

These controls are publisher-side lab approximations. They are useful for SV stream stress, relay readability checks, subscriber behavior experiments, and generated PCAP evidence. They are not calibrated CT/VT transient models or conformance-certified relay-test waveforms.
