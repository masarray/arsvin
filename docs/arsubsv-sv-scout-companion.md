# ArSubsv — IEC 61850 Sampled Values Scout Companion

ArSubsv is the receiver-side companion application for ARSVIN. It is designed to make IEC 61850 Sampled Values visible during lab publishing, commissioning preparation, and publisher verification.

ArSubsv uses its own branding and interface. Its engineering purpose is to discover SV streams, subscribe to one or more streams, visualize current and voltage waveforms, calculate RMS and phase indicators, compare stream timing and counters, and export receiver-side evidence.

## Feature set

- Live Npcap capture for IEC 61850 Sampled Values EtherType `0x88BA`.
- Automatic stream discovery from live traffic.
- Optional SCL/SCD import for stream binding and dataset decoding.
- Classic PCAP import/replay for offline verification.
- Stream health table with APPID, svID, VLAN, `nofASDU`, FPS, `smpCnt`, SCL binding, and issue counters.
- Oscilloscope view for decoded current and voltage traces.
- Phasor view with RMS, peak, and estimated angle.
- Decoded value table with raw bytes.
- Frame detail tab for APPID, dataset, confRev, sample rate, `smpSynch`, source, and destination.
- Markdown report export for receiver-side evidence.

## Verification boundaries

ArSubsv verifies that this PC and selected NIC receive and decode Sampled Values. It cannot prove that another relay, protection IED, BCU, or fault recorder has consumed the same multicast stream.

SCL binding improves decoding and validation quality. Without SCL, ArSubsv can still discover and count streams, but waveform, phasor, and value visualization is limited because the payload layout is not known.

## Capture file support

The first offline mode supports classic `.pcap` files with Ethernet frames. PCAPNG is not supported yet.

## UI philosophy

The UI intentionally follows the ARSVIN Publisher visual family but uses a dedicated scout workflow:

1. Select adapter or open PCAP.
2. Import SCL/SCD if payload decoding is required.
3. Start capture or process file.
4. Select a discovered stream.
5. Review Scope, Phasor, Values, Frame, and Diagnostics tabs.
6. Export a report.
