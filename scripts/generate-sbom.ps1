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
$solution = Join-Path $root 'ARSVIN.sln'

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path $root 'artifacts\release\ARSVIN-SBOM.cdx.json'
}
elseif (-not [System.IO.Path]::IsPathRooted($OutputPath)) {
    $OutputPath = Join-Path $root $OutputPath
}

$outputDirectory = Split-Path -Parent $OutputPath
New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null

Write-Host '==> Resolving complete NuGet dependency graph'
$commandOutput = & dotnet list $solution package --include-transitive --format json --output-version 1 2>&1
if ($LASTEXITCODE -ne 0) {
    throw "dotnet list package failed with exit code $LASTEXITCODE.`n$($commandOutput -join [Environment]::NewLine)"
}

$jsonText = $commandOutput -join [Environment]::NewLine
$jsonStart = $jsonText.IndexOf('{')
$jsonEnd = $jsonText.LastIndexOf('}')
if ($jsonStart -lt 0 -or $jsonEnd -le $jsonStart) {
    throw 'Could not locate the JSON dependency graph in dotnet list package output.'
}

$dependencyGraph = $jsonText.Substring($jsonStart, $jsonEnd - $jsonStart + 1) | ConvertFrom-Json
$packages = @{}

foreach ($project in @($dependencyGraph.projects)) {
    foreach ($framework in @($project.frameworks)) {
        foreach ($scope in @('topLevelPackages', 'transitivePackages')) {
            $property = $framework.PSObject.Properties[$scope]
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
                $isTopLevel = $scope -eq 'topLevelPackages'
                if (-not $packages.ContainsKey($key) -or $isTopLevel) {
                    $packages[$key] = [ordered]@{
                        Id = $id
                        Version = $resolvedVersion
                        Scope = if ($isTopLevel) { 'direct' } else { 'transitive' }
                    }
                }
            }
        }
    }
}

if ($packages.Count -eq 0) {
    throw 'The dependency graph did not contain any resolved NuGet packages.'
}

$components = foreach ($package in $packages.Values | Sort-Object Id, Version) {
    $escapedId = [Uri]::EscapeDataString([string] $package.Id)
    $escapedVersion = [Uri]::EscapeDataString([string] $package.Version)
    $purl = "pkg:nuget/$escapedId@$escapedVersion"

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
            }
        )
    }
}

$metadataProperties = @()
if (-not [string]::IsNullOrWhiteSpace($env:GITHUB_SHA)) {
    $metadataProperties += [ordered]@{
        name = 'arsvin:source-commit'
        value = $env:GITHUB_SHA
    }
}

$sbom = [ordered]@{
    bomFormat = 'CycloneDX'
    specVersion = '1.5'
    version = 1
    metadata = [ordered]@{
        timestamp = [DateTimeOffset]::UtcNow.ToString('o')
        tools = [ordered]@{
            components = @(
                [ordered]@{
                    type = 'application'
                    author = 'ARSVIN project'
                    name = 'generate-sbom.ps1'
                    version = '1.0.0'
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
            properties = $metadataProperties
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
Write-Host "    Components: $($components.Count)"
Write-Host "    Size: $($written.Length) bytes"
