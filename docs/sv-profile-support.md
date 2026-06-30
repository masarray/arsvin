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
