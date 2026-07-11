# Build and Release

ARSVIN uses a tag-driven GitHub Actions workflow to build two self-contained portable applications, one suite installer, one portable ZIP, and SHA-256 checksums.

## Prerequisites

For local source builds:

- Windows 10/11 x64
- .NET 8 SDK, feature band 8.0.4xx
- PowerShell 7+ recommended

For local installer builds:

- All requirements above
- Inno Setup 6

Npcap is required only for live capture/transmission testing. It is not silently bundled or installed by the ARSVIN release process.

## Build and test

```powershell
.\build.ps1
```

The script restores, builds, and tests:

- `src/ARSVIN/ARSVIN.csproj`
- `src/ARSVIN.Subscriber/ARSVIN.Subscriber.csproj`
- `tests/ARSVIN.Tests/ARSVIN.Tests.csproj`

External command exit codes are checked, so CI and local builds stop immediately when a `dotnet` command fails.

## Validate the public site

```powershell
.\scripts\validate-public-site.ps1
```

The validator checks required landing-page files, local asset references, JSON-LD blocks, web-manifest icons, sitemap canonical URL, release download filenames, and the sitemap reference in `robots.txt`.

The same validator runs directly inside the GitHub Pages deployment job. A broken landing page cannot be deployed merely because a separate CI job has not finished yet.

## Build portable release artifacts

```powershell
.\scripts\publish-release.ps1 -Version 0.3.0
```

Compatibility wrapper:

```powershell
.\publish-win-x64.ps1 -Version 0.3.0
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

## Build the installer locally

After running the publish script:

```powershell
$version = '0.3.0'
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

When release tooling, installer definitions, project files, or build configuration change, the workflow runs on the pull request and:

1. restores, builds, and tests the solution,
2. publishes both portable single-file applications,
3. creates the portable suite ZIP,
4. compiles the Inno Setup installer,
5. checks all expected artifact names and non-empty files,
6. silently installs the suite into a temporary directory,
7. verifies Publisher, Subscriber, documentation, version file, and uninstaller,
8. silently uninstalls the temporary installation,
9. generates checksums and uploads a private workflow artifact.

A pull-request run never creates a public GitHub Release.

### Stable tagged release

A public release is created only by pushing a semantic version tag. The tagged commit must already be contained in `main`; the workflow rejects a tag created from an unmerged feature or release branch.

```powershell
git switch main
git pull --ff-only origin main
git tag -a v0.4.0 -m "ARSVIN v0.4.0"
git push origin v0.4.0
```

The workflow repeats the validated packaging path, downloads the validated workflow artifact in a separate least-privilege release job, and publishes all public files to GitHub Releases. Stable tags are eligible to become the repository's latest release.

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
| `SHA256SUMS.txt` | Integrity hashes for all release assets. |

## Verify a download

```powershell
Get-FileHash .\ARSVIN-Suite-Setup-win-x64.exe -Algorithm SHA256
```

Compare the result with `SHA256SUMS.txt` from the same GitHub Release.

## Code signing status

Release binaries are currently unsigned. Windows SmartScreen may display an unknown-publisher warning. The workflow intentionally does not contain placeholder signing steps or require paid signing secrets.

When a trusted code-signing certificate becomes available, sign the portable executables before compiling the installer, then sign the completed installer before checksum generation.

## Release checklist

Before tagging a public release:

- Confirm the intended release commit is already on `main`.
- Confirm `main` CI, release validation, and CodeQL are green.
- Update `VersionPrefix` and `CHANGELOG.md`.
- Test Publisher dry run.
- Test live publishing only on an isolated lab link.
- Test Subscriber live capture and PCAP import.
- Verify generated SV traffic independently in Wireshark.
- Verify portable executables on a clean Windows 10/11 x64 machine.
- Verify install, upgrade, shortcuts, launch, and uninstall behavior.
- Review [Known Limitations](known-limitations.md).
- Review [Safety Boundaries](safety-boundaries.md).
- Review [Public Release Checklist](public-release-checklist.md).
