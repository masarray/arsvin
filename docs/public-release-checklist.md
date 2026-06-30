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
- [ ] `dotnet build -c Release` succeeds on Windows.
- [ ] `dotnet test -c Release` succeeds.
- [ ] CodeQL workflow passes.
- [ ] Dependabot is enabled for NuGet and GitHub Actions.
- [ ] No secrets, real station files, private captures, or confidential SCL files are committed.

## Safety

- [ ] README warns that ARSVIN can transmit raw Ethernet frames.
- [ ] Live mode safety docs are up to date.
- [ ] Known limitations are honest and visible.
- [ ] Release notes state that the app is not a certified protection test set.
- [ ] Testers use isolated lab networks or point-to-point links.

## Release package

- [ ] Tag format is `vX.Y.Z`.
- [ ] Portable ZIP includes executable, README, LICENSE, NOTICE, and relevant docs.
- [ ] Release notes include highlights, fixes, known limitations, and safety reminders.
- [ ] Downloaded ZIP runs on a clean Windows machine with Npcap installed.
