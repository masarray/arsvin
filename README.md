# ARSVIN - SV Injector

**ARSVIN** is a professional IEC 61850 Sampled Values injector for engineering labs, relay testing, process-bus troubleshooting, and digital substation learning.

It provides a modern test-workspace experience for:

- **Manual injection** with fast voltage/current/angle/frequency editing
- **Ramp mode** for time-based magnitude changes
- **State Sequencer** for multi-state voltage/current scenarios
- **SCL-based stream configuration**
- **SV frame publishing** through the ARIEC61850 stack
- **PTP-aware roadmap** for relay-grade process-bus compatibility

> ARSVIN is a software engineering tool for isolated lab networks. It is not a certified timing source, conformance test set, or replacement for calibrated protection test equipment.

## Repository layout

```text
src/
  ARSVIN.App/                 WPF product application

extern/
  ARIEC61850/                 External checkout, protocol stack dependency

docs/
  quick-start.md
  architecture.md
  ptp-sync-strategy.md
  process-bus-lab-setup.md
  relay-compatibility.md
  release-process.md

site/
  index.html                  GitHub Pages landing page
```

## Clone

```powershell
git clone https://github.com/masarray/arsvin.git
cd arsvin
.\build.ps1
```

## Build

Requirements:

- Windows 10/11
- .NET 8 SDK
- Visual Studio 2022 or VS Code with C# Dev Kit
- Npcap for live Ethernet publishing

```powershell
dotnet restore .\src\ARSVIN.App\ARSVIN.App.csproj
dotnet build .\src\ARSVIN.App\ARSVIN.App.csproj -c Release
```

## Run

```powershell
dotnet run --project .\src\ARSVIN.App\ARSVIN.App.csproj -c Debug
```

## PTP strategy

PTP work is split intentionally:

- reusable PTP packet, parser, monitor, and sync-state logic belongs in `ARIEC61850`;
- ARSVIN consumes that engine for product UI, status, warnings, and SV `smpSynch` policy;
- soft PTP publishing stays lab-only unless backed by proper hardware timestamping and validation.

See [`docs/ptp-sync-strategy.md`](docs/ptp-sync-strategy.md).

## Safety

Live SV injection can affect protection devices when connected to real process-bus networks. Use ARSVIN only on isolated lab networks unless a formal test plan has approved the topology, VLANs, adapter, relay subscription, and PTP/synchronization conditions.

## License

Apache-2.0.
