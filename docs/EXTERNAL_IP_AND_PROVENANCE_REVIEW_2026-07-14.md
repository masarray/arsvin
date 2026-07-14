# External IP and Provenance Review — 2026-07-14

This is a repository-evidence and process review. It is not a legal opinion and cannot establish every off-repository fact or contractual obligation.

## Scope

The review covers the current ARSVIN repository, Publisher and Subscriber applications, shared engine, tests, samples, release automation, documentation, website, screenshots, and public claims.

## Repository evidence

At the review date, the tracked project presents ARSVIN as an independently developed IEC 61850 Sampled Values engineering suite. The current source uses project-owned architecture and application assets together with separately identified dependencies listed in `THIRD_PARTY_NOTICES.md`.

Repository inspection can identify tracked source, dependencies, notices, assets, wording, and contribution records. It cannot prove the absence of undisclosed private material, establish employer ownership, or resolve invention-assignment, confidentiality, customer, equipment-use, or working-time obligations.

Git account and commit attribution identify repository activity; they are not by themselves conclusive proof of legal ownership.

## External implementation boundary

External software may be used only as a lawfully licensed black-box interoperability endpoint within the applicable license, organizational policy, and authorization boundary.

The following must not be used as ARSVIN implementation design material unless the project has documented redistribution and relicensing rights:

- external source code or generated bindings;
- API composition, internal structure, tests, examples, or naming schemes;
- copied documentation wording, report templates, screenshots, UI composition, icons, or artwork;
- extracted binaries, resources, databases, manuals, or help content; and
- confidential customer, employer, laboratory, or station material.

Observed behavior must be reduced to neutral protocol facts and independently implemented and tested.

## Fixtures and evidence

Public fixtures should be synthetic or contributor-owned. A real SCL, COMTRADE, PCAP, screenshot, or diagnostic sample requires documented authority to share, sanitization, and a clear provenance record.

Do not publish credentials, customer or employer identifiers, station names, restricted addressing, production network plans, private support material, or captures whose redistribution rights are uncertain.

## Licensing transition

The historical Apache-2.0 boundary is commit `9440f08b6909ef2dc93dd483cfdcb4e1e86077d0`, retained on `archive/apache-2.0-final`. Later community revisions are offered under `GPL-3.0-or-later`.

A separate commercial path may cover only rights controlled by the relevant copyright holder. Third-party and historical material remains subject to its applicable license.

## Controls

- current-license and public-wording verification in CI;
- dependency locking and third-party notices;
- CLA plus DCO for future contributions;
- sanitized issue and pull-request evidence requirements;
- explicit operational and conformance claim boundaries;
- immutable tagged releases and reproducible release evidence; and
- manual review before high-value OEM, white-label, or proprietary distribution.

## Remaining manual checks

Before a significant commercial agreement, review:

- the contributor and copyright chain for the exact revision being licensed;
- employment, invention-assignment, confidentiality, and customer obligations;
- every private SCL, COMTRADE, PCAP, screenshot, design note, and test artifact used during development;
- third-party package and tool terms for the planned distribution model;
- trademark clearance and visual-similarity risk; and
- the exact source, binary, installer, SBOM, and notices delivered to the customer.

## Conclusion

The repository controls support a defensible independent-development and dual-channel licensing process. They reduce, but do not eliminate, copyright, license, trademark, confidentiality, contractual, or ownership risk.