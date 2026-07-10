param(
    [string] $Version = '0.0.0-dev',
    [string] $Runtime = 'win-x64',
    [switch] $SkipTests
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$root = Split-Path -Parent $PSScriptRoot
$artifactRoot = Join-Path $root 'artifacts'
$releaseRoot = Join-Path $artifactRoot 'release'
$tempRoot = Join-Path $artifactRoot 'publish-temp'
$portableRoot = Join-Path $artifactRoot 'ARSVIN-win-x64'
$installerInput = Join-Path $artifactRoot 'installer-input'

$publisherProject = Join-Path $root 'src\ARSVIN\ARSVIN.csproj'
$subscriberProject = Join-Path $root 'src\ARSVIN.Subscriber\ARSVIN.Subscriber.csproj'
$testProject = Join-Path $root 'tests\ARSVIN.Tests\ARSVIN.Tests.csproj'

$normalizedVersion = ($Version.Trim() -replace '^[vV]', '')
if ($normalizedVersion -notmatch '^\d+\.\d+\.\d+(?:[-+][0-9A-Za-z.-]+)?$') {
    throw "Version '$Version' is not a supported semantic version. Example: 1.2.3 or 1.2.3-beta.1."
}

$coreVersion = ($normalizedVersion -split '[-+]')[0]
$fileVersion = "$coreVersion.0"
$informationalVersion = $normalizedVersion

function Invoke-DotNet {
    param(
        [Parameter(Mandatory)]
        [string[]] $Arguments
    )

    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet $($Arguments -join ' ') failed with exit code $LASTEXITCODE."
    }
}

function Reset-Directory {
    param([Parameter(Mandatory)][string] $Path)

    if (Test-Path $Path) {
        Remove-Item $Path -Recurse -Force
    }
    New-Item -ItemType Directory -Path $Path -Force | Out-Null
}

function Copy-ReleaseDocumentation {
    param([Parameter(Mandatory)][string] $Destination)

    New-Item -ItemType Directory -Path $Destination -Force | Out-Null

    Copy-Item (Join-Path $root 'README.md') (Join-Path $Destination 'README.md') -Force
    Copy-Item (Join-Path $root 'LICENSE') (Join-Path $Destination 'LICENSE.txt') -Force
    Copy-Item (Join-Path $root 'NOTICE') (Join-Path $Destination 'NOTICE.txt') -Force
    Copy-Item (Join-Path $root 'THIRD_PARTY_NOTICES.md') (Join-Path $Destination 'THIRD_PARTY_NOTICES.md') -Force

    $docsOut = Join-Path $Destination 'docs'
    New-Item -ItemType Directory -Path $docsOut -Force | Out-Null

    $documents = @(
        'quick-start.md',
        'live-mode-safety.md',
        'known-limitations.md',
        'safety-boundaries.md',
        'subscriber-verification-app.md',
        'arsubsv-sv-scout-companion.md'
    )

    foreach ($document in $documents) {
        $source = Join-Path $root "docs\$document"
        if (Test-Path $source) {
            Copy-Item $source $docsOut -Force
        }
    }

    $samplesSource = Join-Path $root 'samples'
    if (Test-Path $samplesSource) {
        Copy-Item $samplesSource (Join-Path $Destination 'samples') -Recurse -Force
    }
}

Reset-Directory $releaseRoot
Reset-Directory $tempRoot
Reset-Directory $portableRoot
Reset-Directory $installerInput

if (-not $SkipTests) {
    Write-Host '==> Restoring and testing'
    Invoke-DotNet -Arguments @('restore', $publisherProject)
    Invoke-DotNet -Arguments @('restore', $subscriberProject)
    Invoke-DotNet -Arguments @('restore', $testProject)
    Invoke-DotNet -Arguments @('test', $testProject, '-c', 'Release', '--no-restore')
}

$commonPublishArguments = @(
    '-c', 'Release',
    '-r', $Runtime,
    '--self-contained', 'true',
    '-p:PublishSingleFile=true',
    '-p:IncludeNativeLibrariesForSelfExtract=true',
    '-p:EnableCompressionInSingleFile=true',
    '-p:PublishReadyToRun=false',
    '-p:DebugType=None',
    '-p:DebugSymbols=false',
    "-p:Version=$normalizedVersion",
    "-p:FileVersion=$fileVersion",
    "-p:AssemblyVersion=$fileVersion",
    "-p:InformationalVersion=$informationalVersion"
)

$publisherOut = Join-Path $tempRoot 'publisher'
$subscriberOut = Join-Path $tempRoot 'subscriber'

Write-Host "==> Publishing ARSVIN Publisher $normalizedVersion"
Invoke-DotNet -Arguments (@('publish', $publisherProject) + $commonPublishArguments + @('-o', $publisherOut))

Write-Host "==> Publishing ArSubsv Subscriber $normalizedVersion"
Invoke-DotNet -Arguments (@('publish', $subscriberProject) + $commonPublishArguments + @('-o', $subscriberOut))

$publisherExe = Join-Path $publisherOut 'ARSVIN.exe'
$subscriberExe = Join-Path $subscriberOut 'ARSVIN.Subscriber.exe'

if (-not (Test-Path $publisherExe)) {
    throw "Publisher executable was not found: $publisherExe"
}
if (-not (Test-Path $subscriberExe)) {
    throw "Subscriber executable was not found: $subscriberExe"
}

$publisherPortable = Join-Path $releaseRoot 'ARSVIN-Publisher-win-x64.exe'
$subscriberPortable = Join-Path $releaseRoot 'ArSubsv-Subscriber-win-x64.exe'

Copy-Item $publisherExe $publisherPortable -Force
Copy-Item $subscriberExe $subscriberPortable -Force

Copy-Item $publisherExe (Join-Path $portableRoot 'ARSVIN.exe') -Force
Copy-Item $subscriberExe (Join-Path $portableRoot 'ArSubsv.exe') -Force
Copy-ReleaseDocumentation -Destination $portableRoot

Copy-Item $publisherExe (Join-Path $installerInput 'ARSVIN.exe') -Force
Copy-Item $subscriberExe (Join-Path $installerInput 'ArSubsv.exe') -Force
Copy-ReleaseDocumentation -Destination $installerInput

$versionFile = @"
ARSVIN Suite
Version: $normalizedVersion
Runtime: $Runtime
Publisher: ARSVIN.exe
Subscriber: ArSubsv.exe
"@
Set-Content -Path (Join-Path $portableRoot 'VERSION.txt') -Value $versionFile -Encoding utf8
Set-Content -Path (Join-Path $installerInput 'VERSION.txt') -Value $versionFile -Encoding utf8

$portableZip = Join-Path $releaseRoot 'ARSVIN-win-x64-portable.zip'
if (Test-Path $portableZip) {
    Remove-Item $portableZip -Force
}
Compress-Archive -Path (Join-Path $portableRoot '*') -DestinationPath $portableZip -CompressionLevel Optimal -Force

Write-Host '==> Portable release artifacts'
Get-ChildItem $releaseRoot -File | Select-Object Name, Length | Format-Table -AutoSize

Write-Host "Release output: $releaseRoot"
Write-Host "Installer input: $installerInput"
