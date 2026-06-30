# ARSVIN — IEC 61850 Sampled Values Publisher & Process Bus Traffic Tester

[![CI](https://github.com/masarray/arsvin/actions/workflows/ci.yml/badge.svg)](https://github.com/masarray/arsvin/actions/workflows/ci.yml)
[![Release](https://github.com/masarray/arsvin/actions/workflows/release.yml/badge.svg)](https://github.com/masarray/arsvin/actions/workflows/release.yml)
[![CodeQL](https://github.com/masarray/arsvin/actions/workflows/codeql.yml/badge.svg)](https://github.com/masarray/arsvin/actions/workflows/codeql.yml)
[![License](https://img.shields.io/badge/license-Apache--2.0-blue.svg)](LICENSE)
[![Platform](https://img.shields.io/badge/platform-Windows-0078D4.svg)](#requirements)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4.svg)](#build-from-source)

<p align="center">
  <img src="site/assets/arsvin.png" alt="ARSVIN product icon" width="156" />
</p>

**ARSVIN** is a focused Windows desktop tool for IEC 61850 engineers who need to publish **Sampled Values (SV)** traffic, reuse SCL-derived stream settings, replay COMTRADE analog records, and perform relay readability checks in isolated lab networks.

It is intentionally narrow: **SV publishing, process-bus traffic experiments, SCL setup, COMTRADE replay, and lab-only timing compatibility checks**.

> [!WARNING]
> ARSVIN can transmit raw Ethernet frames. Use it only on isolated lab networks, point-to-point test links, or networks where you have explicit authorization. It is **not** a certified protection test set, calibrated current/voltage source, or calibrated PTP grandmaster.

## Why this project exists

Commercial relay test sets are excellent, but engineers often need a transparent, lightweight, source-available tool for learning, lab experiments, SCL sanity checks, and low-risk relay subscription tests. ARSVIN aims to make those workflows easier to inspect and improve.

## What ARSVIN is good for

- Publishing IEC 61850 Sampled Values traffic from a Windows workstation
- Checking whether a relay or subscriber can detect and subscribe to SV traffic
- Loading SCL and reusing APPID, MAC, VLAN, `svID`, and dataset information
- Replaying COMTRADE analog records as Sampled Values
- Running manual, ramp, and state-sequenced SV simulation workflows
- Generating lab-only PTP traffic while explicitly controlling `smpSynch` compatibility behavior
- Verifying stream settings in Wireshark during point-to-point lab tests

## What ARSVIN is not

- Not a certified relay test set
- Not a calibrated protection commissioning tool
- Not intended for closed-loop trip-time validation
- Not intended to replace Omicron-class, HIL, RTDS, or real-time protection test platforms
- Not guaranteed to provide deterministic real-time behavior under standard Windows scheduling
- Not intended for live substation networks or production process-bus networks

## Highlights

| Area | Capability |
| --- | --- |
| SV publishing | Up to three independent SV publisher slots |
| SCL setup | Select stream parameters from SCL instead of retyping APPID, MAC, VLAN, `svID`, and dataset data |
| Signal modes | Manual values, balanced defaults, ramp states, and timed state sequencer |
| COMTRADE | Replay ASCII, BINARY, BINARY32, and FLOAT32 analog records as SV |
| Timing compatibility | Lab PTP traffic and explicit `smpSynch` compatibility behavior |
| Diagnostics | Live preflight checks for common publishing risks |
| Delivery | Portable self-contained Windows release package |
| License | Apache License 2.0 |

## Requirements

For users:

- Windows 10/11 x64
- [Npcap](https://npcap.com/) for live Ethernet publishing
- Administrator rights when transmitting live packets
- Wireshark or equivalent packet analyzer for verification

For developers:

- Windows 10/11 x64
- .NET 8 SDK
- PowerShell 7+ recommended
- Visual Studio 2022, Rider, or VS Code with C# tooling

## Quick start

1. Install Npcap on Windows.
2. Download the latest `ARSVIN-win-x64-portable.zip` from Releases.
3. Extract the ZIP.
4. Run `ARSVIN.exe` as Administrator when using live packet publishing.
5. Open an SCL file.
6. Select Publisher 1, 2, or 3.
7. Select an SV stream, enter manual values, configure a ramp / sequencer, or import COMTRADE.
8. Start a dry run or live publish session.
9. Verify relay subscription behavior or inspect traffic in Wireshark.

See [Quick Start](docs/quick-start.md).

## Build from source

```powershell
git clone https://github.com/masarray/arsvin.git
cd arsvin
.\build.ps1
```

Create a portable package:

```powershell
.\publish-win-x64.ps1
```

Run tests:

```powershell
dotnet test tests/ARSVIN.Tests/ARSVIN.Tests.csproj -c Release
```

## Repository structure

```text
src/ARSVIN/                 WPF desktop application and IEC 61850 engine code
tests/ARSVIN.Tests/         Unit tests for stable protocol primitives
docs/                       Engineering documentation and safety notes
samples/                    Small SCL and COMTRADE samples for lab/demo use
site/                       Static GitHub Pages landing page
.github/workflows/          CI, CodeQL, GitHub Pages, and release automation
```

See [Architecture](docs/architecture.md) and [Public Release Checklist](docs/public-release-checklist.md).

## Documentation

- [Quick Start](docs/quick-start.md)
- [Architecture](docs/architecture.md)
- [Multi-Stream SV Publishing](docs/multi-stream.md)
- [COMTRADE Replay](docs/comtrade-replay.md)
- [Publisher Session Engine](docs/publisher-session-engine.md)
- [PTP and smpSynch Compatibility](docs/ptp-and-smpsynch.md)
- [Sync Compatibility Mode](docs/sync-compatibility-mode.md)
- [Build and Release](docs/build-and-release.md)
- [Live Mode Safety](docs/live-mode-safety.md)
- [Modern SV Setup UX](docs/modern-sv-setup-ux.md)
- [Live Preflight Diagnostics](docs/live-preflight-diagnostics.md)
- [Known Limitations](docs/known-limitations.md)
- [Safety Boundaries](docs/safety-boundaries.md)
- [Repository Audit](docs/repository-audit.md)

## Safety and responsible use

ARSVIN is built for engineering learning and controlled lab workflows. Do not connect it to production substation LANs, process-bus networks, or equipment under service unless you have authorization and a complete risk assessment. Prefer dry run mode before live publishing.

Report security concerns using [SECURITY.md](SECURITY.md).

## Recommended GitHub topics

`iec61850`, `sampled-values`, `sv-publisher`, `sv-injector`, `process-bus`, `comtrade`, `digital-substation`, `ptp`, `wpf`, `dotnet`, `substation-automation`

## Contributing

Practical engineering contributions are welcome. Please read [CONTRIBUTING.md](CONTRIBUTING.md), open focused pull requests, include a short test note, and keep safety wording honest.

## Author

Created and maintained by **Ari Sulistiono**.

GitHub: [github.com/masarray](https://github.com/masarray)

## License

Apache License 2.0. See [LICENSE](LICENSE).

Third-party dependency notes are listed in [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).
