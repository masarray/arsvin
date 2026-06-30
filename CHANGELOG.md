# Changelog

## Unreleased

### Added
- P0 SV publisher protocol upgrade: `nofASDU=1/2/4/8` support.
- Multi-ASDU SavPdu generation with sequential per-ASDU `smpCnt`.
- Second-aligned sample counter initialization for lab publishing.
- Publisher SCL validation and frame preview helpers.
- Sampled Value quality-bit helper and generated PCAP export helper.
- Golden-style unit tests for multi-ASDU frame round-trip, quality bits, sample counter, and PCAP output.
- Sample SCL file for `nofASDU=8`.


All notable changes to ARSVIN should be documented in this file.

This project follows a simple public-release style:

- `Added` for new features
- `Changed` for changes in existing functionality
- `Fixed` for bug fixes
- `Security` for vulnerability-related changes

## 0.1.0

### Added

- Initial public Apache-2.0 repository structure
- WPF desktop app for IEC 61850 Sampled Values publishing
- SCL-based SV stream setup
- Manual, ramp, and state-sequenced publishing workflows
- COMTRADE replay support
- Lab PTP and `smpSynch` compatibility controls
- GitHub Actions CI, CodeQL, Pages, and release packaging
