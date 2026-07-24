[CmdletBinding()]
param(
    [string] $EngineCommit = $(if ($env:ARIEC61850_COMMIT) { $env:ARIEC61850_COMMIT } else { '' }),
    [string] $EngineRef = $(if ($env:ARIEC61850_REF) { $env:ARIEC61850_REF } else { '' }),
    [switch] $AllowEngineDrift
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$root = $PSScriptRoot
$solution = Join-Path $root 'ARSVIN.sln'
$appProject = Join-Path $root 'src\ARSVIN\ARSVIN.csproj'
$subscriberProject = Join-Path $root 'src\ARSVIN.Subscriber\ARSVIN.Subscriber.csproj'
$testProject = Join-Path $root 'tests\ARSVIN.Tests\ARSVIN.Tests.csproj'
$pin = & (Join-Path $root 'scripts\resolve-engine-pin.ps1') -RepositoryRoot $root -AsObject
$resolvedEngineCommit = if ([string]::IsNullOrWhiteSpace($EngineCommit)) { $pin.Commit } else { $EngineCommit.Trim().ToLowerInvariant() }
$resolvedEngineRef = if ([string]::IsNullOrWhiteSpace($EngineRef)) { $pin.Ref } else { $EngineRef.Trim() }

if ($resolvedEngineCommit -notmatch '^[0-9a-f]{40}$') {
    throw "ARIEC61850 commit '$resolvedEngineCommit' is not a full 40-character SHA."
}

$engineRoot = if ($env:ARIEC61850_ROOT) {
    [System.IO.Path]::GetFullPath($env:ARIEC61850_ROOT)
}
else {
    [System.IO.Path]::GetFullPath((Join-Path $root '..\ARIEC61850'))
}
$engineProject = Join-Path $engineRoot 'src\AR.Iec61850\AR.Iec61850.csproj'

function Invoke-Checked {
    param(
        [Parameter(Mandatory)][string] $FilePath,
        [Parameter(Mandatory)][string[]] $Arguments
    )

    & $FilePath @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "$FilePath $($Arguments -join ' ') failed with exit code $LASTEXITCODE."
    }
}

function Invoke-DotNet {
    param([Parameter(Mandatory)][string[]] $Arguments)
    Invoke-Checked -FilePath 'dotnet' -Arguments $Arguments
}

if (-not (Test-Path $engineProject -PathType Leaf)) {
    if ($env:CI -ne 'true') {
        throw "ARIEC61850 sibling repository was not found at $engineRoot. Clone https://github.com/$($pin.Repository) beside this repository, checkout $resolvedEngineCommit, or set ARIEC61850_ROOT."
    }

    Write-Host "==> CI bootstrap: fetching pinned ARIEC61850 commit $resolvedEngineCommit into $engineRoot"
    $engineParent = Split-Path -Parent $engineRoot
    New-Item -ItemType Directory -Path $engineParent -Force | Out-Null
    if (Test-Path $engineRoot) {
        Remove-Item $engineRoot -Recurse -Force
    }
    New-Item -ItemType Directory -Path $engineRoot -Force | Out-Null
    Invoke-Checked -FilePath 'git' -Arguments @('-C', $engineRoot, 'init')
    Invoke-Checked -FilePath 'git' -Arguments @('-C', $engineRoot, 'remote', 'add', 'origin', $pin.RepositoryUrl)
    Invoke-Checked -FilePath 'git' -Arguments @('-C', $engineRoot, 'fetch', '--depth', '1', 'origin', $resolvedEngineCommit)
    Invoke-Checked -FilePath 'git' -Arguments @('-C', $engineRoot, 'checkout', '--detach', 'FETCH_HEAD')
}

$engineShaOutput = & git -C $engineRoot rev-parse HEAD 2>&1
if ($LASTEXITCODE -ne 0) {
    throw "Could not resolve the paired ARIEC61850 commit.`n$($engineShaOutput -join [Environment]::NewLine)"
}
$engineSha = ($engineShaOutput -join '').Trim().ToLowerInvariant()

if (-not $AllowEngineDrift -and $engineSha -ne $resolvedEngineCommit) {
    throw "ARIEC61850 checkout mismatch. Expected $resolvedEngineCommit but found $engineSha. Run: git -C `"$engineRoot`" fetch origin $resolvedEngineCommit; git -C `"$engineRoot`" checkout --detach $resolvedEngineCommit"
}

$env:ARIEC61850_ROOT = $engineRoot
$env:ARIEC61850_REF = $resolvedEngineRef
$env:ARIEC61850_COMMIT = $engineSha
if ($env:GITHUB_ENV) {
    "ARIEC61850_ROOT=$engineRoot" | Add-Content $env:GITHUB_ENV
    "ARIEC61850_REF=$resolvedEngineRef" | Add-Content $env:GITHUB_ENV
    "ARIEC61850_COMMIT=$engineSha" | Add-Content $env:GITHUB_ENV
}

$artifactRoot = Join-Path $root 'artifacts'
New-Item -ItemType Directory -Path $artifactRoot -Force | Out-Null
$revisionEvidence = [ordered]@{
    schemaVersion = 1
    repository = $pin.Repository
    configuredRef = $resolvedEngineRef
    expectedCommit = $resolvedEngineCommit
    actualCommit = $engineSha
    exactMatch = ($engineSha -eq $resolvedEngineCommit)
    engineRoot = $engineRoot
    applicationCommit = (& git -C $root rev-parse HEAD 2>$null | Select-Object -First 1)
    generatedAt = [DateTimeOffset]::UtcNow.ToString('O')
}
$revisionEvidence | ConvertTo-Json -Depth 4 | Set-Content (Join-Path $artifactRoot 'engine-revision.json') -Encoding utf8

Write-Host "==> ARIEC61850 root: $engineRoot"
Write-Host "==> ARIEC61850 ref: $resolvedEngineRef"
Write-Host "==> ARIEC61850 expected commit: $resolvedEngineCommit"
Write-Host "==> ARIEC61850 actual commit: $engineSha"

Write-Host '==> Verifying current license, provenance, and public wording'
& python (Join-Path $root 'scripts\verify-current-license.py')
if ($LASTEXITCODE -ne 0) {
    throw 'Current-license and public-wording verification failed.'
}

Write-Host '==> Validating neutral public terminology'
& python (Join-Path $root 'scripts\validate-public-neutrality.py')
if ($LASTEXITCODE -ne 0) {
    throw 'Public terminology neutrality validation failed.'
}

Write-Host '==> Validating ARIEC61850 engine ownership boundary'
& python (Join-Path $root 'scripts\validate-engine-ownership.py')
if ($LASTEXITCODE -ne 0) {
    throw 'Engine ownership validation failed.'
}

Write-Host '==> Restoring application and pinned sibling-engine dependency graph'
Invoke-DotNet -Arguments @('restore', $solution)

Write-Host '==> Building ARSVIN Publisher'
Invoke-DotNet -Arguments @('build', $appProject, '-c', 'Release', '--no-restore', '-warnaserror')

Write-Host '==> Building ArSubsv Subscriber'
Invoke-DotNet -Arguments @('build', $subscriberProject, '-c', 'Release', '--no-restore', '-warnaserror')

Write-Host '==> Running integration regression tests against pinned ARIEC61850'
Invoke-DotNet -Arguments @('test', $testProject, '-c', 'Release', '--no-restore', '/p:TreatWarningsAsErrors=true')

Write-Host '==> Paired build completed successfully'
