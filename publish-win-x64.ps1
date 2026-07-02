$ErrorActionPreference = 'Stop'

$root = $PSScriptRoot
$artifactRoot = Join-Path $root 'artifacts'
$out = Join-Path $artifactRoot 'ARSVIN-win-x64'
$publisherOut = Join-Path $out 'Publisher'
$subscriberOut = Join-Path $out 'Subscriber'
$publisherProject = Join-Path $root 'src\ARSVIN\ARSVIN.csproj'
$subscriberProject = Join-Path $root 'src\ARSVIN.Subscriber\ARSVIN.Subscriber.csproj'
$zipPath = Join-Path $artifactRoot 'ARSVIN-win-x64-portable.zip'

if (Test-Path $out) { Remove-Item $out -Recurse -Force }
New-Item -ItemType Directory -Path $publisherOut -Force | Out-Null
New-Item -ItemType Directory -Path $subscriberOut -Force | Out-Null

if (Test-Path $zipPath) { Remove-Item $zipPath -Force }

Write-Host '==> Publishing ARSVIN Publisher win-x64 portable package'
dotnet publish $publisherProject `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:EnableCompressionInSingleFile=true `
  -p:DebugType=None `
  -p:DebugSymbols=false `
  -o $publisherOut

Write-Host '==> Publishing ARSVIN Subscriber win-x64 portable package'
dotnet publish $subscriberProject `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:EnableCompressionInSingleFile=true `
  -p:DebugType=None `
  -p:DebugSymbols=false `
  -o $subscriberOut

Copy-Item (Join-Path $root 'README.md') (Join-Path $out 'README.md') -Force
Copy-Item (Join-Path $root 'LICENSE') (Join-Path $out 'LICENSE.txt') -Force
Copy-Item (Join-Path $root 'NOTICE') (Join-Path $out 'NOTICE.txt') -Force
Copy-Item (Join-Path $root 'THIRD_PARTY_NOTICES.md') (Join-Path $out 'THIRD_PARTY_NOTICES.md') -Force

$docsOut = Join-Path $out 'docs'
New-Item -ItemType Directory -Path $docsOut -Force | Out-Null
Copy-Item (Join-Path $root 'docs\quick-start.md') $docsOut -Force
Copy-Item (Join-Path $root 'docs\live-mode-safety.md') $docsOut -Force
Copy-Item (Join-Path $root 'docs\known-limitations.md') $docsOut -Force
Copy-Item (Join-Path $root 'docs\safety-boundaries.md') $docsOut -Force
Copy-Item (Join-Path $root 'docs\subscriber-verification-app.md') $docsOut -Force

Compress-Archive -Path (Join-Path $out '*') -DestinationPath $zipPath -Force
Write-Host "Created $zipPath"
