# ArSubsv Field Mode P4

Field Mode is the practical receiver workflow for live IEC 61850 Sampled Values and offline PCAP/PCAPNG analysis.

The P4 application branch is rebuilt directly on merged `main` and pins the field-core engine by its exact commit SHA. The draft PR therefore contains only Field Mode changes and remains safe to test locally without merging.

## Five evidence axes

ArSubsv separates:

- `CAPTURE`: frames available to the application;
- `PROTOCOL`: Ethernet/SV APDU decoding;
- `STREAM`: sample-counter continuity and payload consistency;
- `CONFIGURATION`: observed traffic versus bound CID/SCD;
- `MEASUREMENT`: channel semantics, scaling, CT/VT context, and signal confidence.

A configuration mismatch does not make a clean protocol stream BAD. Unknown measurement semantics remain UNKNOWN rather than being presented as amperes or volts.

## Offline workflow

1. Open `.pcap` or `.pcapng`.
2. Select a discovered stream.
3. Review raw decoded values and continuity without SCL.
4. Import `.cid`, `.scd`, `.icd`, or `.iid` when available.
5. Review scored SCL binding and expected-versus-observed findings.
6. Enter explicit CT/VT context only from reviewed evidence.
7. Export a support bundle for reproducible review.

## Support bundle

The initial P4 bundle is metadata-only by default and contains:

- manifest and SHA-256 checksums;
- receiver evidence in Markdown and JSON;
- five-axis field summary;
- selected-stream diagnostics;
- measurement context when configured;
- application and engine revision evidence;
- SCL hash when an SCL file is loaded.

The full capture and original SCL are not copied silently. Capture-excerpt and privacy-selection UI are planned as the next tranche.

## Current limitations

- Known-injection comparison exists in ARIEC61850 but the ArSubsv entry dialog is not yet implemented.
- Quiet/noise-floor classification is evidence-based and non-destructive, but waveform minimum-axis presentation still needs local UI review.
- Real Ahmed Mohaisn PCAPNG/CID replay remains a local acceptance gate; deterministic tests do not replace real-device evidence.
