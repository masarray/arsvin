$ErrorActionPreference = 'Stop'

$root = $PSScriptRoot
$appProject = Join-Path $root 'src\ARSVIN\ARSVIN.csproj'
$testProject = Join-Path $root 'tests\ARSVIN.Tests\ARSVIN.Tests.csproj'

Write-Host '==> Restoring ARSVIN app'
dotnet restore $appProject

Write-Host '==> Restoring ARSVIN tests'
dotnet restore $testProject

Write-Host '==> Building ARSVIN app'
dotnet build $appProject -c Release --no-restore

Write-Host '==> Running tests'
dotnet test $testProject -c Release --no-restore
