# Third-Party Notices

This file summarizes third-party components that ARSVIN depends on directly, includes in self-contained releases, or expects users to install for live packet capture and transmission.

## SharpPcap

- Package: `SharpPcap`
- Purpose: Packet capture and injection API used by ARSVIN Publisher and ArSubsv Subscriber
- License: MIT
- Project: https://github.com/dotpcap/sharppcap
- Package: https://www.nuget.org/packages/SharpPcap

SharpPcap is consumed as a NuGet package and is not authored by the ARSVIN maintainer.

## Npcap

- Purpose: Windows packet capture/transmission driver required for live Ethernet workflows
- Project: https://npcap.com/

Npcap is not bundled in this repository or silently installed by the ARSVIN installer. Users install it separately and are responsible for following its license and usage terms.

## .NET and WPF

ARSVIN is built with .NET and WPF. Self-contained release binaries include applicable .NET runtime components. See Microsoft documentation and license terms for those components.

## Inno Setup

- Purpose: Build tooling and installer runtime used to create `ARSVIN-Suite-Setup-win-x64.exe`
- Project: https://jrsoftware.org/isinfo.php

Inno Setup is not required to run the portable applications. Its license and third-party component notices apply to the generated installer as described by the Inno Setup project.
