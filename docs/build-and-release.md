# Build and Release

ARSVIN uses a tag-driven GitHub Actions workflow to build two self-contained portable applications, one suite installer, one portable ZIP, and SHA-256 checksums.

## Prerequisites

For local source builds:

- Windows 10/11 x64
- .NET 8 SDK
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
- warns when Npcap is not detected.

## GitHub Actions release flow

Workflow: `.github/workflows/release.yml`

### Tagged release

Create and push a semantic version tag:

```powershell
git tag v0.3.0
git push origin v0.3.0
```

The workflow then:

1. restores, builds, and tests the solution,
2. publishes both self-contained single-file applications,
3. creates the portable suite ZIP,
4. installs Inno Setup on the Windows runner,
5. compiles the suite installer,
6. generates `SHA256SUMS.txt`,
7. uploads the files as a workflow artifact,
8. creates a GitHub Release and attaches all public artifacts.

### Manual artifact build

Run **Build Windows Release** from the Actions tab and provide a version such as `0.3.0-dev.1`.

Manual runs upload workflow artifacts but do not create a GitHub Release because no tag is present.

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

- Confirm `main` CI and CodeQL are green.
- Test Publisher dry run.
- Test live publishing only on an isolated lab link.
- Test Subscriber live capture and PCAP import.
- Verify generated SV traffic independently in Wireshark.
- Verify portable executables on a clean Windows 10/11 x64 machine.
- Verify install, upgrade, shortcuts, launch, and uninstall behavior.
- Review [Known Limitations](known-limitations.md).
- Review [Safety Boundaries](safety-boundaries.md).
- Review [Public Release Checklist](public-release-checklist.md).
