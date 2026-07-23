[CmdletBinding()]
param(
    [string] $EngineRef = $(if ($env:ARIEC61850_REF) { $env:ARIEC61850_REF } else { 'agent/sv-core-unification' })
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$root = $PSScriptRoot
$solution = Join-Path $root 'ARSVIN.sln'
$appProject = Join-Path $root 'src\ARSVIN\ARSVIN.csproj'
$subscriberProject = Join-Path $root 'src\ARSVIN.Subscriber\ARSVIN.Subscriber.csproj'
$testProject = Join-Path $root 'tests\ARSVIN.Tests\ARSVIN.Tests.csproj'
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
        throw "ARIEC61850 sibling repository was not found at $engineRoot. Clone https://github.com/masarray/ARIEC61850 beside this repository or set ARIEC61850_ROOT."
    }

    Write-Host "==> CI bootstrap: cloning ARIEC61850 ref $EngineRef into $engineRoot"
    $engineParent = Split-Path -Parent $engineRoot
    New-Item -ItemType Directory -Path $engineParent -Force | Out-Null
    if (Test-Path $engineRoot) {
        Remove-Item $engineRoot -Recurse -Force
    }
    Invoke-Checked -FilePath 'git' -Arguments @(
        'clone', '--depth', '1', '--branch', $EngineRef,
        'https://github.com/masarray/ARIEC61850.git', $engineRoot
    )
}

$env:ARIEC61850_ROOT = $engineRoot
$env:ARIEC61850_REF = $EngineRef
if ($env:GITHUB_ENV) {
    "ARIEC61850_ROOT=$engineRoot" | Add-Content $env:GITHUB_ENV
    "ARIEC61850_REF=$EngineRef" | Add-Content $env:GITHUB_ENV
}

$engineSha = Invoke-Expression "git -C `"$engineRoot`" rev-parse HEAD"
Write-Host "==> ARIEC61850 root: $engineRoot"
Write-Host "==> ARIEC61850 ref: $EngineRef"
Write-Host "==> ARIEC61850 commit: $engineSha"

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

Write-Host '==> Restoring application and sibling-engine dependency graph'
Invoke-DotNet -Arguments @('restore', $solution)

Write-Host '==> Building ARSVIN Publisher'
Invoke-DotNet -Arguments @('build', $appProject, '-c', 'Release', '--no-restore', '-warnaserror')

Write-Host '==> Building ArSubsv Subscriber'
Invoke-DotNet -Arguments @('build', $subscriberProject, '-c', 'Release', '--no-restore', '-warnaserror')

Write-Host '==> Running integration regression tests against ARIEC61850'
Invoke-DotNet -Arguments @('test', $testProject, '-c', 'Release', '--no-restore', '/p:TreatWarningsAsErrors=true')

Write-Host '==> Build completed successfully'
