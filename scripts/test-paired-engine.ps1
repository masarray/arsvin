[CmdletBinding()]
param(
    [string] $EngineRoot = $(if ($env:ARIEC61850_ROOT) { $env:ARIEC61850_ROOT } else { Join-Path (Split-Path -Parent $PSScriptRoot) '..\ARIEC61850' }),
    [switch] $SkipEngineTests,
    [switch] $SkipCoverage
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$root = Split-Path -Parent $PSScriptRoot
$pin = & (Join-Path $root 'scripts\resolve-engine-pin.ps1') -RepositoryRoot $root -AsObject
$engineRootFull = [System.IO.Path]::GetFullPath($EngineRoot)
$engineSolution = Join-Path $engineRootFull 'ARIEC61850.sln'

if (-not (Test-Path $engineSolution -PathType Leaf)) {
    throw "ARIEC61850 sibling solution was not found: $engineSolution"
}

$engineSha = (& git -C $engineRootFull rev-parse HEAD 2>&1 | Select-Object -First 1).Trim().ToLowerInvariant()
if ($LASTEXITCODE -ne 0) {
    throw "Could not resolve ARIEC61850 HEAD at $engineRootFull."
}
if ($engineSha -ne $pin.Commit) {
    throw "Paired engine mismatch. Expected $($pin.Commit), found $engineSha. Checkout the pinned commit before testing."
}

$env:ARIEC61850_ROOT = $engineRootFull
$env:ARIEC61850_REF = $pin.Ref
$env:ARIEC61850_COMMIT = $pin.Commit

function Invoke-DotNet {
    param([Parameter(Mandatory)][string[]] $Arguments)
    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet $($Arguments -join ' ') failed with exit code $LASTEXITCODE."
    }
}

if (-not $SkipEngineTests) {
    Write-Host '==> Restoring and testing pinned ARIEC61850'
    Invoke-DotNet @('restore', $engineSolution)
    Invoke-DotNet @('build', $engineSolution, '-c', 'Release', '--no-restore', '-warnaserror')
    Invoke-DotNet @('test', $engineSolution, '-c', 'Release', '--no-build', '/p:TreatWarningsAsErrors=true')
}

Write-Host '==> Building and testing ARSVIN applications against pinned engine'
& (Join-Path $root 'build.ps1') -EngineCommit $pin.Commit -EngineRef $pin.Ref
if ($LASTEXITCODE -ne 0) {
    throw "Paired ARSVIN build failed with exit code $LASTEXITCODE."
}

if (-not $SkipCoverage) {
    Write-Host '==> Running ARSVIN integration coverage gates against pinned engine'
    & (Join-Path $root 'scripts\test-with-coverage.ps1') -MinimumWholeEngineLineCoverage 14.25 -MinimumLineCoverage 72.5 -NoRestore
    if ($LASTEXITCODE -ne 0) {
        throw "Paired coverage validation failed with exit code $LASTEXITCODE."
    }
}

$applicationSha = (& git -C $root rev-parse HEAD 2>&1 | Select-Object -First 1).Trim()
$reportRoot = Join-Path $root 'artifacts\paired-validation'
New-Item -ItemType Directory -Path $reportRoot -Force | Out-Null
[ordered]@{
    schemaVersion = 1
    result = 'passed'
    applicationRepository = 'masarray/arsvin'
    applicationCommit = $applicationSha
    engineRepository = $pin.Repository
    engineRef = $pin.Ref
    engineCommit = $engineSha
    engineTestsExecuted = (-not $SkipEngineTests)
    coverageExecuted = (-not $SkipCoverage)
    generatedAt = [DateTimeOffset]::UtcNow.ToString('O')
} | ConvertTo-Json -Depth 4 | Set-Content (Join-Path $reportRoot 'paired-validation.json') -Encoding utf8

Write-Host "==> Paired validation passed: ARSVIN $applicationSha + ARIEC61850 $engineSha"
