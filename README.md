<div align="center">

# ARSVIN

### IEC 61850 Sampled Values Publisher, Subscriber & Evidence Workbench for Windows

**SCL-driven stream generation, COMTRADE replay, live and PCAP analysis, waveform, phasor, RMS, continuity diagnostics, and repeatable engineering evidence.**

[![CI](https://github.com/masarray/arsvin/actions/workflows/ci.yml/badge.svg)](https://github.com/masarray/arsvin/actions/workflows/ci.yml)
[![Release](https://github.com/masarray/arsvin/actions/workflows/release.yml/badge.svg)](https://github.com/masarray/arsvin/actions/workflows/release.yml)
[![CodeQL](https://github.com/masarray/arsvin/actions/workflows/codeql.yml/badge.svg)](https://github.com/masarray/arsvin/actions/workflows/codeql.yml)
[![License](https://img.shields.io/badge/license-GPL--3.0--or--later-2563eb.svg)](LICENSE)
[![Platform](https://img.shields.io/badge/platform-Windows%2010%2F11-0078D4.svg)](#system-requirements)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4.svg)](#build-from-source)

[**Download**](https://github.com/masarray/arsvin/releases/latest) · [**Product site**](https://masarray.github.io/arsvin/) · [**Quick start**](docs/quick-start.md) · [**Documentation**](docs/index.md) · [**Licensing**](docs/LICENSING.md)

</div>

<p align="center">
  <img src="site/assets/arsvin-publisher-sv-setup.webp" alt="ARSVIN Publisher Sampled Values setup with SCL stream selection, source modes, network settings, and manual output controls" width="920" />
</p>

> [!CAUTION]
> ARSVIN can capture and transmit raw Ethernet traffic. Use live features only on an isolated laboratory network, an authorized point-to-point test link, or another network for which you have explicit authority. ARSVIN is not a certified relay test set, calibrated merging unit, deterministic real-time platform, functional-safety system, production process-bus monitor, or IEC 61850 conformance certificate.

## Engineering purpose

ARSVIN is a focused Windows suite for practical IEC 61850 Sampled Values work. It combines:

- **ARSVIN Publisher** for SCL-assisted stream configuration, manual and scenario-based generation, waveform shaping, COMTRADE replay, timing-health visibility, and transmitter-side evidence; and
- **ArSubsv Subscriber** for live or PCAP stream discovery, decoding, SCL-assisted binding, waveform, phasor, RMS, continuity diagnostics, and receiver-side evidence.

Publisher evidence shows what the selected computer generated. Subscriber evidence shows what the selected computer and network adapter received and decoded. Neither proves that another IED consumed, trusted, or acted on a multicast stream.

## Real application views

| ArSubsv stream monitor | ArSubsv live analysis |
|---|---|
| <img src="site/assets/arsvin-subscriber-waveform-phasor.webp" alt="ArSubsv stream monitor with discovered Sampled Values streams, voltage and current waveforms, phasor view, and RMS values" width="100%" /> | <img src="site/assets/arsvin-subscriber-live-analysis.webp" alt="ArSubsv live analysis workspace with channel table, phasor display, voltage waveform, and current waveform" width="100%" /> |

These screenshots are captured from the project applications. Public artwork and interface assets are maintained under the project’s own branding and provenance controls.

## Core workflows

### Publisher

```text
Open or create configuration
        ↓
Review SCL-derived stream and dataset settings
        ↓
Select manual, scenario, waveform, or COMTRADE source
        ↓
Run preflight and dry-run checks
        ↓
Transmit only on an authorized test link
        ↓
Export PCAP, timing health, and Markdown evidence
```

### Subscriber

```text
Choose live adapter or PCAP file
        ↓
Discover Sampled Values streams
        ↓
Optionally bind observed traffic to SCL expectations
        ↓
Inspect counters, timing, decoded values, waveform, phasor, and RMS
        ↓
Record mismatches and receiver-side evidence
```

## Current capabilities

### ARSVIN Publisher

- SCL/SCD stream setup for APPID, destination MAC, VLAN, `svID`, dataset, `confRev`, `smpRate`, `smpMod`, and `nofASDU`.
- Laboratory-oriented IEC 61850-9-2LE-style 4I+4V publishing.
- Multi-ASDU packing with `nofASDU=1/2/4/8`.
- Up to three independent publisher slots.
- Manual values, ramps, state sequences, per-phase scenarios, waveform shaping, and COMTRADE replay.
- Intentional quality-bit modes for controlled behavior checks.
- Target/actual frame rate, jitter, late-frame, missed-schedule, and send-duration evidence.
- Generated PCAP and Markdown reports.

### ArSubsv Subscriber

- Live capture through Npcap and offline classic-PCAP import.
- Stream discovery and SCL-assisted binding.
- APPID, VLAN, `svID`, `confRev`, `nofASDU`, sample-rate, and payload-layout checks.
- `smpCnt` continuity and stream-health diagnostics.
- Decoded instantaneous values, oscilloscope waveform, phasor, and RMS views.
- Receiver-side Markdown evidence reports.
- Shared-engine foundation for evidence-aware profile classification and configuration-versus-wire comparison.

## Evidence-aware profile foundation

The engine separates:

```text
Observed wire facts
Configured SCL expectations
Evidence-backed profile definitions
Explainable confidence
Strict or compatible mismatch findings
```

Sparse evidence does not produce a `Confirmed` profile result. Unknown or conflicting traffic remains visible. Named-profile support is added only when requirements and test evidence have documented provenance.

See [SV Profile Infrastructure](docs/sv-profile-infrastructure.md), [Profile Detection Output Contract](docs/profile-detection-output.md), and [SV Standards and Evidence Research Gate](docs/sv-research-gate.md).

## Supported scope and claim boundary

| Area | Current status | Boundary |
|---|---|---|
| IEC 61850 Sampled Values APDU | Publisher and Subscriber implementation for engineering and laboratory use | Not certified conformance testing. |
| IEC 61850-9-2LE-style 4I+4V | Implemented laboratory workflow | Formal profile verification and broader device evidence remain pending. |
| Generic SCL-driven Layer-2 SV | Dataset-aware engine foundation | Unknown layouts remain visible and unsupported elements are diagnosed. |
| Evidence-aware profile detection | Engine infrastructure implemented | Named-profile definitions require verified source and device evidence. |
| `nofASDU` | UI workflows emphasize `1`, `2`, `4`, and `8` | Software and PC timing remain non-deterministic. |
| COMTRADE | ASCII, BINARY, BINARY32, and FLOAT32 analog replay | Scaling and channel mapping must be reviewed before live transmission. |
| PTP / `smpSynch` | Compatibility-oriented laboratory behavior | Not an IEC 61850-9-3 certified clock. |
| Windows timing | Best-effort scheduling with visible health metrics | Not deterministic real-time execution. |
| IED subscription proof | Not provided | Sampled Values multicast has no application-layer acknowledgement. |

Evidence language in this repository distinguishes implemented behavior, deterministic tests, simulator or loopback evidence, laboratory observations, and features that remain provisional or unverified.

## Release downloads

Every tagged release builds Windows x64 artifacts with stable filenames:

| Artifact | Purpose |
|---|---|
| `ARSVIN-Publisher-win-x64.exe` | Self-contained single-file Publisher. |
| `ArSubsv-Subscriber-win-x64.exe` | Self-contained single-file Subscriber. |
| `ARSVIN-Suite-Setup-win-x64.exe` | Installer containing both applications, documentation, notices, and uninstaller. |
| `ARSVIN-win-x64-portable.zip` | Portable suite with both applications and required legal/documentation files. |
| `ARSVIN-SBOM.cdx.json` | CycloneDX 1.5 software bill of materials. |
| `SHA256SUMS.txt` | SHA-256 checksums for release verification. |

Tagged releases publish GitHub artifact attestations. Example verification:

```powershell
gh attestation verify .\ARSVIN-Suite-Setup-win-x64.exe --repo masarray/arsvin
```

The binaries are currently unsigned, so Windows SmartScreen may display an unknown-publisher warning. Npcap is not silently installed or bundled; install it separately from its official source when live capture or transmission is required.

## Quick start

### Installer

1. Download `ARSVIN-Suite-Setup-win-x64.exe` from the latest release.
2. Install the suite.
3. Install Npcap separately for live Ethernet workflows.
4. Open **ARSVIN Publisher** or **ArSubsv Subscriber**.
5. Begin with a dry run, synthetic sample, PCAP import, or authorized isolated test link.

### Portable

1. Download the portable Publisher, Subscriber, or suite ZIP.
2. Run the selected application directly.
3. Use elevated privileges only when required by the selected Npcap and network configuration.

See [Quick Start](docs/quick-start.md), [Safety Boundaries](docs/safety-boundaries.md), and [Known Limitations](docs/known-limitations.md).

## System requirements

For users:

- Windows 10 or Windows 11, x64.
- Npcap for live capture or transmission.
- Administrator permission only when required by local driver or adapter configuration.
- An independent packet dissector or process-bus analyzer is recommended for verification.

For developers:

- .NET 8 SDK, feature band 8.0.4xx.
- PowerShell 7+ recommended.
- Visual Studio 2022, JetBrains Rider, or VS Code with C# tooling.
- Inno Setup 6.7.1 when reproducing the automated installer build.

## Build from source

```powershell
git clone https://github.com/masarray/arsvin.git
cd arsvin
.\build.ps1
```

The resolved dependency graph is committed through `packages.lock.json`, and validated automation restores in locked mode.

Build release artifacts except the installer:

```powershell
.\scripts\publish-release.ps1 -Version 0.4.0
```

Run tests with both coverage gates:

```powershell
.\scripts\test-with-coverage.ps1 -MinimumWholeEngineLineCoverage 13 -MinimumLineCoverage 70
```

The current baseline contains 74 deterministic tests. CI enforces the documented whole-engine and protocol-core coverage floors; current values are engineering regression evidence, not formal protocol conformance evidence.

## Repository architecture

```text
src/ARSVIN.Engine/                     Shared production engine
src/ARSVIN.Engine/AR.Iec61850/         IEC 61850, SCL, SV, MMS, capture, and diagnostics
src/ARSVIN.Engine/AR.Iec61850/SampledValues/Profiles/
                                        Profile observation and comparison infrastructure
src/ARSVIN.Engine/AR.Iec61850.Transports.Npcap/
                                        Npcap transport
src/ARSVIN/                            Publisher application
src/ARSVIN.Subscriber/                 ArSubsv Subscriber
 tests/ARSVIN.Tests/                   Deterministic regression tests
installer/                             Windows installer definition
scripts/                               Build, validation, packaging, and release automation
docs/                                  Engineering, safety, licensing, and provenance documentation
samples/                               Synthetic and redistributable examples
site/                                  SEO-ready GitHub Pages product site
```

## Documentation

- [Documentation index](docs/index.md)
- [Quick start](docs/quick-start.md)
- [Architecture](docs/architecture.md)
- [SV standards and evidence research gate](docs/sv-research-gate.md)
- [SV conformance and interoperability matrix](docs/sv-evidence-matrix.md)
- [SV profile infrastructure](docs/sv-profile-infrastructure.md)
- [Subscriber verification app](docs/subscriber-verification-app.md)
- [COMTRADE replay](docs/comtrade-replay.md)
- [Safety boundaries](docs/safety-boundaries.md)
- [Build and release](docs/build-and-release.md)
- [Licensing](docs/LICENSING.md)
- [External IP and provenance review](docs/EXTERNAL_IP_AND_PROVENANCE_REVIEW_2026-07-14.md)
- [Public wording and claim review](docs/WORDING_AND_CLAIM_REVIEW_2026-07-14.md)

## Security and responsible use

Do not attach confidential station SCL/SCD files, credentials, production captures, customer names, restricted network plans, or proprietary support material to public issues. Follow [SECURITY.md](SECURITY.md) and [SUPPORT.md](SUPPORT.md).

## Contributing

Contributions require the project [Contributor License Agreement](CONTRIBUTOR-LICENSE-AGREEMENT.md), a DCO sign-off on every commit, documented provenance, and any required employer authorization. Read [CONTRIBUTING.md](CONTRIBUTING.md).

## Licensing

The current `main` branch and current public release packages are licensed **only** under the GNU General Public License v3.0 or later (`GPL-3.0-or-later`). See [LICENSE](LICENSE).

A separate negotiated commercial license is available for proprietary integration, OEM or white-label distribution, closed-source redistribution, private branches, support, training, and engineering services. [COMMERCIAL-LICENSE.md](COMMERCIAL-LICENSE.md) is an invitation to discuss terms, not an executed license.

Revisions up to and including `9440f08b6909ef2dc93dd483cfdcb4e1e86077d0` were released under Apache-2.0 and remain available on `archive/apache-2.0-final`. Those historical grants apply only to those earlier revisions. See [docs/LICENSING.md](docs/LICENSING.md).

Third-party components retain their applicable licenses. See [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md). Names, logos, icons, and official-release branding are governed separately by [TRADEMARK.md](TRADEMARK.md).

Copyright (C) 2026 Ari Sulistiono.