# Contributing to ARSVIN

ARSVIN is a product-oriented engineering tool. Contributions should improve safety, correctness, usability, and maintainability.

## Development rules

- Keep wording user-facing.
- Do not claim relay-grade timing accuracy without hardware and test evidence.
- Keep SV, PTP, and process-bus safety warnings explicit.
- Avoid vendor comparison language in public docs.

## Build before opening PR

```powershell
dotnet build .\src\ARSVIN.App\ARSVIN.App.csproj -c Release
```
