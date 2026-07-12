[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidatePattern('^\d+\.\d+\.\d+(?:[-+][0-9A-Za-z.-]+)?$')]
    [string] $Version,

    [string] $OutputPath
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$root = Split-Path -Parent $PSScriptRoot
$applicationProjects = @(
    [ordered]@{
        Name = 'ARSVIN Publisher'
        Path = Join-Path $root 'src\ARSVIN\ARSVIN.csproj'
    },
    [ordered]@{
        Name = 'ArSubsv Subscriber'
        Path = Join-Path $root 'src\ARSVIN.Subscriber\ARSVIN.Subscriber.csproj'
    }
)

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path $root 'artifacts\release\ARSVIN-SBOM.cdx.json'
}
elseif (-not [System.IO.Path]::IsPathRooted($OutputPath)) {
    $OutputPath = Join-Path $root $OutputPath
}

$outputDirectory = Split-Path -Parent $OutputPath
New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null

$sourceCommitOutput = & git -C $root rev-parse HEAD 2>&1
if ($LASTEXITCODE -ne 0) {
    throw "Could not resolve the source commit.`n$($sourceCommitOutput -join [Environment]::NewLine)"
}
$sourceCommit = ($sourceCommitOutput -join '').Trim()

$sourceTimestampOutput = & git -C $root show -s --format=%cI HEAD 2>&1
if ($LASTEXITCODE -ne 0) {
    throw "Could not resolve the source commit timestamp.`n$($sourceTimestampOutput -join [Environment]::NewLine)"
}
$sourceTimestamp = [DateTimeOffset]::Parse(
    ($sourceTimestampOutput -join '').Trim(),
    [System.Globalization.CultureInfo]::InvariantCulture
).ToUniversalTime().ToString('o')

$packages = @{}

foreach ($applicationProject in $applicationProjects) {
    $projectName = [string] $applicationProject.Name
    $projectPath = [string] $applicationProject.Path

    Write-Host "==> Resolving NuGet dependencies: $projectName"
    $commandOutput = & dotnet list $projectPath package --include-transitive --format json --output-version 1 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet list package failed for $projectName with exit code $LASTEXITCODE.`n$($commandOutput -join [Environment]::NewLine)"
    }

    $jsonText = $commandOutput -join [Environment]::NewLine
    $jsonStart = $jsonText.IndexOf('{')
    $jsonEnd = $jsonText.LastIndexOf('}')
    if ($jsonStart -lt 0 -or $jsonEnd -le $jsonStart) {
        throw "Could not locate the JSON dependency graph for $projectName."
    }

    $dependencyGraph = $jsonText.Substring($jsonStart, $jsonEnd - $jsonStart + 1) | ConvertFrom-Json

    foreach ($project in @($dependencyGraph.projects)) {
        foreach ($framework in @($project.frameworks)) {
            foreach ($scopeName in @('topLevelPackages', 'transitivePackages')) {
                $property = $framework.PSObject.Properties[$scopeName]
                if (-not $property) {
                    continue
                }

                foreach ($package in @($property.Value)) {
                    $id = [string] $package.id
                    $resolvedVersion = [string] $package.resolvedVersion
                    if ([string]::IsNullOrWhiteSpace($resolvedVersion)) {
                        $resolvedVersion = [string] $package.version
                    }

                    if ([string]::IsNullOrWhiteSpace($id) -or [string]::IsNullOrWhiteSpace($resolvedVersion)) {
                        continue
                    }

                    $key = "$($id.ToLowerInvariant())|$resolvedVersion"
                    $isTopLevel = $scopeName -eq 'topLevelPackages'

                    if (-not $packages.ContainsKey($key)) {
                        $packages[$key] = [pscustomobject]@{
                            Id = $id
                            Version = $resolvedVersion
                            Scope = if ($isTopLevel) { 'direct' } else { 'transitive' }
                            UsedBy = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
                        }
                    }
                    elseif ($isTopLevel) {
                        $packages[$key].Scope = 'direct'
                    }

                    $null = $packages[$key].UsedBy.Add($projectName)
                }
            }
        }
    }
}

if ($packages.Count -eq 0) {
    throw 'The application dependency graphs did not contain any resolved NuGet packages.'
}

$sortedPackages = @(
    $packages.Values |
        Sort-Object `
            @{ Expression = { ([string] $_.Id).ToLowerInvariant() } }, `
            @{ Expression = { [string] $_.Version } }
)

$testOnlyPackagePatterns = @(
    '^coverlet(?:\.|$)',
    '^xunit(?:\.|$)',
    '^Microsoft\.NET\.Test\.Sdk$'
)
$testOnlyPackages = @(
    $sortedPackages | Where-Object {
        $packageId = [string] $_.Id
        $testOnlyPackagePatterns | Where-Object { $packageId -match $_ }
    }
)
if ($testOnlyPackages.Count -gt 0) {
    $names = $testOnlyPackages.Id -join ', '
    throw "Release SBOM unexpectedly contains test-only packages: $names"
}

$components = foreach ($package in $sortedPackages) {
    $escapedId = [Uri]::EscapeDataString([string] $package.Id)
    $escapedVersion = [Uri]::EscapeDataString([string] $package.Version)
    $purl = "pkg:nuget/$escapedId@$escapedVersion"
    $usedBy = @($package.UsedBy | Sort-Object) -join ', '

    [ordered]@{
        type = 'library'
        'bom-ref' = $purl
        name = [string] $package.Id
        version = [string] $package.Version
        purl = $purl
        properties = @(
            [ordered]@{
                name = 'arsvin:dependency-scope'
                value = [string] $package.Scope
            },
            [ordered]@{
                name = 'arsvin:used-by'
                value = $usedBy
            }
        )
    }
}

$sbom = [ordered]@{
    bomFormat = 'CycloneDX'
    specVersion = '1.5'
    version = 1
    metadata = [ordered]@{
        timestamp = $sourceTimestamp
        tools = [ordered]@{
            components = @(
                [ordered]@{
                    type = 'application'
                    author = 'ARSVIN project'
                    name = 'generate-sbom.ps1'
                    version = '1.1.0'
                }
            )
        }
        component = [ordered]@{
            type = 'application'
            'bom-ref' = "pkg:generic/arsvin@$Version"
            name = 'ARSVIN Windows Suite'
            version = $Version
            licenses = @(
                [ordered]@{
                    license = [ordered]@{
                        id = 'Apache-2.0'
                    }
                }
            )
            properties = @(
                [ordered]@{
                    name = 'arsvin:source-commit'
                    value = $sourceCommit
                },
                [ordered]@{
                    name = 'arsvin:included-applications'
                    value = ($applicationProjects.Name -join ', ')
                }
            )
        }
    }
    components = @($components)
}

$json = $sbom | ConvertTo-Json -Depth 20
[System.IO.File]::WriteAllText(
    $OutputPath,
    $json + [Environment]::NewLine,
    [System.Text.UTF8Encoding]::new($false)
)

$written = Get-Item $OutputPath
Write-Host "==> CycloneDX SBOM written: $($written.FullName)"
Write-Host "    Source commit: $sourceCommit"
Write-Host "    Components: $($components.Count)"
Write-Host "    Size: $($written.Length) bytes"
