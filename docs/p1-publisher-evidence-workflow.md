# P1 Publisher Evidence Workflow

ARSVIN remains an IEC 61850 Sampled Values publisher. P1 adds evidence features that help an engineer prove what ARSVIN intends to transmit without turning the application into a network analyzer.

## What P1 adds

- **Publisher evidence report**: exports a Markdown report containing the enabled publisher streams, SCL-derived identity, operator overrides, sample rate, publication rate, `nofASDU`, payload length, estimated bandwidth, quality mode, synchronization marking, and preflight findings.
- **SCL validation evidence**: the report includes publisher-side SCL checks for APPID, destination MAC, svID, dataset, `confRev`, `smpRate`, `nofASDU`, and payload support.
- **TX-side boundary statement**: the report explicitly states that it is publisher evidence only, not a capture analyzer and not conformance certification.
- **Recommended external verification**: the report tells the operator to export generated PCAP and inspect it with an independent packet dissector or process-bus analyzer.

## Intended usage

1. Open an SCL file.
2. Select and enable one or more IED / MU publisher slots.
3. Set source MAC, APPID, destination MAC, VLAN, sample rate, `nofASDU` from SCL, dLSB, signal source, and quality mode.
4. Click **Check** to review blocking errors and warnings.
5. Click **PCAP** to export generated frames for offline inspection.
6. Click **Report** to export the Markdown publisher evidence report.
7. Use the report and PCAP together during lab setup, relay subscription checks, or public issue reporting.

## Scope boundary

P1 does not add a subscriber, network scanner, GOOSE/SV analyzer, or live configuration-versus-wire mismatch engine. The report is generated from ARSVIN's configured TX plan and validation results.

## Current limitations

- Timing remains Windows/Npcap lab-grade unless externally verified.
- The report uses the selected publisher configuration and preflight findings. It does not prove what a remote subscriber actually received.
- Formal IEC 61850 conformance still requires a qualified test procedure and lab.
