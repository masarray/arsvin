[CmdletBinding()]
param(
    [ValidateRange(0, 100)]
    [double] $MinimumLineCoverage = 70,

    [ValidateRange(0, 100)]
    [double] $MinimumWholeEngineLineCoverage = 13,

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

# Instrument the complete production engine. CI protects both the truthful
# whole-engine baseline and the higher protocol-core regression baseline.
$arguments += @(
    '/p:RestoreLockedMode=true',
    '/p:TreatWarningsAsErrors=true',
    '/p:CollectCoverage=true',
    '/p:Include=[ARSVIN.Engine]*',
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
if ([string]::IsNullOrWhiteSpace($overallLineRateText)) {
    throw 'Cobertura report does not contain a root line-rate value.'
}

$overallLinesValid = 0
if (-not [int]::TryParse($overallLinesValidText, [ref] $overallLinesValid) -or $overallLinesValid -le 0) {
    throw 'Coverage report contains no instrumented production source lines.'
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

Write-Host "Whole engine lines: $overallLinesValid"
Write-Host "Whole engine line coverage: $overallLineCoverage%"
Write-Host "Whole engine minimum required: $MinimumWholeEngineLineCoverage%"
Write-Host "Protocol core files: $($coreFiles.Count)"
Write-Host "Protocol core lines: $coreLinesValid"
Write-Host "Protocol core covered lines: $coreLinesCovered"
Write-Host "Protocol core line coverage: $coreLineCoverage%"
Write-Host "Protocol core minimum required: $MinimumLineCoverage%"
Write-Host "Coverage report: $coverageFile"

if ($env:GITHUB_STEP_SUMMARY) {
    @"
## Test coverage

| Metric | Result |
|---|---:|
| Whole `ARSVIN.Engine` instrumented lines | **$overallLinesValid** |
| Whole engine line coverage | **$overallLineCoverage%** |
| Whole-engine regression floor | **$MinimumWholeEngineLineCoverage%** |
| Tested protocol-core files | **$($coreFiles.Count)** |
| Protocol-core instrumented lines | **$coreLinesValid** |
| Protocol-core covered lines | **$coreLinesCovered** |
| Protocol-core line coverage | **$coreLineCoverage%** |
| Protocol-core regression floor | **$MinimumLineCoverage%** |
| Report | `artifacts/test-results/coverage.cobertura.xml` |
"@ | Add-Content $env:GITHUB_STEP_SUMMARY
}

$coverageFailures = [System.Collections.Generic.List[string]]::new()
if ($overallLineCoverage -lt $MinimumWholeEngineLineCoverage) {
    $coverageFailures.Add("Whole-engine line coverage $overallLineCoverage% is below the required $MinimumWholeEngineLineCoverage%.")
}
if ($coreLineCoverage -lt $MinimumLineCoverage) {
    $coverageFailures.Add("Protocol-core line coverage $coreLineCoverage% is below the required $MinimumLineCoverage%.")
}

if ($coverageFailures.Count -gt 0) {
    throw ($coverageFailures -join ' ')
}
