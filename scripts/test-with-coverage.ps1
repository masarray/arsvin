[CmdletBinding()]
param(
    [ValidateRange(0, 100)]
    [double] $MinimumLineCoverage = 50,

    [switch] $NoRestore
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$root = Split-Path -Parent $PSScriptRoot
$testProject = Join-Path $root 'tests\ARSVIN.Tests\ARSVIN.Tests.csproj'
$resultsRoot = Join-Path $root 'artifacts\test-results'
$coveragePrefix = Join-Path $resultsRoot 'coverage'
$testLog = Join-Path $resultsRoot 'dotnet-test.log'

if (Test-Path $resultsRoot) {
    Remove-Item $resultsRoot -Recurse -Force
}
New-Item -ItemType Directory -Path $resultsRoot -Force | Out-Null

$arguments = @(
    'test',
    $testProject,
    '-c', 'Release'
)

if ($NoRestore) {
    $arguments += '--no-restore'
}

# Production engine source is currently linked into ARSVIN.Tests. Force that
# assembly into the instrumentation set, keep source matching permissive for
# linked documents, and exclude test files from the resulting metric.
$arguments += @(
    '/p:TreatWarningsAsErrors=true',
    '/p:CollectCoverage=true',
    '/p:IncludeTestAssembly=true',
    '/p:Include=[ARSVIN.Tests]*',
    '/p:ExcludeAssembliesWithoutSources=None',
    '/p:CoverletOutputFormat=cobertura',
    "/p:CoverletOutput=$coveragePrefix",
    '/p:DeterministicReport=true',
    '/p:ExcludeByFile=**/tests/**%2c**/*Tests.cs%2c**/*.g.cs%2c**/*.g.i.cs%2c**/obj/**',
    '--logger', 'trx;LogFileName=ARSVIN.Tests.trx',
    '--results-directory', $resultsRoot
)

Write-Host "dotnet $($arguments -join ' ')"
& dotnet @arguments 2>&1 | Tee-Object -FilePath $testLog
$testExitCode = $LASTEXITCODE
if ($testExitCode -ne 0) {
    throw "dotnet test with coverage failed with exit code $testExitCode."
}

$coverageFile = Join-Path $resultsRoot 'coverage.cobertura.xml'
if (-not (Test-Path $coverageFile -PathType Leaf)) {
    throw "Coverlet MSBuild integration did not produce $coverageFile."
}

[xml] $coverage = Get-Content $coverageFile -Raw
$lineRateText = [string] $coverage.coverage.'line-rate'
$linesValidText = [string] $coverage.coverage.'lines-valid'
if ([string]::IsNullOrWhiteSpace($lineRateText)) {
    throw 'Cobertura report does not contain a root line-rate value.'
}

$linesValid = 0
if (-not [int]::TryParse($linesValidText, [ref] $linesValid) -or $linesValid -le 0) {
    throw 'Coverage report contains no instrumented production source lines.'
}

$lineRate = [double]::Parse(
    $lineRateText,
    [System.Globalization.CultureInfo]::InvariantCulture
)
$lineCoverage = [Math]::Round($lineRate * 100, 2)

Write-Host "Instrumented lines: $linesValid"
Write-Host "Line coverage: $lineCoverage%"
Write-Host "Minimum required: $MinimumLineCoverage%"
Write-Host "Coverage report: $coverageFile"

if ($env:GITHUB_STEP_SUMMARY) {
    @"
## Test coverage

| Metric | Result |
|---|---:|
| Instrumented production lines | **$linesValid** |
| Line coverage | **$lineCoverage%** |
| Required minimum | **$MinimumLineCoverage%** |
| Report | `artifacts/test-results/coverage.cobertura.xml` |
"@ | Add-Content $env:GITHUB_STEP_SUMMARY
}

if ($lineCoverage -lt $MinimumLineCoverage) {
    throw "Line coverage $lineCoverage% is below the required $MinimumLineCoverage%."
}
