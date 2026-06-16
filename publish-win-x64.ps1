param([string]$Configuration = "Release")
$ErrorActionPreference = "Stop"

if (-not (Test-Path ".\extern\ARIEC61850\.git")) {
  if (Test-Path ".\extern\ARIEC61850") {
    Remove-Item ".\extern\ARIEC61850" -Recurse -Force
  }
  git clone https://github.com/masarray/ARIEC61850.git .\extern\ARIEC61850
} else {
  git -C .\extern\ARIEC61850 pull --ff-only
}

dotnet publish .\src\ARSVIN.App\ARSVIN.App.csproj `
  -c $Configuration `
  -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:EnableCompressionInSingleFile=true `
  -o .\artifacts\ARSVIN-win-x64
