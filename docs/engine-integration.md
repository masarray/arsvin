# ARIEC61850 sibling-engine integration

## Ownership

ARSVIN Publisher and ArSubsv Subscriber are derived applications. Reusable IEC 61850 protocol, Sampled Values, SCL, PCAP, transport, measurement, diagnostics, and evidence behavior is owned by the sibling [`masarray/ARIEC61850`](https://github.com/masarray/ARIEC61850) repository.

The application repository owns WPF presentation, commands, workflow orchestration, application settings, branding, and packaging. The inactive `src/ARSVIN.Engine` directory is temporary migration material and is not an active project reference.

## Pinned engine contract

The paired revision is recorded in `engines/ARIEC61850.lock.json`. CI, CodeQL, local paired validation, packaging, and release automation must resolve the engine from that file and verify the exact 40-character commit SHA. A moving branch name is retained only as human context; it is not the reproducible build identity.

Current paired revision for this draft:

```text
ARIEC61850 ref: agent/sv-core-unification
ARIEC61850 commit: 143e6ca69986cd553405eec883a9928cdfda9367
ARIEC61850 PR: #45
```

## Migrated reusable contracts

The paired engine branch now owns the reusable contracts required by both applications:

- generic ASDU and raw `seqOfData` inspection;
- evidence-gated engineering scaling;
- timebase and `smpCnt` continuity analysis;
- semantic quality decoding and publisher quality encoding;
- explicit CT/VT and measurement-domain context;
- Sampled Values publisher evidence report contracts;
- local transmitter timing-health evidence;
- multi-ASDU Publisher profile/session behavior with bounded counter wrap;
- unified live/PCAP observation windows and stable logical stream keys;
- vendor-neutral profile evidence, confidence, and SCL-versus-wire comparison;
- Subscriber JSON/Markdown evidence and regression-comparison contracts.

These contracts are implemented and tested in ARIEC61850. Application code consumes them; it must not maintain a second behavioral copy.

## Paired local layout

```text
C:\Git\
├── ARIEC61850\
└── arsvin\
```

For the unmerged integration test:

```powershell
git clone https://github.com/masarray/ARIEC61850.git C:\Git\ARIEC61850
git -C C:\Git\ARIEC61850 fetch origin 143e6ca69986cd553405eec883a9928cdfda9367
git -C C:\Git\ARIEC61850 checkout --detach 143e6ca69986cd553405eec883a9928cdfda9367

git clone https://github.com/masarray/arsvin.git C:\Git\arsvin
git -C C:\Git\arsvin switch agent/ariec-sibling-integration
```

Run the complete paired gate:

```powershell
cd C:\Git\arsvin
.\scripts\test-paired-engine.ps1
```

For a faster application-only iteration after the engine suite has already passed:

```powershell
.\build.ps1
```

The default sibling path can be overridden:

```powershell
$env:ARIEC61850_ROOT = 'D:\Engineering\ARIEC61850'
.\scripts\test-paired-engine.ps1
```

## Paired test sequence

1. Verify the local engine checkout exactly matches the lock file.
2. Restore, build, and test `ARIEC61850`.
3. Build ARSVIN Publisher and ArSubsv against that exact checkout.
4. Run ARSVIN regression and coverage gates.
5. Run Publisher dry-run and PCAP generation, including `nofASDU=1` and one multi-ASDU case.
6. Open the generated PCAP in ArSubsv without SCL; verify generic raw words and unresolved semantics.
7. Import the matching SCL; verify ordered dataset mapping, evidence-backed scaling, waveform, RMS, and phasor behavior.
8. Export two Subscriber evidence reports and verify the shared comparator identifies source failover, health regression, and continuity changes.
9. Run authorized isolated live capture/transmit tests only after offline tests pass.
10. Preserve `artifacts/paired-validation/paired-validation.json` with the test notes.

## Migration rules

- New reusable `AR.Iec61850.*` protocol or analysis code goes to ARIEC61850 first.
- Application projects must not reference `ARSVIN.Engine`.
- Manufacturer identity must not select an SV parser, dataset order, scaling, quality interpretation, timebase, or health result.
- Engine changes require deterministic engine tests in ARIEC61850 and paired application regression tests in ARSVIN.
- CI records and verifies the exact paired engine commit.
- Changing the engine requires a reviewed lock-file update in the application PR.
- Lock files remain temporarily unlocked in this draft migration. NuGet lock files must be regenerated after both paired revisions are accepted.

## Current draft pairing

| Repository | Branch | Pull request |
|---|---|---|
| ARIEC61850 | `agent/sv-core-unification` | `masarray/ARIEC61850#45` |
| ARSVIN | `agent/ariec-sibling-integration` | `masarray/arsvin#51` |

Neither pull request is intended for merge until paired local testing, pinned CI, CodeQL, packaging, and offline PCAP/SCL regression tests are complete.
