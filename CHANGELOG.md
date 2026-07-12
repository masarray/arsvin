# Changelog

All notable ARSVIN changes are documented here using a lightweight Keep a Changelog structure.

## Unreleased

_No unreleased changes._

## 0.4.0 — 2026-07-12
### Added

- Added a repository-owned, dependency-free public-site builder that converts every `docs/*.md` guide into a dedicated HTML page.
- Added compact documentation navigation, topic filtering, breadcrumbs, source links, responsive styling, and a generated search index.
- Added unique canonical URLs, descriptions, Open Graph metadata, Twitter Card metadata, and `TechArticle` JSON-LD for engineering documentation pages.
- Added a generated multi-page sitemap containing the product homepage and every published documentation page.
- Added recursive validation for all HTML metadata, structured data, canonical uniqueness, local references, search-index targets, sitemap coverage, web-manifest icons, and robots metadata.
- Added committed NuGet lock files for Publisher, Subscriber, the shared engine, and Tests.
- Added CI evidence upload for the committed dependency lock graph.
- Added whole-engine and protocol-core regression floors of 10.5% and 60% respectively.
- Expanded the deterministic test suite from 26 to 54 tests across SCL, COMTRADE, PCAP, MMS, diagnostics, and Sampled Values publisher sessions.

### Changed

- GitHub Pages and Windows CI now build and validate the same staged public-site artifact instead of copying raw Markdown into the deployment directory.
- The product landing page now links directly to Quick Start, SV Profile Support, COMTRADE Replay, Subscriber Verification, Safety Boundaries, and the complete documentation index.
- Validated CI, CodeQL, build, test, publish, and release paths now restore NuGet dependencies in locked mode.
- README, build guidance, contributor guidance, repository structure, release examples, and coverage baselines now match the current shared-engine implementation.
- CI, CodeQL, and release validation now use the explicit `windows-2025` runner image; Pages and release publication use `ubuntu-24.04`.
- GitHub Pages now retriggers when the Python public-site validator changes.
- Moved all shared engine source physically from `src/ARSVIN/Engine` into the owning `src/ARSVIN.Engine` project without duplicating compilation.
- Raised whole-engine line coverage from 5.64% to 10.74% and protocol-core coverage from 57.89% to 64.97%.

### Fixed

- Multi-ASDU Publisher sessions now apply the configured `smpCnt` wrap to every ASDU inside a frame, preventing counters such as `4000` and `4001` when a 4,000-sample wrap is configured.

### Security

- Published GitHub Releases are now immutable in automation; a release job fails before attestation or upload when the semantic-version tag already has a GitHub Release.
- Artifact corrections require a new semantic-version patch instead of replacing previously published binaries, checksums, or SBOMs.
- Workflow execution no longer depends on moving `windows-latest` or `ubuntu-latest` labels.

### Planned

- Continue expanding deterministic regression tests for remaining live capture, MMS service, scheduling, and device-interoperability paths.
- Add Windows Authenticode signing when a trusted certificate becomes available.

## 0.3.1 — 2026-07-12

### Added

- Added pinned Coverlet MSBuild instrumentation with TRX, Cobertura, and complete test-log evidence upload.
- Added a verified 50% line-coverage regression floor for the established IEC 61850 protocol-core test surface.
- Added transparent whole-engine coverage reporting: 15,726 production lines at the current 5.64% full-engine baseline.
- Established a protocol-core baseline of 57.89% line coverage across 1,534 instrumented lines, with 888 lines covered and all 26 tests passing.
- Added a repository-owned CycloneDX 1.5 SBOM generator for direct and transitive Publisher/Subscriber NuGet dependencies while excluding test-only packages.
- Added `ARSVIN-SBOM.cdx.json` to validated workflow artifacts, release downloads, and SHA-256 checksums.
- Added signed GitHub build-provenance and SBOM attestations for tagged release artifacts.
- Added the shared `ARSVIN.Engine` class library as the single compiled IEC 61850 protocol implementation used by Publisher, Subscriber, and Tests.

### Changed

- Public releases now require semantic-version tags whose commits are already contained in `main`.
- Alpha, beta, and release-candidate tags are published as prereleases without replacing the latest stable release.
- Centralized Publisher and Subscriber source version metadata.
- GitHub Pages now validates the public site immediately before deployment.
- Publisher and Subscriber now reference one engine assembly instead of compiling duplicate copies of the protocol source.
- Protocol tests now exercise the same `ARSVIN.Engine` assembly shipped with both applications.
- Coverage instrumentation now measures the complete shared engine while preserving the tested protocol-core regression gate.

### Security

- Pinned all GitHub Actions used by CI, CodeQL, Pages, and release workflows to immutable commit SHAs while retaining version comments for maintainability and Dependabot updates.
- Pinned automated installer compilation to the exact Inno Setup 6.7.1 Chocolatey package and retained resolved compiler metadata in workflow evidence.
- Treat compiler warnings as errors in validated Publisher, Subscriber, test, release, and CodeQL build paths.
- Stabilized SBOM component ordering, source commit, and metadata timestamp for repeatable review from the same commit.

## 0.3.0 — 2026-07-11

### Added

- Public Apache-2.0 ARSVIN suite positioning with separate Publisher and ArSubsv Subscriber applications.
- Self-contained Windows x64 portable executables for Publisher and Subscriber.
- Inno Setup suite installer with Start Menu shortcuts, documentation, samples, and uninstaller.
- Portable suite ZIP and SHA-256 checksum release assets.
- SCL/SCD-assisted SV stream configuration and validation.
- Multi-ASDU Sampled Values publishing with `nofASDU=1/2/4/8`.
- Manual values, ramps, state sequences, per-phase scenarios, waveform shaping, and COMTRADE replay.
- Publisher timing-health metrics, generated PCAP export, and Markdown evidence reports.
- ArSubsv live Npcap capture, classic-PCAP import, stream discovery, payload decoding, waveform, phasor, RMS, and receiver reports.
- Real application screenshots in the public README and GitHub Pages product site.
- SEO metadata, Open Graph, Twitter Card, `SoftwareApplication` JSON-LD, FAQ structured data, sitemap, robots, and web manifest.
- GitHub Actions CI, full-solution CodeQL, Pages deployment, release packaging, installer smoke test, and dependency vulnerability reports.

### Changed

- Reworked ArSubsv into a compact engineering workspace with a persistent stream explorer, dominant waveform view, phasor rail, values, frame details, diagnostics, and cursor comparison.
- Normalized Subscriber phasor angles to `Va = 0°` for clearer Publisher/Subscriber comparison.
- Centralized public documentation around transparent laboratory evidence and explicit IEC 61850 safety boundaries.
- Made silent installer and uninstaller operation suitable for automated release validation.

### Fixed

- Publisher shutdown dispatch no longer raises late UI exceptions while the application is closing.
- Subscriber payload decoding, linked engine compilation, WPF control resolution, and frame parser build issues.
- Publisher slot selection now updates the currently selected live slot instead of a stale startup selection.
- Release packaging now fails immediately when external `dotnet` or installer commands fail.

### Safety

- Clarified that ARSVIN is not a calibrated protection test set, certified merging unit, deterministic real-time platform, production process-bus monitor, or IEC 61850 conformance tool.
- Clarified that Publisher evidence proves generated traffic and Subscriber evidence proves PC/NIC reception, not IED consumption.
- Kept Npcap external to the installer and limited live use to authorized, isolated laboratory networks.

## 0.2.0 — 2026-07-02

### Added

- Initial ArSubsv IEC 61850 Sampled Values Subscriber companion.
- Npcap capture, SV APDU decode, SCL binding, stream-health analysis, sample-counter continuity checks, decoded values, waveform, phasor, RMS, PCAP replay, and Markdown verification reports.

### Changed

- Kept Publisher and Subscriber as separate applications so each workflow remains focused and its evidence boundary remains clear.

## 0.1.0 — 2026-06-01

### Added

- Initial Windows WPF IEC 61850 Sampled Values Publisher.
- SCL-based stream setup.
- Manual, ramp, and state-sequenced publishing workflows.
- COMTRADE replay.
- Laboratory PTP and `smpSynch` compatibility controls.
- Initial CI, CodeQL, Pages, and release packaging.
