# Quick Start

## Clone

```powershell
git clone --recurse-submodules https://github.com/masarray/arsvin.git
cd arsvin
```

## Build

```powershell
dotnet build .\src\ARSVIN.App\ARSVIN.App.csproj -c Release
```

## Run

```powershell
dotnet run --project .\src\ARSVIN.App\ARSVIN.App.csproj -c Debug
```

## Configure SV

1. Click **Config**.
2. Open the SCL file.
3. Select the SV stream.
4. Select the NIC adapter.
5. Verify APPID, destination MAC, VLAN, sample rate, dataset, and mapped entries.
