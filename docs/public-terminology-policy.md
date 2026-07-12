# Public Terminology Policy

ARSVIN public documentation describes engineering outcomes, protocol behavior, standards references, and evidence requirements in neutral terms.

## Public content rules

- Do not use proprietary product comparisons as feature requirements.
- Do not imply compatibility, endorsement, certification, or equivalence with third-party products.
- Describe desired outcomes such as SCL-versus-wire comparison, orphan detection, stream-health analysis, triggered evidence capture, and repeatable reporting.
- Cite applicable standards publications and implementation guidelines without reproducing protected standard text.
- Keep manufacturer-specific observations inside authorized evidence records and publish only anonymized or synthetic regression fixtures.
- Record source provenance separately from product-facing language.

## Automated validation

CI and Pages deployment run `scripts/validate-public-neutrality.py` before building public documentation. The validator checks active public content and blocks prohibited comparison terminology.

This validation protects the current repository tree. Historical Git commits remain part of normal repository history and should not be treated as current product documentation.
