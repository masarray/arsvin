[CmdletBinding()]
param(
    [ValidateRange(0, 100)]
    [double] $MinimumLineCoverage = 20,

    [switch] $NoRestore
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$root = Split-Path -Parent $PSScriptRoot
$testProject = Join-Path $root 'tests\ARSVIN.Tests\ARSVIN.Tests.csproj'
$resultsRoot = Join-Path $root 'artifacts\test-results'

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

$arguments += @(
    '/p:TreatWarningsAsErrors=true',
    '--logger', 'trx;LogFileName=ARSVIN.Tests.trx',
    '--results-directory', $resultsRoot,
    '--collect', 'XPlat Code Coverage',
    '--',
    'DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=cobertura',
    'DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.ExcludeByFile=**/*.g.cs,**/*.g.i.cs,**/obj/**'
)

& dotnet @arguments
if ($LASTEXITCODE -ne 0) {
    throw "dotnet test with coverage failed with exit code $LASTEXITCODE."
}

$coverageFile = Get-ChildItem $resultsRoot -Recurse -Filter 'coverage.cobertura.xml' -File |
    Sort-Object LastWriteTimeUtc -Descending |
    Select-Object -First 1

if (-not $coverageFile) {
    throw "Coverage collector did not produce coverage.cobertura.xml under $resultsRoot."
}

$normalizedCoverage = Join-Path $resultsRoot 'coverage.cobertura.xml'
if ($coverageFile.FullName -ne $normalizedCoverage) {
    Copy-Item $coverageFile.FullName $normalizedCoverage -Force
}

[xml] $coverage = Get-Content $normalizedCoverage -Raw
$lineRateText = [string] $coverage.coverage.'line-rate'
if ([string]::IsNullOrWhiteSpace($lineRateText)) {
    throw 'Cobertura report does not contain a root line-rate value.'
}

$lineRate = [double]::Parse(
    $lineRateText,
    [System.Globalization.CultureInfo]::InvariantCulture
)
$lineCoverage = [Math]::Round($lineRate * 100, 2)

Write-Host "Line coverage: $lineCoverage%"
Write-Host "Minimum required: $MinimumLineCoverage%"
Write-Host "Coverage report: $normalizedCoverage"

if ($env:GITHUB_STEP_SUMMARY) {
    @"
## Test coverage

| Metric | Result |
|---|---:|
| Line coverage | **$lineCoverage%** |
| Required minimum | **$MinimumLineCoverage%** |
| Report | `artifacts/test-results/coverage.cobertura.xml` |
"@ | Add-Content $env:GITHUB_STEP_SUMMARY
}

if ($lineCoverage -lt $MinimumLineCoverage) {
    throw "Line coverage $lineCoverage% is below the required $MinimumLineCoverage%."
}
