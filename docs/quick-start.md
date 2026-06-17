# Quick Start

## 1. Install prerequisites

- Windows 10/11
- Npcap for live Ethernet publishing
- Administrator rights for live packet transmission
- A lab network or isolated point-to-point setup when using live publish mode

## 2. Start ARSVIN

Run `ARSVIN.exe`.

For live packet publishing, launch it as **Administrator**.

## 3. Open SCL

Use **Config** to open an SCL file and select an SV stream.

The imported stream can provide APPID, VLAN, destination MAC, `svID`, dataset reference, and other network details.

## 4. Select a publisher slot

Use the Publisher selector to switch between:

- Publisher 1
- Publisher 2
- Publisher 3

Each publisher can have its own stream, APPID, MAC, VLAN, sample rate, and values.

## 5. Choose a publishing workflow

ARSVIN supports several practical lab workflows:

- **Manual Continue** — continuous steady-state publishing until stopped
- **Ramp** — state-based magnitude / angle changes using the configured ramp timing
- **Sequencer** — timed state sequence publishing using the configured state durations
- **COMTRADE Replay** — replay analog records as Sampled Values

## 6. Review network settings

Before using live mode, confirm:

- selected adapter
- destination MAC
- source MAC
- APPID
- VLAN ID and priority
- sample rate / `smpCnt` progression expectation
- selected `smpSynch` behavior / compatibility mode

## 7. Publish

Use **Check** for optional live diagnostics, then **Start** for live publishing. ARSVIN uses a KM Looptest friendly preflight model: warnings do not block live publish, but fatal errors still stop invalid traffic.

You can also run a dry test for validation without transmitting on the network.

## 8. Verify

Use Wireshark and the relay / subscriber status to confirm the stream is visible and readable.

Useful checks include:

- Ethernet type `0x88BA`
- correct destination multicast MAC
- correct APPID
- correct VLAN tag
- expected `svID`
- expected `smpSynch` behavior, especially whether the relay requires global compatibility mode

## Sync compatibility note

For point-to-point relay readability checks, **Global compatibility — smpSynch=2** is usually the most practical starting point. It helps strict subscribers accept the SV stream, but it does not prove real PTP timing accuracy.


## 9. Use the modern SV setup workflow

The **SV Setup** window is organized for quick looptest work:

- use the left **SV streams** navigator to select SV1, SV2, or SV3
- edit the selected stream in the main panel
- choose **Manual**, **Ramp**, **Sequence**, or **COMTRADE** source mode
- press **Check** to open the Preflight Results window when you need details

Warnings are shown in the Preflight Results window and event log. They do not block live publishing. Only fatal configuration errors block live publishing.
