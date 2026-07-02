$ErrorActionPreference = 'Stop'

$root = $PSScriptRoot
$appProject = Join-Path $root 'src\ARSVIN\ARSVIN.csproj'
$subscriberProject = Join-Path $root 'src\ARSVIN.Subscriber\ARSVIN.Subscriber.csproj'
$testProject = Join-Path $root 'tests\ARSVIN.Tests\ARSVIN.Tests.csproj'

Write-Host '==> Restoring ARSVIN Publisher'
dotnet restore $appProject

Write-Host '==> Restoring ARSVIN Subscriber'
dotnet restore $subscriberProject

Write-Host '==> Restoring ARSVIN tests'
dotnet restore $testProject

Write-Host '==> Building ARSVIN Publisher'
dotnet build $appProject -c Release --no-restore

Write-Host '==> Building ARSVIN Subscriber'
dotnet build $subscriberProject -c Release --no-restore

Write-Host '==> Running tests'
dotnet test $testProject -c Release --no-restore
