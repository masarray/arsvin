# Third-Party Notices

ARSVIN project material covered by the current community license is distributed under `GPL-3.0-or-later`. The components below retain their own licenses and are not relicensed merely because they are used, restored, bundled, or documented by ARSVIN.

This file summarizes third-party components that ARSVIN depends on directly, includes in self-contained releases, or expects users to install for live packet capture and transmission. Release SBOM data provides the resolved managed dependency graph for a specific build.

## SharpPcap

- Package: `SharpPcap`
- Purpose: packet capture and injection API used by ARSVIN Publisher and ArSubsv Subscriber
- License: MIT
- Project: https://github.com/dotpcap/sharppcap
- Package: https://www.nuget.org/packages/SharpPcap

SharpPcap is consumed as a NuGet package and is not authored by the ARSVIN maintainer. Its license and notices remain applicable.

## Npcap

- Purpose: Windows packet capture and transmission driver for live Ethernet workflows
- Project: https://npcap.com/

Npcap is not bundled in this repository or silently installed by the ARSVIN installer. Users install it separately and are responsible for its license and usage terms.

## .NET and WPF

ARSVIN is built with .NET and WPF. Self-contained release binaries include applicable .NET runtime components. The relevant Microsoft licenses and notices remain applicable to those components.

## Inno Setup

- Purpose: build tooling and installer runtime used to create `ARSVIN-Suite-Setup-win-x64.exe`
- Project: https://jrsoftware.org/isinfo.php

Inno Setup is not required to run the portable applications. Its license and third-party component notices apply to the generated installer as documented by that project.

## Build and test dependencies

The exact direct and transitive NuGet dependency graph is locked in committed `packages.lock.json` files. Tagged releases include a CycloneDX SBOM for resolved runtime dependencies. Test-only and hosted build-service components may be governed by additional licenses and are not necessarily part of the distributed application.

## Commercial licensing boundary

A separate ARSVIN commercial agreement can cover only rights controlled by the relevant copyright holder. It does not replace, sublicense beyond, or remove obligations imposed by third-party licenses.