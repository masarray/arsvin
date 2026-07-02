# ArSubsv DigSubAnalyzer UI Port

This patch moves ArSubsv away from the early light prototype layout and into the same family as Mas Ari's DigSubAnalyzer process-bus workspace.

## What changed

- Dark engineering shell.
- Compact top capture rail.
- Stream explorer at the left.
- Selected stream inspector at the right.
- Scope / Phasor / Values / Frame / Diagnostics tabs.
- Dark oscilloscope and phasor controls.
- Auto fixed-layout payload decoding for common value+quality SV payloads.

## Waveform without SCL

ArSubsv now attempts auto decoding when SCL is not loaded and the ASDU payload is a recognizable fixed value+quality layout:

- 3I
- 4I+4V / 9-2LE-style
- 8I+4V
- 9I+6V
- 12I+4V
- 12I+8V

For custom IEC 61869-9 layouts, SCL is still recommended because the packet payload itself does not carry full semantic channel names.
