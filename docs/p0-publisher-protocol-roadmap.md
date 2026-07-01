# P0 Publisher Protocol Upgrade

This document records the first engineering-grade publisher upgrades for ARSVIN. The scope stays intentionally narrow: ARSVIN is a Sampled Values publisher, not a process-bus analyzer.

## Implemented in this package

### Multi-ASDU Sampled Values publishing

ARSVIN now accepts SCL streams with `nofASDU` from 1 to 8. The publisher packs sequential samples into one SavPdu frame:

- `nofASDU=1`: one sample per Ethernet frame.
- `nofASDU=2/4/8`: sequential samples are packed as multiple ASDUs in one frame.
- `smpCnt` is incremented per ASDU, not per Ethernet frame.
- publication frame rate is calculated as `sampleRateHz / nofASDU`.

The current hard limit is 8 ASDUs per frame. Higher values are blocked by preflight to avoid accidentally generating oversized or non-interoperable frames before dedicated test vectors exist.

### Second-aligned sample counter start

The runtime initializes `smpCnt` from the fractional UTC second when publishing starts. This is closer to synchronized SV publisher behavior than always starting at zero. It remains a lab/compatibility behavior and is not a certified PTP implementation.

### Publisher validation and frame preview

A publisher-side validation layer now checks the SCL stream before live TX:

- APPID presence and zero APPID warning.
- destination MAC presence, multicast sanity, and common SV multicast range warning.
- `svID` / `smvID` presence.
- DataSet resolution.
- `confRev`, `smpRate`, `smpMod`, and `nofASDU` readiness.
- payload layout support.

Preflight also reports a frame preview summary: `nofASDU`, sample rate, publication rate, and payload bytes per ASDU.

### Quality bit simulation foundation

The engine now has `SampledValueQuality` helpers for common publisher simulation states:

- good
- invalid
- questionable
- oldData
- test
- operatorBlocked

The UI currently publishes good quality by default. The next UI step is exposing this as a compact quality selector per stream or per channel.

### Generated PCAP evidence foundation

The engine now has a small generated-frame PCAP export helper. This is not a network analyzer; it only writes frames produced by the publisher so they can be opened in Wireshark or shared as evidence.

### Golden protocol tests

Unit tests were added for:

- multi-ASDU frame build/parse round-trip.
- ASDU sample counter rollover.
- wrong payload batch rejection.
- frame preview publication rate.
- quality bit encoding.
- second-aligned sample counter.
- generated PCAP header/output.

## Still intentionally not included

- full network analyzer
- process-bus scanner
- subscriber diagnostics
- GOOSE/SV mismatch live diagnosis
- certified PTP grandmaster/slave operation
- calibrated analog source behavior

## Next P0 follow-up recommended

1. Expose `SampledValueQuality` in the WPF UI as a compact stream-level selector.
2. Add one-click dry-run PCAP export from the WPF app.
3. Add golden PCAP files under `tests/golden/` and compare byte-level expected output.
4. Add `nofASDU=2`, `nofASDU=4`, and `nofASDU=8` sample SCL files to CI test data.
5. Add jitter/p95/p99 TX health statistics to the publish status bar.

## P0.1 Publisher evidence update

This revision keeps ARSVIN scoped as a publisher, not an analyzer.

Added evidence-oriented publisher functions:

- TX quality preset selection for the selected publisher slot: good, invalid, questionable, oldData, test, operatorBlocked.
- Runtime payload quality fields now follow the selected preset instead of always emitting good quality.
- Preflight shows a warning when a non-default quality preset is selected.
- Generated PCAP export writes ARSVIN-created SV Ethernet frames without capturing live network traffic.
- Publish status includes per-publisher `q=` labels so operator intent is visible during live/dry runs.

Generated PCAP export is meant for offline evidence in Wireshark or third-party SV tools. It is not packet capture and it does not turn ARSVIN into a network analyzer.

## P0.2 TX Timing Health update

This revision adds publisher-side timing health only. It does not capture external packets and does not make ARSVIN a process-bus analyzer.

The runtime records, per enabled publisher slot:

- target publication rate, derived from `sampleRateHz / nofASDU`;
- actual publication rate observed from the TX loop;
- target frame interval;
- average and maximum absolute jitter;
- late frame count;
- missed schedule count;
- average and maximum send duration.

The status bar shows a compact global summary using the worst status from all enabled publishers:

```text
TX Timing: GOOD act=1599.8/1600.0fps jitter=8/42us late=0 missed=0 send=6/18us maxLate=20us
```

Status meaning:

- `GOOD`: frame rate and local TX timing are stable enough for lab publishing.
- `WARN`: the publisher is running but jitter/late frames are visible.
- `BAD`: the runtime missed at least one schedule slot, actual FPS is significantly low, or jitter is larger than one target interval.

This is a Windows/Npcap runtime health indicator, not certified PTP or hardware timestamp evidence. Use it to determine whether the PC/NIC is strong enough for the selected SV profile.
