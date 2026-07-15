# Changelog

All notable ARSVIN changes are documented here using a lightweight Keep a Changelog structure.

## Unreleased

### Added

- Added the current GPL-3.0-or-later community licensing model and a separate negotiated commercial path.
- Added `COMMERCIAL-LICENSE.md`, `COPYRIGHT.md`, `TRADEMARK.md`, a Contributor License Agreement, and Developer Certificate of Origin.
- Added the historical Apache-2.0 boundary at commit `9440f08b6909ef2dc93dd483cfdcb4e1e86077d0` with preservation branch `archive/apache-2.0-final`.
- Added current-license, packaging, provenance, and public-wording verification to CI, Pages, build, and release paths.
- Added evidence-based external IP/provenance and public-claim reviews.
- Added a formal Sampled Values standards and evidence research gate covering authoritative source hierarchy, claim levels, licensed-standard gaps, and implementation acceptance rules.
- Added a conformance and interoperability matrix separating implemented, provisional, unknown, and out-of-scope behavior for Layer-2 SV, 9-2LE-style workflows, IEC 61869-9, timing, scaling, redundancy, and product claims.
- Added a guarded Sampled Values evidence-intake guide for anonymized or authorized SCL/PCAP cases, device metadata, independent verification, negative tests, and evidence quality levels.
- Added transport-independent SV frame observations and an accumulator for stable-field checks, rate estimation, and sample-counter wrap detection.
- Added evidence-aware profile definitions with standards-neutral sampling, dataset, packing, payload, and source metadata.
- Added an explainable weighted profile detector that reports matches, conflicts, missing evidence, confidence, and score.
- Added strict and compatible configuration-versus-wire comparison findings without blocking capture or decoding.
- Added deterministic tests for observation windows, profile detection, evidence explanations, configuration mismatch handling, sparse-evidence confidence, and profile-definition provenance.
- Added public terminology-neutrality validation to CI and Pages deployment with retained validation evidence.
- Added bounded, thread-safe SV observation windows with fact provenance and explicit sequential, gap, duplicate, out-of-order/reset, and confirmed-wrap analysis.
- Added a shared per-stream observation manager used identically by live Npcap capture and PCAP replay.
- Added stable stream identity, input-source tracking, SCL-derived dataset signatures, immutable observation snapshots, and deterministic live/PCAP pipeline tests.

### Changed

- Current README, website, generated documentation, project metadata, installer, and portable packages now identify GPL-3.0-or-later as the only license for later community revisions.
- Release packages include GPL, commercial-licensing notice, copyright, trademark, third-party, and historical-boundary documentation without presenting Apache as a current license choice.
- Documentation now prevents unverified profile constants from being promoted directly into production support claims.
- Public engineering workflow targets are described generically without proprietary product comparisons or branding.
- The built-in profile catalog contains only the generic SCL-driven Layer-2 SV fallback until profile-specific requirements are verified.
- Sparse evidence can no longer produce a `Confirmed` profile result; confirmation requires sufficient evaluated evidence plus matching dataset and sampling behavior.
- Research-candidate confidence is capped at `Possible`, generic implemented profiles are capped at `Likely`, and raw detector confidence remains available separately.
- Subscriber live and PCAP paths now share one observation pipeline and no longer split a stream solely because `confRev` changes.
- Expanded the deterministic suite from 54 to 88 tests.
- Raised whole-engine coverage from 10.74% to 14.61% and the enforced floor from 10.5% to 14.25%.
- Raised protocol-core coverage from 64.97% to 72.76% and the enforced floor from 60% to 72.5%.

### Fixed

- MAC-address formatting normalization is no longer applied to `svID` or dataset references, preventing punctuation-related false matches.
- Backward sample-counter transitions require zero plus sequential recovery before they are classified as wraps.

## 0.4.0 — 2026-07-12

### Added

- Added a repository-owned, dependency-free public-site builder that converts every `docs/*.md` guide into a dedicated HTML page.
- Added compact documentation navigation, topic filtering, breadcrumbs, source links, responsive styling, and a generated search index.
- Added unique canonical URLs, descriptions, Open Graph metadata, Twitter Card metadata, and `TechArticle` JSON-LD for engineering documentation pages.
- Added a generated multi-page sitemap containing the product homepage and every published documentation page.
- Added recursive validation for HTML metadata, structured data, canonical uniqueness, local references, search-index targets, sitemap coverage, web-manifest icons, and robots metadata.
- Added committed NuGet lock files for Publisher, Subscriber, shared engine, and Tests.
- Added CI evidence upload for the committed dependency lock graph.
- Expanded deterministic tests across SCL, COMTRADE, PCAP, MMS, diagnostics, and Sampled Values publisher sessions.

### Changed

- GitHub Pages and Windows CI build and validate the same staged public-site artifact.
- The product landing page links directly to Quick Start, SV Profile Support, COMTRADE Replay, Subscriber Verification, Safety Boundaries, and the documentation index.
- Validated CI, CodeQL, build, test, publish, and release paths restore NuGet dependencies in locked mode.
- README, build guidance, contributor guidance, repository structure, release examples, and coverage baselines match the shared-engine implementation.
- CI, CodeQL, and release validation use explicit runner images.
- Shared engine source is physically owned by `src/ARSVIN.Engine` without duplicate compilation.

### Fixed

- Multi-ASDU Publisher sessions apply the configured `smpCnt` wrap to every ASDU inside a frame.

### Security

- Published GitHub Releases are immutable in automation; corrections require a new semantic-version tag.
- Workflow execution no longer depends on moving default runner labels.

## 0.3.1 — 2026-07-12

### Added

- Added Coverlet MSBuild instrumentation with TRX, Cobertura, and complete test-log evidence upload.
- Added whole-engine and protocol-core coverage reporting and enforcement.
- Added a repository-owned CycloneDX 1.5 SBOM generator for direct and transitive runtime dependencies.
- Added `ARSVIN-SBOM.cdx.json` to workflow artifacts, releases, and SHA-256 checksums.
- Added GitHub build-provenance and SBOM attestations for tagged release artifacts.
- Added `ARSVIN.Engine` as the single compiled IEC 61850 implementation used by Publisher, Subscriber, and Tests.

### Changed

- Public releases require semantic-version tags whose commits are contained in `main`.
- Prerelease tags do not replace the latest stable release.
- Publisher and Subscriber reference one engine assembly.
- Protocol tests exercise the same engine assembly shipped with both applications.

### Security

- Pinned GitHub Actions to immutable commit SHAs.
- Pinned automated installer compilation to a reviewed Inno Setup package version.
- Treat compiler warnings as errors in validated build paths.
- Stabilized SBOM ordering, source commit, and metadata timestamp for repeatable review.

## 0.3.0 — 2026-07-11

### Added

- Public Apache-2.0 ARSVIN suite release with separate Publisher and ArSubsv Subscriber applications.
- Self-contained Windows x64 portable executables, suite installer, portable ZIP, and SHA-256 checksums.
- SCL/SCD-assisted SV configuration, multi-ASDU publishing, scenarios, waveform shaping, and COMTRADE replay.
- Publisher timing-health metrics, PCAP export, and Markdown evidence reports.
- ArSubsv live capture, PCAP import, stream discovery, payload decoding, waveform, phasor, RMS, and receiver reports.
- Actual application screenshots, SEO metadata, structured data, sitemap, robots, web manifest, CI, CodeQL, Pages, and release automation.

### Changed

- Reworked ArSubsv into a compact engineering workspace.
- Normalized Subscriber phasor angles to `Va = 0°` for Publisher/Subscriber comparison.
- Centralized public documentation around laboratory evidence and explicit operational boundaries.

### Fixed

- Publisher shutdown dispatch, Subscriber payload decoding, linked engine compilation, WPF control resolution, frame parser, slot selection, and release-command handling issues.

### Operational boundary

- Clarified that ARSVIN is not calibrated, deterministic real-time, production-monitoring, or formal conformance equipment.
- Clarified that Publisher and Subscriber evidence does not prove IED consumption.
- Kept Npcap external to the installer and limited live use to authorized isolated test networks.

## 0.2.0 — 2026-07-02

### Added

- Initial ArSubsv Subscriber companion with Npcap capture, SV decode, SCL binding, stream health, values, waveform, phasor, RMS, PCAP replay, and reports.

### Changed

- Kept Publisher and Subscriber separate so each evidence boundary remains explicit.

## 0.1.0 — 2026-06-01

### Added

- Initial Windows WPF Sampled Values Publisher with SCL setup, manual and sequenced publishing, COMTRADE replay, laboratory timing controls, CI, Pages, and release packaging.