# Public Release Checklist

Use this checklist before making a public GitHub release.

## Repository and licensing health

- [ ] README explains purpose, scope, installation, build, operational boundaries, evidence limits, and licensing.
- [ ] `LICENSE` contains the GNU General Public License version 3 text.
- [ ] `Directory.Build.props` declares `GPL-3.0-or-later`.
- [ ] Current source and release wording do not present Apache-2.0 as an active alternative license.
- [ ] The historical boundary commit and `archive/apache-2.0-final` branch are documented.
- [ ] `COMMERCIAL-LICENSE.md` states that it is an invitation to negotiate and grants no additional rights by itself.
- [ ] NOTICE, copyright, trademark, and third-party notices are current.
- [ ] CLA and DCO contribution gates are present.
- [ ] CONTRIBUTING, SECURITY, SUPPORT, and CODE_OF_CONDUCT are present.
- [ ] Current-license, provenance, terminology, and public-wording validators pass.
- [ ] Repository description, topics, and Pages URL are current and searchable.

## Engineering quality

- [ ] Locked `dotnet restore` succeeds.
- [ ] Warning-free Release builds succeed for Publisher and Subscriber.
- [ ] Deterministic tests and configured coverage gates succeed.
- [ ] TRX and Cobertura evidence are retained by CI.
- [ ] CodeQL and dependency vulnerability workflows pass.
- [ ] Dependency lock files and SBOM inputs are reviewed.
- [ ] No credentials, customer data, station files, restricted addressing, production captures, confidential SCL, or proprietary third-party material are committed.
- [ ] Every non-synthetic fixture has documented authority, sanitization, and provenance.

## Operational and claim boundary

- [ ] README and installer state that ARSVIN can transmit and capture raw Ethernet frames.
- [ ] Live features are limited to authorized test networks and approved workflows.
- [ ] Known limitations and evidence boundaries are visible.
- [ ] Release notes do not imply formal conformance, calibration, deterministic timing, functional safety, cybersecurity approval, switching authority, or IED consumption proof.
- [ ] Validation sources are identified as automated tests, loopback, simulator, laboratory equipment, or approved commissioning evidence.

## Release package

- [ ] Tag format is `vX.Y.Z` or a valid semantic-version prerelease.
- [ ] Tagged commit is already contained in protected `main`.
- [ ] Publisher and Subscriber portable EXEs exist and are non-empty.
- [ ] Suite installer passes silent install and uninstall smoke testing.
- [ ] Portable ZIP and installer include `LICENSE.txt`, `NOTICE.txt`, `COMMERCIAL-LICENSE.md`, `COPYRIGHT.md`, `TRADEMARK.md`, `THIRD_PARTY_NOTICES.md`, and `docs/LICENSING.md`.
- [ ] No historical Apache license file is included in a current GPL package.
- [ ] CycloneDX SBOM parses and contains resolved runtime dependency components.
- [ ] SHA-256 checksums cover every distributed release asset.
- [ ] GitHub build-provenance and SBOM attestations are created for tagged assets.
- [ ] `gh attestation verify` succeeds for a downloaded release file.
- [ ] Release notes include highlights, fixes, known limitations, licensing, and operational reminders.
- [ ] Downloaded artifacts run on a clean Windows machine; Npcap is installed separately only for authorized live-network testing.