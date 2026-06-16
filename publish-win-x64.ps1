$ErrorActionPreference = 'Stop'

$out = Join-Path $PSScriptRoot 'artifacts\ARSVIN-win-x64'
if (Test-Path $out) { Remove-Item $out -Recurse -Force }
New-Item -ItemType Directory -Path $out | Out-Null

dotnet publish .\src\ARSVIN\ARSVIN.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:EnableCompressionInSingleFile=true `
  -p:DebugType=None `
  -p:DebugSymbols=false `
  -o $out

Copy-Item .\README.md (Join-Path $out 'README.txt') -Force
Copy-Item .\LICENSE (Join-Path $out 'LICENSE.txt') -Force
Compress-Archive -Path (Join-Path $out '*') -DestinationPath .rtifacts\ARSVIN-win-x64-portable.zip -Force
Write-Host "Created artifacts\ARSVIN-win-x64-portable.zip"
