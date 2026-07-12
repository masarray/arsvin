# Sampled Values Standards and Evidence Research Gate

This document defines the evidence required before ARSVIN labels an IEC 61850 Sampled Values profile as supported. It intentionally separates verified standards requirements, vendor documentation, observed traffic, engineering inference, and product ideas.

ARSVIN is an open engineering tool. It is not an accredited conformance test system, calibrated merging unit, deterministic real-time platform, or substitute for licensed standards.

## Why this gate exists

Sampled Values interoperability cannot be established from a sample rate or payload length alone. A credible profile implementation must align:

- the abstract Sampled Values service,
- the Ethernet mapping,
- the SCL model and dataset,
- profile-specific instrument-transformer requirements,
- synchronization and timing behavior,
- the actual wire traffic,
- publisher and subscriber behavior,
- and evidence from real devices.

No profile-specific constant should be added to the production catalog merely because it is common in articles, vendor screenshots, or engineering memory.

## Source hierarchy

Use sources in this order:

1. Licensed IEC or IEC/IEEE standard text, including applicable amendments and corrigenda.
2. Official IEC tissue resolution or official implementation clarification.
3. UCAIug implementation guideline or conformance material.
4. Official manufacturer documentation.
5. Anonymized SCL and PCAP evidence from real merging units and subscribers.
6. Byte-exact interoperability tests with real devices.
7. Independent reference implementations and Wireshark as supporting evidence only.
8. Engineering inference, clearly marked provisional.

A lower-level source must not override a higher-level source.

## Current public catalogue baseline

The current public IEC 61850 series catalogue identifies the following relevant publications:

| Publication | Role in ARSVIN research |
|---|---|
| IEC 61850-6:2009 + AMD1:2018 + AMD2:2024 CSV | SCL structures, SampledValueControl, datasets, communication addressing, and engineering-file behavior. |
| IEC 61850-7-2:2010 + AMD1:2020 CSV | Abstract Sampled Values service model and semantics. |
| IEC 61850-9-2:2011 + AMD1:2020 CSV | Ethernet mapping for Sampled Values over ISO/IEC 8802-3. |
| IEC/IEEE 61850-9-3:2016 | Power utility PTP profile relevant to synchronization claims. |
| IEC 61850-10:2012 + AMD1:2025 CSV | Conformance-test terminology and claim boundaries. |
| IEC TR 61850-90-5:2012 | Routable communication research boundary; not part of the current Layer-2 milestone. |

IEC 61869-9 must be reviewed from a licensed current copy before ARSVIN freezes preferred Sampled Values variants, scaling rules, rates, dataset rules, or profile names.

Public catalogue metadata is sufficient to establish which documents must be reviewed. It is not sufficient to implement clause-level behavior.

## OMICRON benchmark findings

### StationScout benchmark

StationScout is a benchmark for system-level engineering workflow rather than the primary reference for SV byte-level implementation. The official material emphasizes:

- automatic visualization of SCL-based system structure,
- a clear status overview,
- signal tracing,
- comparison of configured and live system behavior,
- simulation of unavailable IEDs,
- reusable test cases,
- automated assessments,
- and documented results.

ARSVIN should adopt the workflow principles, not copy the product UI or make unsupported feature claims.

### DANEO 400 benchmark

DANEO 400 is the stronger functional benchmark for ARSVIN Subscriber and merging-unit analysis. Official material describes:

- SCL-based verification that expected GOOSE and Sampled Values are present,
- side-by-side visibility of configuration versus observed traffic,
- orphan stream detection,
- PRP and HSR redundancy comparison,
- merging-unit stream-parameter verification,
- comparison of conventional input waveforms against SV output,
- packet interval and packet delay statistics,
- detection of dropped samples, synchronization issues, quality problems, and traffic abnormalities,
- trigger-based recording,
- time-aligned analysis,
- and report generation.

These capabilities define useful product outcomes, but they do not define protocol constants.

## Product claim levels

ARSVIN uses these statuses:

| Status | Meaning |
|---|---|
| Research candidate | Product idea or commonly discussed profile; no frozen requirements. |
| Standard-reviewed | Applicable clauses and amendments have been reviewed and recorded. |
| Fixture-validated | Byte-exact SCL, frame, and PCAP regression fixtures pass. |
| Device-observed | An anonymized real-device capture matches the implementation. |
| Lab-interoperable | Publisher and subscriber have been exercised with at least one real counterpart. |
| Supported | Standard review, fixtures, real-device evidence, diagnostics, and documentation are complete. |
| Certified | Reserved for an accredited external process; ARSVIN does not currently use this status. |

A profile cannot move directly from research candidate to supported.

## Research targets

### Track A — Installed-base 9-2LE-style workflows

Research goals:

- identify the exact implementation guideline revision used by target devices,
- verify dataset ordering and payload representation,
- verify protection and high-rate variants,
- verify nominal-frequency-dependent behavior,
- verify quality and synchronization fields,
- collect representative SCL and PCAP fixtures,
- and document vendor variations.

Until the guideline and real evidence are reviewed, ARSVIN must retain the wording `9-2LE-style` rather than claim universal 9-2LE conformance.

### Track B — IEC 61869-9 preferred variants

Research goals:

- review the licensed standard and amendments,
- record every preferred variant relevant to protection and measurement,
- verify configurable dataset expectations,
- verify sampling basis and rate semantics,
- verify nofASDU expectations,
- verify scaling and unit rules,
- verify counter behavior,
- verify synchronization requirements,
- and validate against current merging-unit captures.

No rate, nofASDU value, scaling constant, or dataset shape is considered normative until entered in the evidence matrix with a clause reference.

### Track C — Generic SCL-driven Layer-2 SV

The engine already supports a broader set of SCL-derived payload types than the current fixed 4I+4V workflow. Research must determine:

- which bTypes are valid and useful in SV datasets,
- how nested data attributes are flattened and ordered,
- whether each width and representation is correct,
- how unknown or vendor-specific fields are preserved,
- and when compatible decoding is safe without claiming a known profile.

### Track D — Routable SV

Routable SV is a separate architectural program. It requires separate review of transport, session, security, routing, synchronization, and test requirements. It must not be mixed into the current Layer-2 profile expansion.

## Required evidence package per profile

Each supported profile needs:

```text
profile-requirements.md
source-register.md
golden.scd or golden.icd
golden.pcap
expected-frame.json
expected-detection.json
expected-values.json
publisher-byte-tests
subscriber-decode-tests
mismatch-tests
performance-baseline.md
real-device-evidence.md
```

Real-device evidence may remain private when licensing or confidentiality requires it, but the public repository must contain a non-sensitive synthetic fixture that protects the same behavior.

## Research acceptance rules

A requirement may enter production only when:

1. Its source and edition are identified.
2. The relevant clause or official vendor section is recorded without copying protected standard text.
3. Its normative strength is classified as required, recommended, optional, or engineering convention.
4. A deterministic automated test protects the behavior where technically possible.
5. Conflicting evidence is documented instead of silently resolved.
6. The UI distinguishes verified profile facts from inferred observations.
7. Unknown traffic remains observable and does not block capture.
8. Live transmission is blocked when a verified critical requirement fails.

## Immediate implementation order after the gate

1. Add source and evidence models; do not add unverified constants.
2. Add observed-stream facts and confidence-based detection infrastructure.
3. Formalize the current 9-2LE-style workflow using reviewed evidence.
4. Add configuration-versus-wire mismatch reporting.
5. Add scaling provenance and raw-value visibility.
6. Add IEC 61869-9 variants only after licensed clause review.
7. Add high-rate performance work after functional fixtures pass.
8. Add real-device interoperability evidence before public support claims.

## Official public references

- IEC 61850 series catalogue: https://webstore.iec.ch/en/publication/6028
- OMICRON StationScout: https://www.omicronenergy.com/en/products/stationscout/
- OMICRON StationScout brochure: https://www.omicronenergy.com/download/document/A73FDBE4-EFBB-4673-A765-E58195B69D2B/
- OMICRON DANEO 400: https://www.omicronenergy.com/en/products/daneo-400/
- OMICRON DANEO 400 brochure: https://www.omicronenergy.com/download/document/8C945CAA-C82D-4EFE-A1D8-BE6A52691FE6/
- OMICRON IEC 61850 thematic introduction: https://www.omicronenergy.com/download/document/918A983B-DD8B-43AE-938A-168E40ECB8C1/

These links support public product and catalogue research. They do not replace licensed standards.