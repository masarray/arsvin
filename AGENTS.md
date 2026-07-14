# Repository Guidance

ARSVIN is an IEC 61850 Sampled Values engineering suite with a shared engine, ARSVIN Publisher, and ArSubsv Subscriber.

## Priority order

1. Observable and evidence-backed Sampled Values behavior.
2. Explicit operational guardrails for live capture and transmission.
3. Clear separation between Publisher, Subscriber, and shared engine responsibilities.
4. Repeatable SCL, COMTRADE, PCAP, scenario, test, and release workflows.
5. Accurate public documentation, licensing, provenance, and claim boundaries.
6. Performance and usability improvements that preserve engineering evidence.

## Architecture boundary

Protocol parsing, stream generation, SCL semantics, profile observations, comparison logic, capture, and transport behavior belong in `ARSVIN.Engine`. WPF applications should consume typed engine contracts rather than reimplement protocol rules.

## External-material boundary

External software may be used only as a lawfully licensed black-box interoperability endpoint. Do not use unrelated source, API composition, examples, tests, internal structure, documentation wording, UI composition, screenshots, icons, reports, or extracted resources as implementation design material.

Use synthetic or contributor-owned fixtures. Real SCL, COMTRADE, PCAP, screenshots, and diagnostics require documented authority to share and sanitization.

## Public claim boundary

Do not claim formal IEC 61850 conformance, universal interoperability, calibrated measurement, deterministic real-time execution, functional safety, cybersecurity certification, switching authority, or proof that another IED consumed a multicast stream.

State the evidence source and use scoped terms such as implemented, tested, observed, provisional, unsupported, or not yet verified.

## Licensing boundary

Current community revisions are `GPL-3.0-or-later`. Historical Apache-2.0 revisions end at `9440f08b6909ef2dc93dd483cfdcb4e1e86077d0` and remain on `archive/apache-2.0-final`. Commercial rights require a separate negotiated agreement and can cover only rights controlled by the relevant copyright holder.