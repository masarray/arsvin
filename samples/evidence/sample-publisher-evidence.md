# ARSVIN Sampled Values Publisher Evidence Report

Generated: 2026-07-01 09:00:00 +07:00
Tool: ARSVIN 0.1.0
SCL: `samples/scl/02_SV_Stream_4I+4V_nofASDU8.scd`
Adapter: Ethernet 1
Mode: Manual; continuous=True; duration=1s
TX timing: TX Timing: GOOD act=1600.0/1600.0fps jitter=8/42us late=0 missed=0 send=6/18us maxLate=20us

> This report is TX-side publisher evidence. It is not a network analyzer capture, not a calibrated measurement certificate, and not IEC 61850 conformance certification.

## Summary

- Enabled publishers: 1
- Fatal findings: 0
- Warnings: 0
- Info: 5
- Safety boundary: Lab publisher / TX-side evidence only; not an analyzer and not a certified merging unit.

## Publisher streams

| Slot | Status | svID | APPID | Destination | VLAN | nofASDU | Sample rate | Publish rate | Payload | Bandwidth | Quality | Source |
|---|---|---|---|---|---|---:|---:|---:|---:|---:|---|---|
| IED / MU 1 | ready | MU01SV01 | 0x4000 | 01:0C:CD:04:00:01 | VID=100/PCP=4 | 8 | 12800 | 1600 | 64 | 9216 kbps | good | Manual phasor |

Use this sample as a shape reference only. Generate real reports from the ARSVIN **Report** button.
