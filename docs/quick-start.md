# Quick Start

ARSVIN contains two focused Windows applications:

- **ARSVIN Publisher** for generating IEC 61850 Sampled Values.
- **ArSubsv Subscriber** for live/PCAP capture, decoding, visualization, and receiver-side evidence.

## 1. Install or run portable

### Installer

Download `ARSVIN-Suite-Setup-win-x64.exe` from the latest GitHub Release and install the suite. Start Menu shortcuts are created for Publisher and Subscriber.

### Portable

Download either:

- `ARSVIN-Publisher-win-x64.exe`
- `ArSubsv-Subscriber-win-x64.exe`

The applications are self-contained single-file Windows x64 executables.

## 2. Install live-network prerequisite

Install Npcap separately from its official website when live Ethernet capture or transmission is required.

Npcap is not bundled or silently installed by ARSVIN. Offline PCAP import, configuration, documentation, and Publisher dry-run workflows do not require live network access.

## 3. Use an authorized test environment

For live traffic, use only:

- an isolated laboratory network,
- a point-to-point engineering link, or
- another network where you have explicit authorization.

Administrator rights may be required by the local Npcap/network configuration.

# Publisher workflow

## 4. Open SCL

Open **ARSVIN Publisher**, then use **Config** to load an SCL/SCD file and select an SV stream.

Imported engineering data can provide APPID, VLAN, destination MAC, `svID`, dataset reference, `confRev`, sample rate, `smpMod`, and `nofASDU`.

## 5. Select a publisher slot

Switch between Publisher 1, Publisher 2, and Publisher 3. Each slot can use its own stream, APPID, MAC, VLAN, sample rate, source mode, and values.

## 6. Choose a source mode

Available workflows include:

- **Manual Continue** — continuous steady-state publishing.
- **Ramp** — magnitude/angle changes using configured timing.
- **Sequencer** — timed states and repeatable per-phase scenarios.
- **COMTRADE Replay** — analog record replay as Sampled Values.
- **Waveform shaping** — harmonic, DC-offset, clipping, and related laboratory scenarios.

## 7. Review and preflight

Before live transmission, confirm:

- selected adapter,
- destination and source MAC,
- APPID,
- VLAN ID and priority,
- `svID`, dataset, and `confRev`,
- sample rate and `nofASDU`,
- expected `smpCnt` progression,
- selected quality mode,
- `smpSynch` compatibility behavior.

Run a dry test first. Warnings remain visible but do not block valid laboratory traffic; fatal configuration errors do.

## 8. Publish and preserve evidence

Start live publishing only after preflight and network authorization.

Use TX Timing Health to inspect target/actual FPS, jitter, late frames, missed schedules, and send duration. Export generated PCAP and the Markdown publisher report when evidence is needed.

# Subscriber workflow

## 9. Select live capture or PCAP import

Open **ArSubsv Subscriber** and choose:

- a live Npcap adapter, or
- a classic PCAP file for offline analysis.

When SCL is available, load it to assist stream binding and expected-configuration checks.

## 10. Inspect the stream

Review:

- discovered streams,
- APPID, VLAN, `svID`, and `confRev`,
- `nofASDU`, sample rate, and payload layout,
- `smpCnt` continuity and stream health,
- decoded values,
- waveform,
- phasor and RMS views.

Export the receiver-side Markdown report when a repeatable record is required.

## 11. Verify independently

Use Wireshark, relay diagnostics, vendor tools, or certified equipment when independent verification is required.

Useful packet checks include:

- Ethernet type `0x88BA`,
- correct multicast destination MAC,
- correct APPID and VLAN tag,
- expected `svID`, dataset, and `confRev`,
- expected ASDU packing and sample counter behavior.

Publisher evidence shows what the PC generated. Subscriber evidence shows what the selected PC/NIC received and decoded. Neither alone proves that a protection IED consumed the multicast stream.

## Sync compatibility note

For point-to-point relay readability checks, **Global compatibility — `smpSynch=2`** can be a practical starting point for strict subscribers. It does not prove IEC 61850-9-3 timing accuracy or make the Windows PC a certified PTP grandmaster.
