[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidatePattern('^\d+\.\d+\.\d+(?:[-+][0-9A-Za-z.-]+)?$')]
    [string] $Version,

    [string] $OutputPath
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function New-DeterministicUuidUrn {
    param(
        [Parameter(Mandatory)]
        [string] $Value
    )

    $sha256 = [System.Security.Cryptography.SHA256]::Create()
    try {
        $inputBytes = [System.Text.Encoding]::UTF8.GetBytes($Value)
        $hash = $sha256.ComputeHash($inputBytes)
    }
    finally {
        $sha256.Dispose()
    }

    $uuidBytes = [byte[]]::new(16)
    [Array]::Copy($hash, $uuidBytes, $uuidBytes.Length)
    $uuidBytes[6] = [byte](($uuidBytes[6] -band 0x0F) -bor 0x50)
    $uuidBytes[8] = [byte](($uuidBytes[8] -band 0x3F) -bor 0x80)

    $hex = -join ($uuidBytes | ForEach-Object { $_.ToString('x2') })
    $uuid = '{0}-{1}-{2}-{3}-{4}' -f (
        $hex.Substring(0, 8),
        $hex.Substring(8, 4),
        $hex.Substring(12, 4),
        $hex.Substring(16, 4),
        $hex.Substring(20, 12)
    )

    return "urn:uuid:$uuid"
}

function Invoke-GitText {
    param(
        [Parameter(Mandatory)][string] $Repository,
        [Parameter(Mandatory)][string[]] $Arguments
    )

    $output = & git -C $Repository @Arguments 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "git -C $Repository $($Arguments -join ' ') failed.`n$($output -join [Environment]::NewLine)"
    }
    return ($output -join '').Trim()
}

$root = Split-Path -Parent $PSScriptRoot
$engineRoot = if ($env:ARIEC61850_ROOT) {
    [System.IO.Path]::GetFullPath($env:ARIEC61850_ROOT)
}
else {
    [System.IO.Path]::GetFullPath((Join-Path $root '..\ARIEC61850'))
}
$engineProject = Join-Path $engineRoot 'src\AR.Iec61850\AR.Iec61850.csproj'
if (-not (Test-Path $engineProject -PathType Leaf)) {
    throw "ARIEC61850 sibling project was not found at $engineProject."
}

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

$sourceCommit = Invoke-GitText -Repository $root -Arguments @('rev-parse', 'HEAD')
$engineCommit = Invoke-GitText -Repository $engineRoot -Arguments @('rev-parse', 'HEAD')
$engineRef = if ($env:ARIEC61850_REF) { $env:ARIEC61850_REF } else { 'local-checkout' }
$sourceTimestamp = [DateTimeOffset]::Parse(
    (Invoke-GitText -Repository $root -Arguments @('show', '-s', '--format=%cI', 'HEAD')),
    [System.Globalization.CultureInfo]::InvariantCulture
).ToUniversalTime().ToString('o')

$serialNumber = New-DeterministicUuidUrn -Value "https://github.com/masarray/arsvin|$Version|$sourceCommit|$engineCommit"
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

$nugetComponents = foreach ($package in $sortedPackages) {
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

$enginePurl = "pkg:generic/ARIEC61850@$engineCommit"
$engineComponent = [ordered]@{
    type = 'library'
    'bom-ref' = $enginePurl
    name = 'ARIEC61850'
    version = $engineCommit
    purl = $enginePurl
    licenses = @(
        [ordered]@{
            license = [ordered]@{ id = 'GPL-3.0-or-later' }
        }
    )
    properties = @(
        [ordered]@{ name = 'arsvin:source-repository'; value = 'https://github.com/masarray/ARIEC61850' },
        [ordered]@{ name = 'arsvin:source-ref'; value = $engineRef },
        [ordered]@{ name = 'arsvin:source-commit'; value = $engineCommit },
        [ordered]@{ name = 'arsvin:dependency-scope'; value = 'direct-source-project' }
    )
}

$allComponents = @($engineComponent) + @($nugetComponents)
$sbom = [ordered]@{
    bomFormat = 'CycloneDX'
    specVersion = '1.5'
    serialNumber = $serialNumber
    version = 1
    metadata = [ordered]@{
        timestamp = $sourceTimestamp
        tools = [ordered]@{
            components = @(
                [ordered]@{
                    type = 'application'
                    author = 'ARSVIN project'
                    name = 'generate-sbom.ps1'
                    version = '2.0.0'
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
                    license = [ordered]@{ id = 'GPL-3.0-or-later' }
                }
            )
            properties = @(
                [ordered]@{ name = 'arsvin:source-commit'; value = $sourceCommit },
                [ordered]@{ name = 'arsvin:engine-ref'; value = $engineRef },
                [ordered]@{ name = 'arsvin:engine-commit'; value = $engineCommit },
                [ordered]@{ name = 'arsvin:included-applications'; value = ($applicationProjects.Name -join ', ') }
            )
        }
    }
    components = $allComponents
}

$json = $sbom | ConvertTo-Json -Depth 20
[System.IO.File]::WriteAllText(
    $OutputPath,
    $json + [Environment]::NewLine,
    [System.Text.UTF8Encoding]::new($false)
)

$written = Get-Item $OutputPath
Write-Host "==> CycloneDX SBOM written: $($written.FullName)"
Write-Host "    ARSVIN source commit: $sourceCommit"
Write-Host "    ARIEC61850 source commit: $engineCommit"
Write-Host "    Serial number: $serialNumber"
Write-Host "    Components: $($allComponents.Count)"
Write-Host "    Size: $($written.Length) bytes"
