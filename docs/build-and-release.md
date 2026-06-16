# Build and Release

## Build

```powershell
.\build.ps1
```

## Publish portable win-x64 package

```powershell
.\publish-win-x64.ps1
```

## GitHub Release

Push a semantic version tag:

```powershell
git tag v0.1.0
git push origin v0.1.0
```

The release workflow publishes a self-contained Windows package.
