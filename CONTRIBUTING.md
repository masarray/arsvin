# Contributing to ARSVIN

Thank you for considering a contribution to ARSVIN.

ARSVIN is an IEC 61850 Sampled Values engineering suite for authorized laboratory, development, troubleshooting, commissioning-preparation, and education workflows. Contributions should improve observable engineering behavior without overstating safety, conformance, or interoperability.

## Licensing and acceptance

The current community edition is `GPL-3.0-or-later` and the project maintains a separate commercial-licensing path.

Before merge, contributors must:

1. read and affirmatively accept [CONTRIBUTOR-LICENSE-AGREEMENT.md](CONTRIBUTOR-LICENSE-AGREEMENT.md);
2. add a Developer Certificate of Origin sign-off to every commit;
3. have the legal right and any required employer or organizational authorization to submit the contribution; and
4. identify any third-party material together with its provenance and license.

Sign commits with:

```text
Signed-off-by: Full Name <email@example.com>
```

The CLA grants rights needed for GPL-compatible publication and a separate commercial path. It does not transfer ownership of the contributor’s work.

## Engineering principles

- Keep Publisher and Subscriber responsibilities explicit and independently testable.
- Keep protocol and transport behavior in the shared engine rather than duplicating it in application UI code.
- Add analysis features only when they improve Sampled Values visibility, interoperability, troubleshooting, or evidence quality.
- Keep live packet capture and transmission guarded, visible, and clearly labelled.
- Prefer scoped evidence language over universal marketing claims.
- Update documentation when behavior, UI, operational assumptions, licensing, or packaging changes.
- Preserve committed dependency lock files and immutable public releases.

## Independent-development and provenance boundary

External software may be used only as a lawfully licensed black-box interoperability endpoint within the applicable authorization and license boundary.

Do not use external source, generated bindings, API composition, examples, tests, internal structures, documentation wording, screenshots, UI layouts, icons, reports, or extracted resources as implementation design material unless the project has documented rights to incorporate and relicense that material.

Public fixtures should be synthetic or contributor-owned. Real SCL, COMTRADE, PCAP, screenshots, and diagnostics require documented sharing rights and sanitization.

Do not submit:

- confidential customer, employer, station, credential, or restricted network information;
- production captures or configuration files without explicit redistribution authority;
- proprietary manuals, screenshots, support responses, binaries, or copied UI assets; or
- unverified profile constants presented as normative requirements.

## Development setup

Recommended environment:

- Windows 10 or Windows 11, x64;
- .NET 8 SDK, feature band 8.0.4xx;
- PowerShell 7+;
- Npcap for authorized live packet capture or publishing tests; and
- an independent packet dissector or process-bus analyzer for verification.

Build with the committed dependency graph:

```powershell
.\build.ps1
```

Run tests and coverage gates:

```powershell
.\scripts\test-with-coverage.ps1 -MinimumWholeEngineLineCoverage 13 -MinimumLineCoverage 70
```

Create portable release artifacts:

```powershell
.\scripts\publish-release.ps1 -Version 0.4.0
```

## Dependency updates

NuGet versions are centrally managed in `Directory.Packages.props`, and each project commits `packages.lock.json`.

When deliberately changing a dependency:

1. update the central version;
2. regenerate lock files from the repository root;
3. review direct and transitive changes, license compatibility, and vulnerability output; and
4. commit the package version and lock-file changes together.

```powershell
dotnet restore ARSVIN.sln --force-evaluate
```

Do not hand-edit lock files or include unrelated lock-file churn.

## Pull request expectations

A pull request should state:

- the engineering problem and application/engine boundary;
- the source of protocol requirements and test expectations;
- the evidence environment: automated test, simulator, loopback, isolated laboratory equipment, or approved commissioning environment;
- operational and data-handling impact;
- public wording or documentation changes; and
- any limitations not covered by the validation.

Before opening a PR, verify:

- relevant Release builds and deterministic tests pass;
- whole-engine and protocol-core coverage gates remain satisfied;
- public-site, terminology, licensing, and provenance checks pass;
- active network behavior remains explicitly guarded;
- no claim implies formal conformance, deterministic timing, functional safety, cybersecurity approval, switching authority, or IED consumption proof;
- screenshots and captures are sanitized and legally shareable; and
- release changes preserve immutable existing releases and required legal notices.

## Reporting issues

Use the structured GitHub issue forms. Include the ARSVIN version, application, Windows and Npcap versions, reproduction steps, and the smallest sanitized evidence needed to explain the problem.

Never post confidential SCL, COMTRADE, PCAP, station addressing, credentials, customer identity, or proprietary third-party material in a public issue.