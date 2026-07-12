# Public Release Checklist

Use this checklist before making a public GitHub release.

## Repository health

- [ ] README explains purpose, scope, install, build, and safety boundaries.
- [ ] LICENSE contains the Apache License 2.0 text.
- [ ] NOTICE and third-party notices are current.
- [ ] CONTRIBUTING, SECURITY, SUPPORT, and CODE_OF_CONDUCT are present.
- [ ] GitHub topics are set.
- [ ] Repository description is concise and searchable.
- [ ] GitHub Pages link is enabled if the landing page is used.

## Engineering quality

- [ ] `dotnet restore` succeeds.
- [ ] warning-free `dotnet build -c Release -warnaserror` succeeds on Windows.
- [ ] unit tests succeed.
- [ ] the configured line-coverage regression floor succeeds.
- [ ] TRX and Cobertura evidence are retained by CI.
- [ ] CodeQL workflow passes.
- [ ] dependency vulnerability reports pass.
- [ ] Dependabot is enabled for NuGet and GitHub Actions.
- [ ] no secrets, real station files, private captures, or confidential SCL files are committed.

## Safety

- [ ] README warns that ARSVIN can transmit raw Ethernet frames.
- [ ] live mode safety docs are up to date.
- [ ] known limitations are honest and visible.
- [ ] release notes state that the app is not a certified protection test set.
- [ ] testers use isolated lab networks or point-to-point links.

## Release package

- [ ] tag format is `vX.Y.Z` or a valid semantic-version prerelease.
- [ ] tagged commit is already contained in protected `main`.
- [ ] Publisher portable EXE exists and is non-empty.
- [ ] Subscriber portable EXE exists and is non-empty.
- [ ] suite installer exists and passes silent install/uninstall smoke testing.
- [ ] portable ZIP includes executables, README, LICENSE, notices, and relevant docs.
- [ ] CycloneDX SBOM exists, parses, and contains resolved dependency components.
- [ ] SHA-256 checksum file covers all distributed release assets.
- [ ] GitHub build-provenance and SBOM attestations are created for tagged assets.
- [ ] `gh attestation verify` succeeds for a downloaded release file.
- [ ] release notes include highlights, fixes, known limitations, and safety reminders.
- [ ] downloaded artifacts run on a clean Windows machine with Npcap installed for live-network testing.
