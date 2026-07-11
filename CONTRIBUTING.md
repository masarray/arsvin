# Contributing

Thank you for considering a contribution to ARSVIN.

ARSVIN is an engineering suite for IEC 61850 Sampled Values laboratory workflows. The project values clear code, cautious live-network behavior, reproducible test notes, and documentation that an engineer can use in the field.

## Project principles

- Keep Publisher and Subscriber responsibilities explicit and independently testable.
- Add analysis features only when they improve Sampled Values visibility, interoperability, troubleshooting, or evidence quality.
- Keep live packet capture and injection guarded, visible, and clearly labelled.
- Preserve Apache-2.0 compatibility.
- Prefer explicit engineering wording over marketing claims.
- Update documentation when behavior, UI, safety assumptions, or release packaging changes.

## Development setup

Recommended environment:

- Windows 10/11 x64
- .NET 8 SDK
- PowerShell 7+
- Npcap for live packet capture/publishing tests
- Wireshark for packet inspection

Build:

```powershell
.\build.ps1
```

Run tests:

```powershell
dotnet test tests/ARSVIN.Tests/ARSVIN.Tests.csproj -c Release
```

Create portable release artifacts:

```powershell
.\scripts\publish-release.ps1 -Version 0.1.0
```

## Pull request checklist

Before opening a PR, please check:

- The change has a narrow engineering purpose.
- Both affected applications build in Release mode.
- Relevant unit tests were added or updated where practical.
- Safety behavior is not weakened.
- Docs are updated for user-visible changes.
- Screenshots or Wireshark notes are included for UI / packet behavior changes.
- New dependencies are necessary, maintained, and license-compatible.

## Commit and branch style

Short, clear commit messages are preferred:

```text
Fix VLAN TCI validation
Add COMTRADE replay guardrail
Document smpSynch compatibility mode
```

## Reporting issues

Use GitHub Issues and include:

- ARSVIN version or commit
- Application: Publisher or ArSubsv Subscriber
- Windows version
- Npcap version
- SCL/COMTRADE/PCAP sample if shareable
- Steps to reproduce
- Expected behavior
- Actual behavior
- Screenshots or Wireshark capture notes when relevant

Do not upload confidential station SCL files, relay IP plans, credentials, or production network captures.
