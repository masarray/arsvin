# ARSVIN — IEC 61850 Sampled Values Publisher & Process Bus Traffic Tester

[![CI](https://github.com/masarray/arsvin/actions/workflows/ci.yml/badge.svg)](https://github.com/masarray/arsvin/actions/workflows/ci.yml)
[![Release](https://github.com/masarray/arsvin/actions/workflows/release.yml/badge.svg)](https://github.com/masarray/arsvin/actions/workflows/release.yml)
[![CodeQL](https://github.com/masarray/arsvin/actions/workflows/codeql.yml/badge.svg)](https://github.com/masarray/arsvin/actions/workflows/codeql.yml)
[![License](https://img.shields.io/badge/license-Apache--2.0-blue.svg)](LICENSE)

<p align="center">
  <img src="site/assets/arsvin.png" alt="ARSVIN product icon" width="160" />
</p>

**ARSVIN** is a lightweight IEC 61850 **Sampled Values publisher simulator** for engineers who need to publish SV traffic from Windows, validate whether a relay can subscribe to it, and experiment with process-bus communication in isolated lab setups.

It combines **SCL-based SV stream setup**, **manual and sequenced value publishing**, **COMTRADE replay**, and **lab PTP traffic and explicit smpSynch compatibility modes** in one focused desktop tool.

> ARSVIN is built for **lab publishing, point-to-point relay readability checks, process-bus experiments, and engineering education**. It is **not** a certified protection test set, a calibrated current/voltage source, or a calibrated PTP grandmaster.

## What ARSVIN is good for

- Publishing IEC 61850 Sampled Values traffic from a Windows workstation
- Checking whether a relay or subscriber can detect and subscribe to SV traffic
- Loading SCL and reusing APPID, MAC, VLAN, svID, and dataset information
- Replaying COMTRADE analog records as Sampled Values
- Running manual, ramp, and state-sequenced SV simulation workflows
- Generating lab-only PTP traffic while explicitly controlling `smpSynch` compatibility behavior
- Verifying stream settings in Wireshark during point-to-point lab tests

## What ARSVIN is not

- Not a certified relay test set
- Not a calibrated protection commissioning tool
- Not intended for closed-loop trip-time validation
- Not intended to replace Omicron-class or HIL / RTDS / real-time test platforms
- Not guaranteed to provide deterministic real-time behavior under standard Windows scheduling

## Highlights

- **Focused SV publishing workflow** — keep the tool narrow, understandable, and useful for process-bus lab work
- **Up to three SV publishers** — simulate multiple logical sources from one workstation
- **Independent publisher settings** — separate values, phasors, APPID, VLAN, MAC, svID, dataset, and scaling per publisher
- **SCL-based setup** — avoid manual entry mistakes by selecting Sampled Values streams directly from SCL
- **Manual, ramp, and sequencer modes** — use continuous output, state-based ramping, or timed state sequences
- **COMTRADE replay** — replay ASCII, BINARY, BINARY32, and FLOAT32 analog COMTRADE records as Sampled Values
- **PTP / smpSynch compatibility options** — keep lab timing traffic separate from explicit `smpSynch` behavior for subscriber readability testing
- **Portable Windows release** — build a self-contained win-x64 package for easy lab deployment
- **Apache-2.0** — open-source and permissive

## Who it is for

- Protection and substation automation engineers
- IEC 61850 R&D and integration teams
- Digital substation labs and engineering classrooms
- Developers building IEC 61850 tooling
- Engineers validating SCL-derived process-bus settings

## Quick start

1. Install **Npcap** on Windows.
2. Download the latest `ARSVIN-win-x64-portable.zip` from Releases.
3. Run `ARSVIN.exe` as Administrator when using live packet publishing.
4. Open an SCL file.
5. Select Publisher 1, 2, or 3.
6. Select an SV stream, enter manual values, configure a ramp / sequencer, or import COMTRADE.
7. Start a dry run or live publish session.
8. Verify relay subscription behavior or inspect traffic in Wireshark.

See [Quick Start](docs/quick-start.md).

## Build from source

Requirements:

- Windows 10/11
- .NET 8 SDK
- Npcap for live Ethernet publishing

```powershell
cd arsvin
.\build.ps1
```

Create a portable package:

```powershell
.\publish-win-x64.ps1
```

## Documentation

- [Quick Start](docs/quick-start.md)
- [Multi-Stream SV Publishing](docs/multi-stream.md)
- [COMTRADE Replay](docs/comtrade-replay.md)
- [Publisher Session Engine](docs/publisher-session-engine.md)
- [PTP and smpSynch Compatibility](docs/ptp-and-smpsynch.md)
- [Sync Compatibility Mode](docs/sync-compatibility-mode.md)
- [Build and Release](docs/build-and-release.md)
- [Live Mode Safety](docs/live-mode-safety.md)
- [Live Preflight Diagnostics](docs/live-preflight-diagnostics.md)
- [Known Limitations](docs/known-limitations.md)
- [Safety Boundaries](docs/safety-boundaries.md)

## Repository topics

Recommended GitHub topics:

`iec61850`, `sampled-values`, `sv-publisher`, `sv-injector`, `process-bus`, `comtrade`, `digital-substation`, `ptp`, `wpf`, `dotnet`

## Author

Created and maintained by **Ari Sulistiono**.

GitHub: [github.com/masarray](https://github.com/masarray)

## License

Apache License 2.0. See [LICENSE](LICENSE).
