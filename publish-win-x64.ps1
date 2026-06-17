$ErrorActionPreference = 'Stop'

$out = Join-Path $PSScriptRoot 'artifacts\ARSVIN-win-x64'
if (Test-Path $out) { Remove-Item $out -Recurse -Force }
New-Item -ItemType Directory -Path $out | Out-Null

$project = Join-Path $PSScriptRoot 'src\ARSVIN\ARSVIN.csproj'
$zipPath = Join-Path $PSScriptRoot 'artifacts\ARSVIN-win-x64-portable.zip'

if (Test-Path $zipPath) { Remove-Item $zipPath -Force }

dotnet publish $project `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:EnableCompressionInSingleFile=true `
  -p:DebugType=None `
  -p:DebugSymbols=false `
  -o $out

Copy-Item (Join-Path $PSScriptRoot 'README.md') (Join-Path $out 'README.md') -Force
Copy-Item (Join-Path $PSScriptRoot 'LICENSE') (Join-Path $out 'LICENSE.txt') -Force
Compress-Archive -Path (Join-Path $out '*') -DestinationPath $zipPath -Force
Write-Host "Created $zipPath"
