# Public Terminology Policy

ARSVIN public documentation describes engineering outcomes, protocol behavior, standards references, evidence requirements, licensing, and operational boundaries in neutral and verifiable terms.

## Public content rules

- Do not use unrelated product comparisons, branding, UI, reports, or marketing language as feature requirements.
- Do not imply compatibility, endorsement, certification, approval, or equivalence with another product or organization.
- Describe desired outcomes directly: SCL-versus-wire comparison, continuity diagnostics, stream-health analysis, transmitter evidence, receiver evidence, and repeatable reporting.
- Cite applicable standards publications and public implementation guidance without reproducing protected standard text.
- Keep manufacturer-specific observations inside authorized evidence records and publish only synthetic, contributor-owned, or documented authorized fixtures.
- Record requirement and fixture provenance separately from product-facing language.
- Distinguish configured expectations, observed traffic, software interpretation, and external-device behavior.
- Identify evidence sources such as deterministic tests, simulator, loopback, isolated laboratory equipment, or approved commissioning work.

## Claims requiring qualification

Do not use unqualified claims such as certified, conformant, compliant, calibrated, deterministic, real-time accurate, safe, secure, trusted, universal, production-ready, field-proven, or guaranteed.

Use scoped evidence wording such as implemented, covered by the stated test, observed on the selected adapter, laboratory-exercised, provisional, partial, unsupported, unknown, or not yet verified.

ARSVIN does not establish functional safety, cybersecurity approval, switching authority, equipment isolation, calibrated measurement, deterministic execution, or proof that another IED consumed multicast traffic.

## Licensing terminology

- Current community source and current releases are `GPL-3.0-or-later` only.
- Historical Apache-2.0 references must identify the exact historical boundary and must not imply a current Apache-or-GPL choice.
- The commercial licensing notice is an invitation to negotiate and does not itself grant additional rights.
- Commercial terms can cover only rights controlled by the relevant copyright holder.
- Third-party components retain their own licenses and notices.

## Automated validation

CI, Pages, build, and release paths run:

```text
scripts/verify-current-license.py
scripts/validate-public-neutrality.py
```

These controls protect the current tracked tree and generated public content. Historical commits remain repository history and are not current product documentation.