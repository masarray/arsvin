# Build and Release

ARSVIN uses tag-driven GitHub Actions to build two self-contained applications, a suite installer, a portable ZIP, a CycloneDX SBOM, SHA-256 checksums, and GitHub artifact attestations.

## Prerequisites

Local source builds require Windows 10 or Windows 11 x64, .NET 8 SDK feature band 8.0.4xx, and PowerShell 7+ recommended. Reproducing the installer also requires Inno Setup 6.7.1.

Npcap is required only for authorized live capture or transmission tests. It is not bundled or silently installed.

## Licensing and public-content gate

Before restore, build, site publication, or release packaging, run:

```powershell
python scripts/verify-current-license.py
python scripts/validate-public-neutrality.py
```

The first command verifies:

- GNU GPL version 3 as the current root license;
- `GPL-3.0-or-later` project metadata;
- historical Apache boundary documentation without an active dual-license presentation;
- commercial-notice wording that grants no rights by itself;
- required copyright, trademark, CLA, DCO, provenance, and third-party files; and
- current README, site, installer, and packaging markers.

The second command rejects prohibited external-product comparison terms from public content.

## Dependency locking

Package versions are centrally managed in `Directory.Packages.props`. Every project commits `packages.lock.json` so the resolved graph is reviewable and reproducible.

```powershell
dotnet restore ARSVIN.sln --locked-mode
```

A deliberate dependency update uses:

```powershell
dotnet restore ARSVIN.sln --force-evaluate
```

Review direct and transitive changes, licenses, vulnerability output, and lock-file integrity. Do not hand-edit lock files.

## Build and test

```powershell
.\build.ps1
```

The build script:

1. verifies current licensing, provenance, and public wording;
2. validates public terminology neutrality;
3. restores the locked dependency graph;
4. builds Publisher and Subscriber with warnings as errors; and
5. runs deterministic tests.

Publisher, Subscriber, and Tests reference the same `ARSVIN.Engine` assembly. Protocol parsing, Sampled Values behavior, SCL handling, profile observations, comparison logic, capture, and transport code remain owned by the shared engine project.

## Coverage evidence

```powershell
.\scripts\test-with-coverage.ps1 -MinimumWholeEngineLineCoverage 13 -MinimumLineCoverage 70
```

The coverage workflow retains TRX, full test logs, and Cobertura output under `artifacts/test-results`, then enforces whole-engine and protocol-core regression floors.

The documented baseline contains 74 deterministic tests, 13.35% whole-engine line coverage, and 70.47% protocol-core line coverage. These values are regression evidence, not formal conformance, complete live-network validation, deterministic timing evidence, or universal device-interoperability evidence.

## Build and validate the public site

```powershell
python scripts/build-public-site.py --output artifacts/public-site
.\scripts\validate-public-site.ps1 -SiteRoot artifacts/public-site
```

The site validator checks:

- one H1 per page;
- meta descriptions and canonical URLs;
- canonical uniqueness;
- valid JSON-LD;
- local links and assets;
- documentation search-index targets;
- sitemap coverage;
- web-manifest icons;
- required release filenames; and
- sitemap metadata in `robots.txt`.

Generated documentation pages identify GPL-3.0-or-later as the current community license and link to the separate commercial path without presenting it as an automatically granted alternative.

## Build release artifacts

```powershell
.\scripts\publish-release.ps1 -Version 0.4.0
```

Compatibility wrapper:

```powershell
.\publish-win-x64.ps1 -Version 0.4.0
```

Generated portable files:

```text
artifacts/release/
├── ARSVIN-Publisher-win-x64.exe
├── ArSubsv-Subscriber-win-x64.exe
└── ARSVIN-win-x64-portable.zip
```

Installer staging is created under `artifacts/installer-input/`.

The portable ZIP and installer staging include:

```text
README.md
LICENSE.txt
NOTICE.txt
COMMERCIAL-LICENSE.md
COPYRIGHT.md
TRADEMARK.md
THIRD_PARTY_NOTICES.md
docs/LICENSING.md
```

`LICENSE.txt` must contain GNU GPL version 3. A historical Apache license file must not be included in a current GPL package.

## SBOM

After restoring the solution:

```powershell
.\scripts\generate-sbom.ps1 -Version 0.4.0
```

Output:

```text
artifacts/release/ARSVIN-SBOM.cdx.json
```

The CycloneDX 1.5 SBOM records locked managed runtime dependencies used by Publisher and Subscriber. It does not claim to inventory Windows, separately installed Npcap, hosted-runner contents, or every build-service component.

## Installer

After publishing the applications:

```powershell
$version = '0.4.0'
$sourceDir = (Resolve-Path '.\artifacts\installer-input').Path
$outputDir = (Resolve-Path '.\artifacts\release').Path
$iscc = "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe"

& $iscc `
  "/DMyAppVersion=$version" `
  "/DSourceDir=$sourceDir" `
  "/DOutputDir=$outputDir" `
  '.\installer\ARSVIN.iss'
```

The installer is per-user, contains Publisher and Subscriber, includes current legal and documentation files, creates Start Menu shortcuts, supports silent install and uninstall validation, and informs the user when Npcap is not detected.

## GitHub Actions release flow

Workflow: `.github/workflows/release.yml`

### Pull-request validation

When release tooling or application inputs change, the workflow:

1. runs the licensing and wording gate through `build.ps1`;
2. restores, builds, and tests;
3. publishes both portable applications;
4. creates the portable suite ZIP;
5. installs pinned installer tooling;
6. compiles and smoke-tests the installer;
7. generates and validates the SBOM;
8. validates artifact names and non-empty files;
9. generates checksums; and
10. uploads a private workflow artifact.

A pull-request run does not create a public GitHub Release or public attestation.

### Stable release

A release is created only by pushing a semantic-version tag whose commit is already contained in `main`:

```powershell
git switch main
git pull --ff-only origin main
git tag -a v0.4.0 -m "ARSVIN v0.4.0"
git push origin v0.4.0
```

Published releases are immutable in automation. Corrections require a new semantic-version patch.

Prerelease suffixes such as `v0.5.0-rc.1` produce prereleases without replacing the latest stable release. Manual workflow dispatch produces private workflow artifacts only.

## Release assets

| File | Description |
|---|---|
| `ARSVIN-Publisher-win-x64.exe` | Portable Publisher. |
| `ArSubsv-Subscriber-win-x64.exe` | Portable Subscriber. |
| `ARSVIN-Suite-Setup-win-x64.exe` | Installer for both applications. |
| `ARSVIN-win-x64-portable.zip` | Portable suite package. |
| `ARSVIN-SBOM.cdx.json` | CycloneDX managed runtime-dependency SBOM. |
| `SHA256SUMS.txt` | Integrity hashes for release assets. |

## Verify a download

```powershell
Get-FileHash .\ARSVIN-Suite-Setup-win-x64.exe -Algorithm SHA256
gh attestation verify .\ARSVIN-Suite-Setup-win-x64.exe --repo masarray/arsvin
```

Checksums verify file integrity against the same release. Artifact attestations verify repository and workflow provenance. Neither is Windows Authenticode signing.

## Code-signing status

Release binaries are currently unsigned. Windows SmartScreen may show an unknown-publisher warning. When a trusted certificate and operating process are available, sign the portable executables before installer compilation and sign the completed installer before checksum generation.

## Before tagging

- Confirm the intended commit is on `main`.
- Confirm CI, CodeQL, Pages validation, and release validation are green.
- Update `VersionPrefix`, `CHANGELOG.md`, and release notes.
- Review dependency locks, SBOM, checksums, legal notices, and package contents.
- Confirm no release already exists for the tag.
- Test Publisher dry run and Subscriber PCAP import.
- Use authorized isolated links for any live test.
- Use a new patch version for any correction to an existing release.