# ARSVIN Subscriber Verification App

ARSVIN Subscriber is a separate WPF companion application for receiver-side IEC 61850 Sampled Values verification. It is intentionally separated from the publisher so the publisher remains focused on SV generation.

## Scope

ARSVIN Subscriber can:

- capture IEC 61850 Sampled Values frames from an Npcap adapter,
- decode APPID, VLAN, source/destination MAC, svID, DataSet reference, confRev, smpCnt, smpRate, smpMod, smpSynch, nofASDU, and payload length,
- import SCL/SCD and bind received streams to expected `SampledValueControl` entries,
- verify SCL-derived fields such as APPID, destination MAC, VLAN, svID, confRev, nofASDU, sample rate, and payload layout,
- decode payload values when the SCL dataset layout is available,
- track sample-counter continuity, duplicates, forward jumps, out-of-order samples, and receiver-side frame rate,
- export a Markdown receiver-side verification report.

## What it does not prove

SV is multicast and unacknowledged. ARSVIN Subscriber verifies that **this PC/NIC** receives and decodes the stream. It does not prove that any relay or IED consumed the stream.

It is not a formal IEC 61850 conformance test certificate.

## Recommended lab workflow

1. Start ARSVIN Publisher on one NIC or PC.
2. Start ARSVIN Subscriber on a different PC/NIC or mirrored port.
3. Import the same station SCD file into Subscriber.
4. Select the capture adapter.
5. Start listening.
6. Confirm stream health:
   - `GOOD`: bound to SCL and no current issues,
   - `WARN`: unbound/no SCL or non-blocking issue,
   - `BAD`: payload/SCL mismatch, parse issue, or sequence issue.
7. Export the report as evidence.

## Build

```powershell
dotnet build .\src\ARSVIN.Subscriber\ARSVIN.Subscriber.csproj -c Release
```
