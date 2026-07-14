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

$solution = Join-Path $root 'ARSVIN.sln'
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
    param([Parameter(Mandatory)][string[]] $Arguments)
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

function Copy-RequiredFile {
    param(
        [Parameter(Mandatory)][string] $Source,
        [Parameter(Mandatory)][string] $Destination
    )
    if (-not (Test-Path $Source -PathType Leaf)) {
        throw "Required release document was not found: $Source"
    }
    Copy-Item $Source $Destination -Force
}

function Copy-ReleaseDocumentation {
    param([Parameter(Mandatory)][string] $Destination)

    New-Item -ItemType Directory -Path $Destination -Force | Out-Null

    $rootDocuments = @(
        @{ Source = 'README.md'; Destination = 'README.md' },
        @{ Source = 'LICENSE'; Destination = 'LICENSE.txt' },
        @{ Source = 'NOTICE'; Destination = 'NOTICE.txt' },
        @{ Source = 'COMMERCIAL-LICENSE.md'; Destination = 'COMMERCIAL-LICENSE.md' },
        @{ Source = 'COPYRIGHT.md'; Destination = 'COPYRIGHT.md' },
        @{ Source = 'TRADEMARK.md'; Destination = 'TRADEMARK.md' },
        @{ Source = 'THIRD_PARTY_NOTICES.md'; Destination = 'THIRD_PARTY_NOTICES.md' }
    )

    foreach ($document in $rootDocuments) {
        Copy-RequiredFile `
            -Source (Join-Path $root $document.Source) `
            -Destination (Join-Path $Destination $document.Destination)
    }

    $docsOut = Join-Path $Destination 'docs'
    New-Item -ItemType Directory -Path $docsOut -Force | Out-Null

    $documents = @(
        'LICENSING.md',
        'quick-start.md',
        'live-mode-safety.md',
        'known-limitations.md',
        'safety-boundaries.md',
        'subscriber-verification-app.md',
        'arsubsv-sv-scout-companion.md'
    )

    foreach ($document in $documents) {
        $source = Join-Path $root "docs\$document"
        if (Test-Path $source -PathType Leaf) {
            Copy-Item $source $docsOut -Force
        }
    }

    $samplesSource = Join-Path $root 'samples'
    if (Test-Path $samplesSource) {
        Copy-Item $samplesSource (Join-Path $Destination 'samples') -Recurse -Force
    }

    $forbiddenHistoricalLicense = Join-Path $Destination 'LICENSE-APACHE-2.0'
    if (Test-Path $forbiddenHistoricalLicense) {
        throw "Historical Apache license must not be included in a current GPL release: $forbiddenHistoricalLicense"
    }

    $licenseText = Get-Content (Join-Path $Destination 'LICENSE.txt') -Raw
    if ($licenseText -notmatch 'GNU GENERAL PUBLIC LICENSE' -or $licenseText -notmatch 'Version 3') {
        throw 'Current release LICENSE.txt is not the GNU General Public License version 3 text.'
    }
}

Reset-Directory $releaseRoot
Reset-Directory $tempRoot
Reset-Directory $portableRoot
Reset-Directory $installerInput

if (-not $SkipTests) {
    Write-Host '==> Restoring locked solution graph and testing'
    Invoke-DotNet -Arguments @('restore', $solution, '--locked-mode')
    Invoke-DotNet -Arguments @('test', $testProject, '-c', 'Release', '--no-restore', '/p:TreatWarningsAsErrors=true')
}

Write-Host "==> Restoring locked publish graph for $Runtime"
Invoke-DotNet -Arguments @('restore', $publisherProject, '-r', $Runtime, '--locked-mode')
Invoke-DotNet -Arguments @('restore', $subscriberProject, '-r', $Runtime, '--locked-mode')

$commonPublishArguments = @(
    '-c', 'Release',
    '-r', $Runtime,
    '--self-contained', 'true',
    '--no-restore',
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
if (-not (Test-Path $publisherExe)) { throw "Publisher executable was not found: $publisherExe" }
if (-not (Test-Path $subscriberExe)) { throw "Subscriber executable was not found: $subscriberExe" }

Copy-Item $publisherExe (Join-Path $releaseRoot 'ARSVIN-Publisher-win-x64.exe') -Force
Copy-Item $subscriberExe (Join-Path $releaseRoot 'ArSubsv-Subscriber-win-x64.exe') -Force

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
Community license: GPL-3.0-or-later
Commercial rights: separate negotiated agreement only
"@
Set-Content -Path (Join-Path $portableRoot 'VERSION.txt') -Value $versionFile -Encoding utf8
Set-Content -Path (Join-Path $installerInput 'VERSION.txt') -Value $versionFile -Encoding utf8

$portableZip = Join-Path $releaseRoot 'ARSVIN-win-x64-portable.zip'
Compress-Archive -Path (Join-Path $portableRoot '*') -DestinationPath $portableZip -CompressionLevel Optimal -Force

Write-Host '==> Portable release artifacts'
Get-ChildItem $releaseRoot -File | Select-Object Name, Length | Format-Table -AutoSize
Write-Host "Release output: $releaseRoot"
Write-Host "Installer input: $installerInput"