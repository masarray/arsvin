## Summary

Describe the change in user-facing terms.

## Validation

- [ ] `dotnet build src/ARSVIN.App/ARSVIN.App.csproj -c Release`
- [ ] Manual workspace opens
- [ ] Stream Config opens
- [ ] No SV packet is sent unless Start is pressed
- [ ] Relevant docs updated

## Safety note

For live SV injection, confirm the test network is isolated from production protection systems.
