# Build and Release

ARSVIN uses a tag-driven GitHub Actions workflow to build two self-contained portable applications, one suite installer, one portable ZIP, a CycloneDX software bill of materials, SHA-256 checksums, and signed GitHub artifact attestations.

## Prerequisites

For local source builds:

- Windows 10/11 x64
- .NET 8 SDK, feature band 8.0.4xx
- PowerShell 7+ recommended

For local installer builds:

- All requirements above
- Inno Setup 6.7.1

Npcap is required only for live capture/transmission testing. It is not silently bundled or installed by the ARSVIN release process.

## Dependency locking

Package versions are centrally managed in `Directory.Packages.props`. Each project also commits a `packages.lock.json` file so the resolved direct and transitive NuGet graph is reviewable and repeatable.

Validated automation restores with locked mode:

```powershell
dotnet restore ARSVIN.sln --locked-mode
```

A dependency update must intentionally regenerate the affected lock files and include them in the same pull request:

```powershell
dotnet restore ARSVIN.sln --force-evaluate
```

Do not hand-edit lock files. Review dependency and integrity changes in the generated diff.

## Build and test

```powershell
.\build.ps1
```

The script restores the solution in locked mode, then builds and tests:

- `src/ARSVIN.Engine/ARSVIN.Engine.csproj`
- `src/ARSVIN/ARSVIN.csproj`
- `src/ARSVIN.Subscriber/ARSVIN.Subscriber.csproj`
- `tests/ARSVIN.Tests/ARSVIN.Tests.csproj`

External command exit codes are checked, and compiler warnings are treated as errors for the validated build path.

### Coverage evidence

```powershell
.\scripts\test-with-coverage.ps1 -MinimumLineCoverage 50
```

The script:

1. runs the xUnit suite using pinned Coverlet MSBuild instrumentation,
2. instruments the complete shared production `ARSVIN.Engine` assembly,
3. writes TRX, the complete `dotnet test` log, and Cobertura evidence under `artifacts/test-results`,
4. reports whole-engine coverage transparently,
5. calculates the regression gate over the established protocol-core surface,
6. fails when no production lines are instrumented or protocol-core coverage falls below the configured threshold.

Current verified baselines:

| Metric | Result |
|---|---:|
| Whole `ARSVIN.Engine` instrumented lines | 15,726 |
| Whole-engine line coverage | 5.64% |
| Protocol-core instrumented lines | 1,534 |
| Protocol-core covered lines | 888 |
| Protocol-core line coverage | 57.89% |
| Enforced protocol-core floor | 50% |

This is not a claim that the complete WPF UI or every live-network path is covered. Whole-engine coverage is intentionally shown as a transparent baseline and must rise as SCL, COMTRADE, capture, diagnostics, MMS, scheduling, and transport tests are expanded.

## Validate the public site

Build the staged landing page and HTML documentation:

```powershell
python scripts/build-public-site.py --output artifacts/public-site
```

Validate the staged output:

```powershell
.\scripts\validate-public-site.ps1 -SiteRoot artifacts/public-site
```

The validator recursively checks:

- one `<h1>` per public page,
- descriptions and canonical URLs,
- canonical uniqueness,
- valid JSON-LD,
- local links and assets,
- documentation search-index targets,
- sitemap coverage,
- web-manifest icons,
- release filenames,
- sitemap metadata in `robots.txt`.

The same builder and validator run directly inside GitHub Pages deployment. A broken page cannot deploy merely because a separate CI job has not finished yet.

## Build portable release artifacts

```powershell
.\scripts\publish-release.ps1 -Version 0.3.1
```

Compatibility wrapper:

```powershell
.\publish-win-x64.ps1 -Version 0.3.1
```

Generated files:

```text
artifacts/release/
├── ARSVIN-Publisher-win-x64.exe
├── ArSubsv-Subscriber-win-x64.exe
└── ARSVIN-win-x64-portable.zip
```

Staging files for the installer are generated under:

```text
artifacts/installer-input/
```

The two direct `.exe` files are self-contained .NET 8 single-file applications. The ZIP includes both applications, essential documentation, license notices, and sample files.

## Generate the CycloneDX SBOM

After restoring the solution, run:

```powershell
.\scripts\generate-sbom.ps1 -Version 0.3.1
```

Output:

```text
artifacts/release/ARSVIN-SBOM.cdx.json
```

The generator reads the locked, resolved NuGet graphs for Publisher and Subscriber, deduplicates direct and transitive application packages, records which application uses each package, and writes CycloneDX 1.5 JSON. Test-only packages such as xUnit and Coverlet are intentionally excluded.

The SBOM includes a deterministic UUID URN `serialNumber`, source commit metadata, component version, and stable component ordering. Repeated generation from the same commit and version is reviewable. The SBOM covers managed application dependencies; it does not claim to inventory Windows, Npcap, GitHub-hosted runner contents, or every build-service tool.

## Build the installer locally

After running the publish script:

```powershell
$version = '0.3.1'
$sourceDir = (Resolve-Path '.\artifacts\installer-input').Path
$outputDir = (Resolve-Path '.\artifacts\release').Path
$iscc = "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe"

& $iscc `
  "/DMyAppVersion=$version" `
  "/DSourceDir=$sourceDir" `
  "/DOutputDir=$outputDir" `
  '.\installer\ARSVIN.iss'
```

Output:

```text
artifacts/release/ARSVIN-Suite-Setup-win-x64.exe
```

The automated workflow installs and verifies the exact Chocolatey package version declared in `INNO_SETUP_VERSION`. It records the resolved `ISCC.exe` path and file metadata for evidence, while the exact package version remains the authoritative toolchain pin.

The installer:

- installs per-user under `%LOCALAPPDATA%\Programs\ARSVIN`,
- includes Publisher and Subscriber,
- creates Start Menu shortcuts,
- offers an optional Publisher desktop shortcut,
- includes an uninstaller,
- preserves Apache-2.0 and third-party notices,
- warns interactively when Npcap is not detected,
- supports unattended installation and removal without an Npcap message box.

## GitHub Actions release flow

Workflow: `.github/workflows/release.yml`

### Pull-request validation

When release tooling, installer definitions, project files, dependency locks, or build configuration change, the workflow runs on the pull request and:

1. restores the locked dependency graph,
2. builds and tests the solution with warnings treated as errors,
3. publishes both portable single-file applications,
4. creates the portable suite ZIP,
5. installs and verifies pinned Inno Setup,
6. compiles the installer,
7. generates and structurally validates the application-only CycloneDX SBOM,
8. checks all expected artifact names and non-empty files,
9. silently installs the suite into a temporary directory,
10. verifies Publisher, Subscriber, documentation, version file, and uninstaller,
11. silently uninstalls the temporary installation,
12. generates checksums and uploads a private workflow artifact.

A pull-request run never creates a public GitHub Release or public attestation.

### Stable tagged release

A public release is created only by pushing a semantic-version tag. The tagged commit must already be contained in `main`; the workflow rejects a tag created from an unmerged feature or release branch.

```powershell
git switch main
git pull --ff-only origin main
git tag -a v0.4.0 -m "ARSVIN v0.4.0"
git push origin v0.4.0
```

The workflow repeats the validated packaging path, downloads the validated artifact in a separate least-privilege release job, verifies that no GitHub Release already exists for the tag, creates signed provenance and SBOM attestations, and publishes the public files.

Published GitHub Releases are immutable in automation. A rerun cannot replace existing assets. Any artifact correction requires a new patch version, such as `v0.4.1`.

Stable tags are eligible to become the repository's latest release.

### Prerelease tag

Semantic versions containing a suffix are published as GitHub prereleases and do not replace the latest stable release:

```powershell
git switch main
git pull --ff-only origin main
git tag -a v0.4.0-rc.1 -m "ARSVIN v0.4.0-rc.1"
git push origin v0.4.0-rc.1
```

Examples recognized as prereleases include `-alpha.1`, `-beta.1`, and `-rc.1`.

### Manual artifact build

Run **Build Windows Release** from the Actions tab and provide a version such as `0.4.0-dev.1`.

Manual runs upload private workflow artifacts only. They never create or replace a public GitHub Release, even when started while viewing another branch.

## Release assets

| File | Description |
|---|---|
| `ARSVIN-Publisher-win-x64.exe` | Portable Publisher. |
| `ArSubsv-Subscriber-win-x64.exe` | Portable Subscriber/analysis companion. |
| `ARSVIN-Suite-Setup-win-x64.exe` | Installer for both applications. |
| `ARSVIN-win-x64-portable.zip` | Portable suite package. |
| `ARSVIN-SBOM.cdx.json` | CycloneDX 1.5 managed application-dependency SBOM. |
| `SHA256SUMS.txt` | Integrity hashes for all release assets. |

## Verify a download

Verify the local hash:

```powershell
Get-FileHash .\ARSVIN-Suite-Setup-win-x64.exe -Algorithm SHA256
```

Compare the result with `SHA256SUMS.txt` from the same GitHub Release.

Verify signed GitHub build provenance:

```powershell
gh attestation verify .\ARSVIN-Suite-Setup-win-x64.exe --repo masarray/arsvin
```

GitHub artifact attestations use a short-lived signing identity issued during the tagged workflow. This validates repository/workflow provenance; it is separate from Windows Authenticode signing.

## Code signing status

Release binaries are currently unsigned. Windows SmartScreen may display an unknown-publisher warning. The workflow intentionally does not contain placeholder signing steps or require paid signing secrets.

When a trusted code-signing certificate becomes available, sign the portable executables before compiling the installer, then sign the completed installer before checksum generation.

## Release checklist

Before tagging a public release:

- Confirm the intended release commit is already on `main`.
- Confirm `main` CI, release validation, and CodeQL are green.
- Update `VersionPrefix` and `CHANGELOG.md`.
- Confirm committed NuGet lock files match the reviewed dependency update.
- Review the generated application SBOM and checksums.
- Confirm no GitHub Release already exists for the intended tag.
- Test Publisher dry run and Subscriber PCAP import.
- Use a new patch version for any correction to an already published release.
