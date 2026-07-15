# Sampled Values Profile Infrastructure

ARSVIN separates observed wire facts, configured expectations, and profile claims. This keeps the engine useful for unknown traffic while preventing unverified profile constants from becoming production claims.

## Data flow

```text
Live Npcap frame ─┐
                  ├── SvStreamObservationManager
Imported PCAP ────┘          ↓
                     per-stream bounded window
                              ↓
                    SvObservedStreamFacts
                         ├── SvProfileDetector
                         └── SvConfigurationComparer
```

The profile infrastructure is part of the shared `ARSVIN.Engine` assembly and has no dependency on WPF or Npcap. Subscriber live capture and PCAP replay use the same manager and stable stream identity, so both paths produce the same facts and diagnostics contract.

## Observed facts

An observation window can describe:

- EtherType and APPID,
- destination MAC and VLAN,
- `svID` and dataset reference,
- `confRev`,
- ASDU count and payload length,
- declared sample rate and mode,
- measured frames and samples per second,
- observed sample-counter wrap,
- nominal frequency when supplied by trusted context,
- ordered dataset element signatures,
- unstable-field and insufficient-window diagnostics.

Fields that change during the window become unknown with a diagnostic instead of producing a false stable value.

## Profile definitions

A profile definition can carry:

- stable ID and display name,
- profile family,
- samples-per-cycle, samples-per-second, or custom sampling basis,
- expected EtherType,
- allowed ASDU counts,
- expected payload length,
- expected dataset count and ordered signature,
- nominal-frequency options,
- expected sample-counter wrap,
- rate tolerance,
- evidence status and source metadata.

The built-in catalog currently contains only a generic SCL-driven Layer-2 SV fallback. Numeric definitions for named profiles remain outside the production catalog until the research gate is satisfied.

## Explainable detection

`SvProfileDetector` evaluates only facts present in both the observation and definition. Each evaluated field produces one of:

- match,
- conflict,
- unknown.

Results include weighted evidence, score, matched and conflicting weight, and confidence:

```text
Unknown
Possible
Likely
Confirmed
Conflict
```

A high score is an engineering classification result, not a conformance certificate.

## Configuration-versus-wire comparison

`SvExpectedStreamConfigurationFactory` converts the SCL-bound `SampledValuesPublisherProfile` into a transport-neutral `SvExpectedStreamConfiguration`. The expected configuration includes addressing, identifiers, revision, packing, payload length, declared sampling fields, and ordered dataset signature.

`SvStreamObservationManager` compares that expected configuration with the rolling `SvObservedStreamFacts` for every bound live or PCAP stream. The resulting immutable snapshot carries:

- the expected configuration,
- field-level findings,
- comparison mode,
- exact/warning/error counts,
- a compact summary such as `Exact`, `2 warnings`, or `1 error`.

Two modes are available:

- **Compatible** — the Subscriber default; mismatches become warnings suitable for troubleshooting and unfamiliar devices.
- **Strict** — explicit opt-in for validation, preflight, and formal test cases; mismatches become errors.

Neither mode stops receive-side capture or decoding. Unknown and conflicting streams remain visible.

Before accepting an SCL candidate, the observation manager requires APPID, destination MAC, and VLAN to identify the same configured stream. A candidate that fails this address gate is rejected instead of contaminating observed facts with the wrong dataset layout.

## Subscriber compact state

The selected stream exposes one compact analysis strip instead of permanent large evidence cards:

```text
PROFILE       Generic Layer-2 SV
CONFIDENCE    Unknown · insufficient evidence
SCL MATCH     Exact | N warnings | N errors
WINDOW        duration · observed samples
```

Detailed detector evidence, configuration findings, and observation diagnostics remain collapsed behind an expandable evidence panel. Capture and visualization continue without requiring repeated manual selection.

Waveform, phasor, and RMS collections use one reset notification per UI refresh. The visual layer withholds partial waveform windows until a complete two-cycle set is available, then retains the most recent complete window if the next refresh is incomplete. Compatible SCL warnings remain warnings and do not force the stream into a blocking `BAD` state.

## Evidence report bundle

Subscriber Export creates two files from the same report snapshot:

- a Markdown engineering report for review and handover,
- a JSON evidence document using schema `arsvin.sv-subscriber-evidence/v1` for automation, archival, and later comparison.

Both files include:

- generation time, product version, informational version, repository, and source commit,
- live, PCAP, or mixed input provenance,
- SCL path, adapter, user filter, capture duration, frame counts, parse errors, and filtered-frame counts,
- per-stream identity, runtime integrity, quality, cursor state, waveform readiness, and phasors,
- first and last observation timestamps, frame and sample counts, measured rates, sample-counter transitions, and wrap evidence,
- every stable observed fact with its provenance (`WireObserved`, `CaptureCalculated`, `SclDerived`, or trusted context),
- profile definition, evidence maturity, confidence, weighted match evidence, and source metadata,
- expected SCL configuration, comparison mode, exact/warning/error summary, and field-level findings,
- runtime and observation diagnostics.

Unknown nullable values remain explicit `null`; unknown text fields remain empty strings in JSON and render as `unknown` in Markdown. They are not silently removed or interpreted as matches. The report schema validates generation time, product identity, stream count, and unique stream keys before serialization.

GitHub Actions injects `GITHUB_SHA` into `SourceRevisionId`; the .NET informational version therefore carries the build commit for release and CI artifacts. Local builds without source revision metadata report the commit as `unknown` rather than inventing one.

## Evidence report comparison

The Subscriber **Compare** action accepts a baseline and candidate `arsvin.sv-subscriber-evidence/v1` JSON report and writes a paired comparison bundle:

- Markdown for engineering review,
- JSON using schema `arsvin.sv-subscriber-evidence-comparison/v1` for automation and regression gates.

Comparison uses the full stable stream key first. Unmatched streams are then paired through a logical identity made from APPID, destination MAC, VLAN, `svID`, and dataset reference. Source MAC is deliberately excluded from that fallback identity so publisher/NIC failover is reported as a source change rather than a false removed-plus-added stream.

The comparison classifies:

- report-level schema, software, commit, capture-source, SCL-source, and health changes,
- added, removed, changed, and unchanged logical streams,
- health regression or recovery,
- source-MAC failover,
- `confRev`, ASDU packing, sample-rate, and sample-mode changes,
- sequence gaps, duplicates, out-of-order frames, payload issues, and SCL mismatches,
- observation-window size and measured-rate changes,
- SCL binding and configuration-comparison regression,
- profile identity and confidence changes,
- payload, counter-wrap, nominal-frequency, dataset-signature, provenance, and diagnostic changes.

Changes are assigned `Info`, `Warning`, or `Error` severity. Removed streams, health transitions to `BAD`/`ERROR`, new out-of-order or payload failures, blocking configuration errors, profile conflicts, and dataset-signature changes are treated as high-signal regressions. Measured-rate changes within the one-percent comparison tolerance do not create false warnings.

## Current integration boundary

`SvStreamObservationManager` and Subscriber now:

- accept parsed frames from live Npcap capture and classic-PCAP replay;
- group frames by source, destination, VLAN, APPID, `svID`, and dataset reference;
- deliberately keep `confRev` outside the key so revision changes remain observable;
- retain input provenance (`LiveCapture` or `PcapReplay`);
- validate the SCL candidate against the observed address tuple;
- convert a bound SCL stream into `SvExpectedStreamConfiguration`;
- run Compatible comparison by default, with Strict available per observation call;
- evaluate the built-in evidence-aware profile catalog;
- expose compact profile, confidence, SCL match, and observation-window state;
- expose detailed evidence only on demand;
- keep full-window waveform, phasor, and RMS visualization stable through bulk collection refreshes;
- serialize the same observation, profile, configuration, provenance, source, and build evidence into paired Markdown and JSON reports; and
- compare baseline and candidate evidence reports through deterministic, severity-ranked Markdown and JSON output.

## Next integration

1. Add profile-specific definitions only after source review and deterministic evidence.
2. Accumulate real comparison history before defining organization-specific pass/fail policy or CI blocking thresholds.
