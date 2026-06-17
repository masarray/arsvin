# PTP and smpSynch Compatibility

ARSVIN separates two different ideas that are often mixed up during lab testing:

1. **PTP traffic visibility** — optional lab traffic that helps you see PTP-like packets on the process-bus test network.
2. **SV `smpSynch` behavior** — the value written inside the Sampled Values ASDU so a subscriber can decide whether it accepts the stream.

The built-in PTP traffic generator is for isolated lab experiments only. It is not a certified grandmaster and it is not evidence of calibrated time accuracy.

## smpSynch behavior modes

| UI mode | SV value | Meaning inside ARSVIN |
|---|---:|---|
| Global compatibility | `2` | Marks the SV stream as globally synchronized so stricter subscribers can accept point-to-point lab traffic. This is compatibility behavior, not proof of real PTP accuracy. |
| Local compatibility | `1` | Marks the stream as locally synchronized for subscribers that accept local lab synchronization. |
| Honest unsynchronized | `0` | Publishes the stream as not synchronized. Use this when you want the frame to be semantically conservative. |
| External PTP auto | `0`, `1`, or `2` | Derives `smpSynch` from observed external PTP health. If no valid PTP evidence is visible, ARSVIN does not claim global synchronization. |

## Recommended default for point-to-point relay checks

For practical relay readability checks, use:

```text
smpSynch: Global compatibility — smpSynch=2
PTP traffic: Off or LabPublisher, depending on what the target relay expects to see
```

This mode exists because some subscribers reject SV streams that do not report synchronized status. The label intentionally says **compatibility** so it is clear that ARSVIN is helping the subscriber accept the stream, not certifying real time synchronization.

## When to use External PTP auto

Use **External PTP auto** when the lab has a real external PTP source and you want ARSVIN to behave more conservatively.

In this mode, ARSVIN looks at observed PTP health and chooses the SV `smpSynch` value based on the current evidence. If no valid PTP evidence is visible, the stream falls back to unsynchronized behavior instead of silently claiming global sync.

## Safety boundary

Compatibility modes are intended for isolated point-to-point testing, lab visibility checks, and process-bus experiments.

They should not be used as proof of:

- calibrated PTP accuracy
- relay timing performance
- protection trip-time acceptance
- FAT / SAT synchronization compliance
