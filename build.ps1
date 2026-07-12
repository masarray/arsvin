$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$root = $PSScriptRoot
$appProject = Join-Path $root 'src\ARSVIN\ARSVIN.csproj'
$subscriberProject = Join-Path $root 'src\ARSVIN.Subscriber\ARSVIN.Subscriber.csproj'
$testProject = Join-Path $root 'tests\ARSVIN.Tests\ARSVIN.Tests.csproj'

function Invoke-DotNet {
    param(
        [Parameter(Mandatory)]
        [string[]] $Arguments
    )

    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet $($Arguments -join ' ') failed with exit code $LASTEXITCODE."
    }
}

Write-Host '==> Restoring solution projects'
Invoke-DotNet -Arguments @('restore', $appProject)
Invoke-DotNet -Arguments @('restore', $subscriberProject)
Invoke-DotNet -Arguments @('restore', $testProject)

Write-Host '==> Building ARSVIN Publisher'
Invoke-DotNet -Arguments @('build', $appProject, '-c', 'Release', '--no-restore', '-warnaserror')

Write-Host '==> Building ArSubsv Subscriber'
Invoke-DotNet -Arguments @('build', $subscriberProject, '-c', 'Release', '--no-restore', '-warnaserror')

Write-Host '==> Running tests'
Invoke-DotNet -Arguments @('test', $testProject, '-c', 'Release', '--no-restore', '/p:TreatWarningsAsErrors=true')

Write-Host '==> Build completed successfully'
