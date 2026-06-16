# Release Process

Create a tag:

```powershell
git tag v0.1.0
git push origin v0.1.0
```

The release workflow publishes a Windows x64 portable build.

## Local publish

```powershell
dotnet publish .\src\ARSVIN.App\ARSVIN.App.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -o .\artifacts\publish\win-x64
```
