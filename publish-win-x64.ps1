param(
    [string] $Version = '0.0.0-dev',
    [string] $Runtime = 'win-x64',
    [switch] $SkipTests
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$script = Join-Path $PSScriptRoot 'scripts\publish-release.ps1'
& $script -Version $Version -Runtime $Runtime -SkipTests:$SkipTests

if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}
