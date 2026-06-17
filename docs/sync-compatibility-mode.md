# Sync Compatibility Mode

ARSVIN includes explicit `smpSynch` compatibility modes because many relay / subscriber implementations will not process Sampled Values unless the stream reports an acceptable synchronization state.

This is a practical lab feature for point-to-point readability checks.

## Design rule

ARSVIN must never hide the difference between:

- **compatibility marking** — writing `smpSynch=1` or `smpSynch=2` so a subscriber accepts the stream
- **real timing proof** — verified and calibrated PTP synchronization accuracy

That is why the UI uses labels such as:

```text
Global compatibility — smpSynch=2
Local compatibility — smpSynch=1
Honest unsynchronized — smpSynch=0
External PTP auto — monitor based
```

## Practical field use

For isolated relay point-to-point checks, start with **Global compatibility**. This makes the stream more likely to be accepted by stricter subscribers.

For conservative engineering diagnostics, use **Honest unsynchronized** or **External PTP auto**.

## PTP traffic is separate

The PTP traffic setting controls whether ARSVIN transmits lab PTP-like traffic.

It does not automatically mean the SV stream is accurately synchronized. The SV `smpSynch` behavior is controlled by the selected `smpSynch` mode.
