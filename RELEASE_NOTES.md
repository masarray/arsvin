# Release Notes

## Current development line

ARSVIN is an IEC 61850 Sampled Values Publisher and Subscriber suite for Windows.

Current public revisions are licensed under `GPL-3.0-or-later`. A separate negotiated commercial path is available for proprietary integration, OEM or white-label distribution, closed-source redistribution, private branches, and contractual engineering services. The commercial notice grants no additional rights by itself.

Historical revisions through `9440f08b6909ef2dc93dd483cfdcb4e1e86077d0` remain available under Apache-2.0 on `archive/apache-2.0-final`.

### Current capability summary

- SCL-assisted Sampled Values stream configuration.
- Up to three Publisher slots with multi-ASDU packing.
- Manual, scenario, waveform-shaped, and COMTRADE sources.
- Generated PCAP, timing-health, and transmitter evidence.
- ArSubsv live Npcap and offline PCAP reception.
- Stream discovery, decoding, continuity diagnostics, waveform, phasor, RMS, and receiver evidence.
- Locked dependency graph, deterministic tests, coverage gates, SBOM, checksums, and artifact attestations.

### Operational boundary

ARSVIN can capture and transmit raw Ethernet frames. Use live features only under explicit authorization and an appropriate test boundary. The software is not calibrated deterministic real-time equipment, a formal IEC 61850 conformance platform, a functional-safety system, cybersecurity-certified equipment, or proof that another IED consumed or acted on a multicast stream.

See [CHANGELOG.md](CHANGELOG.md), [docs/LICENSING.md](docs/LICENSING.md), and [docs/known-limitations.md](docs/known-limitations.md).