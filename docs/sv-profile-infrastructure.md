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

The profile infrastructure is part of the shared `ARSVIN.Engine` assembly and has no dependency on WPF or Npcap. Subscriber live capture and PCAP replay now use the same manager and stable stream identity, so both paths produce the same facts and diagnostics contract.

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

`SvConfigurationComparer` compares configured expectations with observed facts for addressing, identifiers, dataset, revision, packing, payload, and declared sampling fields.

Two modes are available:

- **Strict** — mismatches become errors suitable for validation and live-transmission preflight.
- **Compatible** — mismatches become warnings suitable for troubleshooting and unfamiliar devices.

Neither mode stops receive-side capture or decoding. Unknown and conflicting streams remain visible.

## Current integration boundary

`SvStreamObservationManager` now:

- accepts parsed frames from live Npcap capture and classic-PCAP replay;
- groups frames by source, destination, VLAN, APPID, `svID`, and dataset reference;
- deliberately keeps `confRev` outside the key so revision changes remain observable;
- retains input provenance (`LiveCapture` or `PcapReplay`);
- adds SCL-derived dataset signatures when a stream is bound; and
- exposes immutable rolling facts to the Subscriber runtime snapshot.

## Next integration

1. Convert selected SCL stream bindings into `SvExpectedStreamConfiguration`.
2. Run strict or compatible configuration comparison per observed stream.
3. Present one compact profile/confidence and SCL-match state per selected stream.
4. Show evidence and mismatch details on demand rather than adding permanent visual noise.
5. Add profile-specific definitions only after source review and deterministic evidence.
