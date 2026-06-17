# Modern SV Setup UX

ARSVIN uses a quick looptest-oriented setup workflow instead of a long wizard.

## Core flow

1. Open SCL.
2. Select SV1, SV2, or SV3 from the stream navigator.
3. Select the SCL stream and NIC adapter.
4. Choose the source mode: Manual, Ramp, Sequence, or COMTRADE.
5. Press Check when you want a readable diagnostic report.
6. Start Dry Run or Start Live.

Warnings are visible and actionable, but they do not block live publishing. Only fatal configuration errors block live publishing.

## Stream navigator

The left stream navigator shows the three publisher slots as compact stream cards:

- stream number
- enabled state
- selected SV identity
- APPID / VLAN / sample rate summary
- source mode
- ready / disabled / needs stream state

Clicking a card changes the active stream editor immediately.

## Preflight results window

The Check action opens a dedicated Preflight Results window with:

- fatal count
- warning count
- information count
- stream / area association
- diagnostic detail
- copy report action

This makes warning counts visible without forcing a heavy commissioning workflow.

## Source mode workspace

The selected stream editor includes a dynamic source workspace:

- Manual: quick steady-state looptest waveform / phasor preview
- Ramp: ramp timeline preview
- Sequence: state sequence timeline preview
- COMTRADE: replay-focused workspace with file summary, waveform preview, loop option, and mapping hint

The goal is to make source mode feel like the working context, not just another form field.
