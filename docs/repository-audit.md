# Repository Audit

Audit date: 2026-06-30

## Summary

ARSVIN already has a strong public-repository foundation: focused README, engineering docs, Apache-2.0 intent, GitHub Actions CI, CodeQL, Dependabot, release packaging, and a GitHub Pages landing page.

The main gaps before calling it a polished global open-source repository were:

- License text needed the full Apache-2.0 appendix / application notice.
- No issue templates or pull request template.
- No unit test project.
- Architecture documentation was not explicit enough for external contributors.
- Third-party dependency notices were not listed separately.
- Release readiness checklist was missing.

## Current strengths

- Clear niche: IEC 61850 Sampled Values lab publishing.
- Honest safety positioning: not a certified protection test set.
- Existing docs for quick start, live safety, PTP/smpSynch, COMTRADE, and limitations.
- CI, CodeQL, release workflow, Pages workflow, and Dependabot already present.
- No obvious committed secrets found during static text scan.

## High-priority recommendations

1. Split pure IEC 61850 protocol code into a class library once the API stabilizes.
2. Add more tests around SCL parsing, SV frame encoding, COMTRADE reading, VLAN behavior, and PTP compatibility.
3. Add golden packet fixtures for SV and PTP behavior.
4. Keep safety language prominent in README, docs, and release notes.
5. Avoid marketing claims such as “certified,” “calibrated,” or “real-time accurate” unless independently validated.

## Public readiness score

- Before cleanup: 7.8 / 10
- After repository-polish cleanup: 8.8 / 10
- To reach 9.5+ global standard: separate engine library, test coverage, signed releases, SBOM/provenance, and packet-level golden fixtures.
