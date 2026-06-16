$ErrorActionPreference = 'Stop'

dotnet restore .\src\ARSVIN\ARSVIN.csproj
dotnet build .\src\ARSVIN\ARSVIN.csproj -c Release --no-restore
