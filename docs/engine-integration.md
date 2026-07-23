# ARIEC61850 sibling-engine integration

## Ownership

ARSVIN Publisher and ArSubsv Subscriber are derived applications. Reusable IEC 61850 protocol, Sampled Values, SCL, PCAP, transport, measurement, diagnostics, and evidence behavior is owned by the sibling [`masarray/ARIEC61850`](https://github.com/masarray/ARIEC61850) repository.

The application repository owns WPF presentation, commands, workflow orchestration, application settings, branding, and packaging. The inactive `src/ARSVIN.Engine` directory is temporary migration material and is not an active project reference.

## Paired local layout

```text
C:\Git\
├── ARIEC61850\
└── arsvin\
```

For the unmerged integration test:

```powershell
git clone https://github.com/masarray/ARIEC61850.git C:\Git\ARIEC61850
git -C C:\Git\ARIEC61850 switch agent/sv-core-unification

git clone https://github.com/masarray/arsvin.git C:\Git\arsvin
git -C C:\Git\arsvin switch agent/ariec-sibling-integration
```

Then build the paired graph:

```powershell
cd C:\Git\arsvin
.\build.ps1
```

The default sibling path can be overridden:

```powershell
$env:ARIEC61850_ROOT = 'D:\Engineering\ARIEC61850'
.\build.ps1
```

or for a direct MSBuild invocation:

```powershell
dotnet build .\src\ARSVIN.Subscriber\ARSVIN.Subscriber.csproj `
  -c Release `
  -p:ARIEC61850_ROOT='D:\Engineering\ARIEC61850'
```

## Paired test sequence

1. Build and test `ARIEC61850` first.
2. Build ARSVIN Publisher and ArSubsv against that exact checkout.
3. Run Publisher dry-run and PCAP generation.
4. Open the generated PCAP in ArSubsv without SCL; verify generic raw words and unresolved semantics.
5. Import the matching SCL; verify ordered dataset mapping, evidence-backed scaling, waveform, RMS, and phasor behavior.
6. Run authorized isolated live capture/transmit tests only after offline tests pass.
7. Record both Git commit SHAs in every test note.

Commands:

```powershell
cd C:\Git\ARIEC61850
dotnet restore .\ARIEC61850.sln
dotnet build .\ARIEC61850.sln -c Release
dotnet test .\ARIEC61850.sln -c Release --no-build

cd C:\Git\arsvin
.\build.ps1
```

## Migration rules

- New reusable `AR.Iec61850.*` protocol or analysis code goes to ARIEC61850 first.
- Application projects must not reference `ARSVIN.Engine`.
- Manufacturer identity must not select an SV parser, dataset order, scaling, quality interpretation, timebase, or health result.
- Engine changes require deterministic engine tests in ARIEC61850 and paired application regression tests in ARSVIN.
- CI records the exact paired engine commit; a moving unrecorded engine revision is not a reproducible release input.
- Lock files remain temporarily unlocked in this draft migration. They must be regenerated after the reviewed ARIEC61850 commit is pinned.

## Current draft pairing

| Repository | Branch | Pull request |
|---|---|---|
| ARIEC61850 | `agent/sv-core-unification` | `masarray/ARIEC61850#45` |
| ARSVIN | `agent/ariec-sibling-integration` | `masarray/arsvin#51` |

Neither pull request is intended for merge until paired local testing, CI, CodeQL, packaging, and offline PCAP/SCL regression tests are complete.
