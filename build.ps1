$ErrorActionPreference = 'Stop'

$project = Join-Path $PSScriptRoot 'src\ARSVIN\ARSVIN.csproj'

dotnet restore $project
dotnet build $project -c Release --no-restore
