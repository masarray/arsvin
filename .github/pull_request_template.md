## Summary

Describe the change, why it is needed, and the user or engineering impact.

## Type of change

- [ ] Bug fix
- [ ] Feature
- [ ] Protocol engine / packet behavior
- [ ] Documentation / website
- [ ] Refactor
- [ ] Build / CI
- [ ] Release preparation
- [ ] Safety wording / guardrail

## Engineering notes

Explain any IEC 61850, Sampled Values, SCL/SCD, COMTRADE, PTP, timing, packet-layout, or UI behavior affected by this PR. Write **Not applicable** when the change is unrelated to runtime engineering behavior.

## Safety impact

- [ ] No runtime or live-network behavior changes
- [ ] Changes offline parsing, display, reporting, or PCAP behavior only
- [ ] Changes live capture behavior and was tested on an authorized isolated link
- [ ] Changes live packet publishing and was tested on an authorized isolated link
- [ ] Safety documentation or warnings were reviewed and updated where needed

## Validation

- [ ] `dotnet build ARSVIN.sln -c Release`
- [ ] `dotnet test tests/ARSVIN.Tests/ARSVIN.Tests.csproj -c Release`
- [ ] Public-site validator passed, when applicable
- [ ] Release packaging and installer smoke test passed, when applicable
- [ ] Manual dry run tested, when applicable
- [ ] Live mode tested only on an isolated lab link, when applicable

## Evidence

Add relevant screenshots, sanitized Wireshark notes, test output, release artifact names, or a concise explanation when visual/packet evidence is not applicable.
