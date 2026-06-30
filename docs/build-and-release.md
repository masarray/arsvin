# Build and Release

## Build

Requirements:

- Windows 10/11 x64
- .NET 8 SDK
- PowerShell 7+ recommended

```powershell
.\build.ps1
```

The build script restores the WPF application and test project, builds Release configuration, and runs the unit tests.

## Test

```powershell
dotnet test tests/ARSVIN.Tests/ARSVIN.Tests.csproj -c Release
```

The current test project focuses on stable protocol primitives. More SCL, COMTRADE, SV, and PTP tests should be added as public APIs stabilize.

## Publish portable Windows package

```powershell
.\publish-win-x64.ps1
```

Output:

```text
artifacts/ARSVIN-win-x64-portable.zip
```

The ZIP should include:

- `ARSVIN.exe`
- README
- LICENSE
- NOTICE
- THIRD_PARTY_NOTICES
- documentation files needed for safe use

## GitHub release

Create and push a semantic version tag:

```powershell
git tag v0.1.0
git push origin v0.1.0
```

The release workflow builds a self-contained win-x64 package and attaches it to the GitHub Release.

## Manual release checklist

Before publishing:

- Run the build script on Windows.
- Test dry run mode.
- Test live publishing only on an isolated lab link.
- Verify packet behavior in Wireshark.
- Review [Known Limitations](known-limitations.md).
- Review [Public Release Checklist](public-release-checklist.md).
