# Sampled Values Profile Support

ARSVIN is an IEC 61850 Sampled Values publisher for lab and engineering use. It is not an accredited conformance test tool and not a calibrated merging unit.

| Capability | Status |
|---|---|
| Ethernet Sampled Values EtherType `0x88BA` | Supported |
| APPID / length / reserved process-bus header | Supported |
| VLAN tagging | Supported |
| SCL-driven `SampledValueControl` import | Supported |
| `svID` / `smvID` | Supported |
| `datSet` reference | Supported |
| `confRev` | Supported |
| `smpCnt` | Supported |
| `smpSynch` compatibility modes | Supported |
| `smpRate` / `smpMod` emission | Supported |
| `nofASDU=1` | Supported |
| `nofASDU=2/4/8` | Supported |
| `nofASDU>8` | Blocked by preflight |
| Quality bits | Engine foundation, good quality default in UI |
| COMTRADE replay | Supported for mapped analog channels |
| Generated PCAP export | Engine foundation |
| Certified IEC/IEEE 61850-9-3 PTP | Not claimed |
| Formal UCAIug conformance | Not claimed |

## Publisher-only evidence features

ARSVIN can export generated SV frames to PCAP for offline verification. The export path creates frames from the configured publisher plan and writes them to disk; it does not sniff or analyze traffic from the process bus.

Quality field presets are available for intentional relay-behavior tests:

- good
- invalid
- questionable
- oldData
- test
- operatorBlocked

Use non-default quality presets only in isolated lab tests and document the intent in the FAT/SAT notes.

## TX Timing Health

ARSVIN reports publisher-side timing health while publishing. This is local TX-loop measurement only; it does not capture live process-bus traffic.

| Metric | Meaning |
|---|---|
| Target FPS | Expected Ethernet frame rate after `nofASDU` packing. |
| Actual FPS | Frame rate observed by the local publisher loop. |
| Jitter avg/max | Difference between actual frame interval and target interval. |
| Late frames | Frames that started sending later than the local threshold. |
| Missed schedule | Frames that were late by more than one target interval. |
| Send avg/max | Duration around the local transport send call. |

Timing Health is intended to make Windows/Npcap limitations visible. It is not a claim of IEC/IEEE 61850-9-3 timing compliance.
