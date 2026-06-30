## Summary

Describe the change and why it is needed.

## Type of change

- [ ] Bug fix
- [ ] Feature
- [ ] Documentation
- [ ] Refactor
- [ ] Build / CI / release
- [ ] Safety wording / guardrail

## Engineering notes

Explain the IEC 61850 / SV / SCL / COMTRADE / PTP behavior affected by this PR.

## Safety impact

- [ ] Does not affect live packet publishing
- [ ] Affects live packet publishing and has been reviewed carefully
- [ ] Documentation / warning updated if needed

## Test notes

- [ ] `dotnet build src/ARSVIN/ARSVIN.csproj -c Release`
- [ ] `dotnet test tests/ARSVIN.Tests/ARSVIN.Tests.csproj -c Release`
- [ ] Manual dry run tested
- [ ] Live mode tested only on isolated lab link, if applicable

## Screenshots / packet notes

Add screenshots or sanitized Wireshark notes when relevant.
