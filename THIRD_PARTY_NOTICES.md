# Third-Party Notices

This file summarizes third-party components that ARSVIN depends on directly or expects users to install for live packet publishing.

## SharpPcap

- Package: `SharpPcap`
- Purpose: Packet capture and injection API used for live Ethernet publishing support
- License: MIT
- Project: https://github.com/dotpcap/sharppcap
- Package: https://www.nuget.org/packages/SharpPcap

SharpPcap is consumed as a NuGet package and is not authored by the ARSVIN maintainer.

## Npcap

- Purpose: Windows packet capture/transmission driver required for live packet publishing
- Project: https://npcap.com/

Npcap is not bundled in this repository. Users install it separately and are responsible for following its license and usage terms.

## .NET and WPF

ARSVIN is built with .NET and WPF. See Microsoft documentation and license terms for the installed SDK/runtime components.
