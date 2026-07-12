# Changelog

All notable ARSVIN changes are documented here using a lightweight Keep a Changelog structure.

## Unreleased

### Added

- Added a pinned Coverlet collector, TRX/Cobertura evidence upload, and a 20% line-coverage regression floor for the currently testable engine surface.
- Added a repository-owned CycloneDX 1.5 SBOM generator for direct and transitive NuGet dependencies.
- Added `ARSVIN-SBOM.cdx.json` to validated workflow artifacts, release downloads, and SHA-256 checksums.
- Added signed GitHub build-provenance and SBOM attestations for tagged release artifacts.

### Security

- Pinned all GitHub Actions used by CI, CodeQL, Pages, and release workflows to immutable commit SHAs while retaining version comments for maintainability and Dependabot updates.
- Pinned automated installer compilation to Inno Setup 6.7.1 and verify both the Chocolatey package and `ISCC.exe` product version.
- Treat compiler warnings as errors in validated Publisher, Subscriber, test, release, and CodeQL build paths.

### Planned

- Shared `ARSVIN.Engine` class library extraction.
- Higher coverage thresholds and expanded protocol regression tests.
- Search-indexable HTML engineering documentation.
- Windows Authenticode signing when a trusted certificate becomes available.

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
