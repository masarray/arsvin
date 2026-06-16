# ARSVIN — IEC 61850 Sampled Values Injector

[![CI](https://github.com/masarray/arsvin/actions/workflows/ci.yml/badge.svg)](https://github.com/masarray/arsvin/actions/workflows/ci.yml)
[![Release](https://github.com/masarray/arsvin/actions/workflows/release.yml/badge.svg)](https://github.com/masarray/arsvin/actions/workflows/release.yml)
[![CodeQL](https://github.com/masarray/arsvin/actions/workflows/codeql.yml/badge.svg)](https://github.com/masarray/arsvin/actions/workflows/codeql.yml)
[![License](https://img.shields.io/badge/license-Apache--2.0-blue.svg)](LICENSE)

**ARSVIN** is an open IEC 61850 Sampled Values injector for digital substation R&D, relay laboratories, process-bus experiments, and engineering education.

It focuses on one job: **generate realistic IEC 61850 Sampled Values traffic from SCL configuration, multiple simulated publishers, lab PTP, and COMTRADE transient records.**

> ARSVIN is not a certified protection test set and not a calibrated timing source. Use an external PTP grandmaster and certified equipment for relay acceptance, FAT, SAT, and calibrated protection testing.

## Highlights

- **Multi-stream SV injection** — simulate up to three SV publishers / merging units from one Windows workstation.
- **Independent values per publisher** — different magnitude, phase, frequency, APPID, VLAN, MAC, svID, and dataset identity per publisher.
- **SCL-based stream setup** — import SCL and select SV streams without manually typing every network field.
- **COMTRADE replay** — replay ASCII, BINARY, BINARY32, and FLOAT32 analog COMTRADE records as Sampled Values.
- **Lab PTP publisher** — optional synthetic PTP traffic for lab-only experiments.
- **PTP monitor and smpSynch policy** — avoid silently claiming global sync when timing is not visible.
- **Portable Windows release** — GitHub Actions builds a self-contained win-x64 package.
- **Apache-2.0** — open-source, permissive license.

## Who it is for

- IEC 61850 relay and process-bus R&D teams
- Protection engineers learning Sampled Values behavior
- University labs and digital substation courses
- Integrators validating SCL-based SV wiring
- Developers building IEC 61850 tools

## Quick start

1. Install **Npcap** on Windows.
2. Download the latest `ARSVIN-win-x64-portable.zip` from Releases.
3. Run `ARSVIN.exe` as Administrator when using live packet injection.
4. Open an SCL file.
5. Select Publisher 1, 2, or 3.
6. Select an SV stream, set values, or import COMTRADE.
7. Start dry-run or live injection.

See [Quick Start](docs/quick-start.md).

## Build from source

Requirements:

- Windows 10/11
- .NET 8 SDK
- Npcap for live Ethernet injection

```powershell
cd arsvin
.uild.ps1
```

Create a portable package:

```powershell
.\publish-win-x64.ps1
```

## Repository topics

Recommended GitHub topics:

`iec61850`, `sampled-values`, `sv-injector`, `comtrade`, `digital-substation`, `process-bus`, `relay-testing`, `ptp`, `wpf`, `dotnet`

## Documentation

- [Quick Start](docs/quick-start.md)
- [Multi-Stream SV Injection](docs/multi-stream.md)
- [COMTRADE Replay](docs/comtrade-replay.md)
- [PTP and smpSynch Strategy](docs/ptp-and-smpsynch.md)
- [Build and Release](docs/build-and-release.md)
- [Safety Boundaries](docs/safety-boundaries.md)

## Author

Created and maintained by **Ari Sulistiono**.

GitHub: [github.com/masarray](https://github.com/masarray)

## License

Apache License 2.0. See [LICENSE](LICENSE).
