[CmdletBinding()]
param(
    [ValidateRange(0, 100)]
    [double] $MinimumLineCoverage = 72.5,

    [ValidateRange(1, [int]::MaxValue)]
    [int] $MinimumWholeEngineCoveredLines = 3000,

    [ValidateRange(1, [int]::MaxValue)]
    [int] $MinimumProtocolCoreCoveredLines = 2300,

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

# ARSVIN tests execute against the pinned sibling ARIEC61850 source of truth.
# The engine repository owns its full-suite percentage gate, including the P4 Field layer.
# This application integration gate checks:
#   1. the application-consumed protocol core remains strongly covered; and
#   2. the absolute amount of exercised production code does not regress when the engine grows.
$arguments += @(
    '/p:RestoreLockedMode=true',
    '/p:TreatWarningsAsErrors=true',
    '/p:CollectCoverage=true',
    '/p:Include=[AR.Iec61850]*',
    '/p:ExcludeAssembliesWithoutSources=None',
    '/p:CoverletOutputFormat=cobertura',
    "/p:CoverletOutput=$coveragePrefix",
    '/p:DeterministicReport=true',
    '/p:ExcludeByFile=**/*.g.cs%2c**/*.g.i.cs%2c**/obj/**',
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
$overallLineRateText = [string] $coverage.coverage.'line-rate'
$overallLinesValidText = [string] $coverage.coverage.'lines-valid'
$overallLinesCoveredText = [string] $coverage.coverage.'lines-covered'
if ([string]::IsNullOrWhiteSpace($overallLineRateText)) {
    throw 'Cobertura report does not contain a root line-rate value.'
}

$overallLinesValid = 0
if (-not [int]::TryParse($overallLinesValidText, [ref] $overallLinesValid) -or $overallLinesValid -le 0) {
    throw 'Coverage report contains no instrumented production source lines.'
}

$overallLinesCovered = 0
if (-not [int]::TryParse($overallLinesCoveredText, [ref] $overallLinesCovered) -or $overallLinesCovered -le 0) {
    throw 'Coverage report contains no covered production source lines.'
}

$overallLineRate = [double]::Parse(
    $overallLineRateText,
    [System.Globalization.CultureInfo]::InvariantCulture
)
$overallLineCoverage = [Math]::Round($overallLineRate * 100, 2)

function Test-IsProtocolCoreFile {
    param([Parameter(Mandatory)][string] $Filename)

    $path = $Filename.Replace('\', '/')
    $leaf = [System.IO.Path]::GetFileName($path)

    if ($path.Contains('/AR.Iec61850/Asn1/')) { return $true }
    if ($path.Contains('/AR.Iec61850/Ethernet/')) { return $true }
    if ($path.Contains('/AR.Iec61850/Transports/')) { return $true }

    # P4 Field contracts are tested by ARIEC61850's own deterministic suite. Excluding them here
    # prevents application coverage from being diluted merely because the shared engine grows.
    if ($path.Contains('/AR.Iec61850/SampledValues/Field/')) { return $false }
    if ($path.Contains('/AR.Iec61850/SampledValues/')) { return $true }

    if ($path.Contains('/AR.Iec61850/Capture/')) {
        return $leaf -in @('PcapPacket.cs', 'PcapWriter.cs')
    }

    if ($path.Contains('/AR.Iec61850/Mms/')) {
        return $leaf -in @('Iec61850UtcTime.cs', 'MmsBinaryTime.cs', 'MmsDataKind.cs', 'MmsDataValue.cs')
    }

    if ($path.Contains('/AR.Iec61850/Scl/')) {
        return $leaf -in @('SclModels.cs', 'SclProfileException.cs')
    }

    return $false
}

$coreLinesValid = 0
$coreLinesCovered = 0
$coreFiles = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)

foreach ($class in @($coverage.coverage.packages.package.classes.class)) {
    $filename = [string] $class.filename
    if ([string]::IsNullOrWhiteSpace($filename) -or -not (Test-IsProtocolCoreFile -Filename $filename)) {
        continue
    }

    $null = $coreFiles.Add($filename)
    foreach ($line in @($class.lines.line)) {
        $coreLinesValid++
        if ([int] $line.hits -gt 0) {
            $coreLinesCovered++
        }
    }
}

if ($coreLinesValid -le 0) {
    throw 'Coverage report contains no protocol-core source lines.'
}

$coreLineCoverage = [Math]::Round(($coreLinesCovered / $coreLinesValid) * 100, 2)

Write-Host "ARIEC61850 instrumented lines: $overallLinesValid"
Write-Host "ARIEC61850 covered lines exercised by ARSVIN: $overallLinesCovered"
Write-Host "Minimum covered production lines: $MinimumWholeEngineCoveredLines"
Write-Host "Informational whole-engine line coverage: $overallLineCoverage%"
Write-Host "Application-consumed protocol core files: $($coreFiles.Count)"
Write-Host "Application-consumed protocol core lines: $coreLinesValid"
Write-Host "Application-consumed protocol core covered lines: $coreLinesCovered"
Write-Host "Minimum protocol-core covered lines: $MinimumProtocolCoreCoveredLines"
Write-Host "Protocol core line coverage: $coreLineCoverage%"
Write-Host "Protocol core minimum percentage: $MinimumLineCoverage%"
Write-Host "Coverage report: $coverageFile"

if ($env:GITHUB_STEP_SUMMARY) {
    @"
## ARSVIN integration coverage against pinned ARIEC61850

ARIEC61850 owns the full-engine and P4 Field-layer test gates. This paired gate measures the reusable protocol core exercised directly by ARSVIN Publisher and ArSubsv.

| Metric | Result |
|---|---:|
| Whole `AR.Iec61850` instrumented lines | **$overallLinesValid** |
| Production lines exercised by ARSVIN | **$overallLinesCovered** |
| Minimum exercised production lines | **$MinimumWholeEngineCoveredLines** |
| Informational whole-engine line coverage | **$overallLineCoverage%** |
| Tested application-consumed protocol-core files | **$($coreFiles.Count)** |
| Protocol-core instrumented lines | **$coreLinesValid** |
| Protocol-core covered lines | **$coreLinesCovered** |
| Minimum protocol-core covered lines | **$MinimumProtocolCoreCoveredLines** |
| Protocol-core line coverage | **$coreLineCoverage%** |
| Protocol-core percentage floor | **$MinimumLineCoverage%** |
| Report | `artifacts/test-results/coverage.cobertura.xml` |
"@ | Add-Content $env:GITHUB_STEP_SUMMARY
}

$coverageFailures = [System.Collections.Generic.List[string]]::new()
if ($overallLinesCovered -lt $MinimumWholeEngineCoveredLines) {
    $coverageFailures.Add("ARSVIN exercised $overallLinesCovered production lines, below the required $MinimumWholeEngineCoveredLines.")
}
if ($coreLinesCovered -lt $MinimumProtocolCoreCoveredLines) {
    $coverageFailures.Add("ARSVIN exercised $coreLinesCovered protocol-core lines, below the required $MinimumProtocolCoreCoveredLines.")
}
if ($coreLineCoverage -lt $MinimumLineCoverage) {
    $coverageFailures.Add("Protocol-core line coverage $coreLineCoverage% is below the required $MinimumLineCoverage%.")
}

if ($coverageFailures.Count -gt 0) {
    throw ($coverageFailures -join ' ')
}
