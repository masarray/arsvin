## Engineering problem

Describe the change, why it is needed, and whether it belongs in the shared engine, Publisher, Subscriber, documentation, website, or release tooling.

## Solution

Summarize the implementation and any application/engine boundary changes.

## Type of change

- [ ] Bug fix
- [ ] Feature
- [ ] Protocol engine / packet behavior
- [ ] Documentation / website
- [ ] Refactor
- [ ] Build / CI
- [ ] Release preparation
- [ ] Operational wording / guardrail
- [ ] Licensing / provenance

## Engineering and evidence notes

Explain any IEC 61850, Sampled Values, SCL/SCD, COMTRADE, timing, packet-layout, capture, or UI behavior. Identify the requirement source and evidence environment. Write **Not applicable** when unrelated to runtime behavior.

## Operational impact

- [ ] No runtime or live-network behavior changes
- [ ] Offline parsing, display, reporting, or PCAP behavior only
- [ ] Live capture was tested only on an authorized isolated link
- [ ] Live transmission was tested only on an authorized isolated link
- [ ] Operational documentation and warnings were reviewed

## Validation

- [ ] Current-license, provenance, and public-wording checks passed
- [ ] `dotnet build ARSVIN.sln -c Release`
- [ ] Relevant automated tests and coverage gates passed
- [ ] Public-site build and validator passed, when applicable
- [ ] Release packaging and installer smoke test passed, when applicable
- [ ] Simulator, loopback, or dry-run evidence recorded, when applicable
- [ ] Laboratory equipment evidence recorded, when applicable
- [ ] Public documentation updated for changed behavior or claims

## Data and external-material boundary

- [ ] No confidential customer, employer, station, credential, restricted network, or production data is included
- [ ] Any SCL, COMTRADE, PCAP, screenshot, or diagnostic sample is synthetic, contributor-owned, or documented as authorized and sanitized
- [ ] No external proprietary code, API composition, example, test, wording, screenshot, manual, report, asset, or UI design was copied or mechanically translated
- [ ] The change does not claim formal conformance, universal interoperability, calibrated measurement, deterministic timing, functional safety, cybersecurity approval, switching authority, or IED-consumption proof

## Contribution licensing

- [ ] I have read and affirmatively agree to `CONTRIBUTOR-LICENSE-AGREEMENT.md`
- [ ] I have the legal right and any required employer or organizational authorization to submit this contribution
- [ ] Every commit includes a DCO sign-off: `Signed-off-by: Name <email>`
- [ ] Any third-party material is identified with its license and provenance

## Evidence

Add the smallest relevant test output, screenshots, or sanitized protocol evidence. Explain what the evidence demonstrates and what it does not demonstrate.