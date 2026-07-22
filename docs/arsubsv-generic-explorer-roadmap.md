# ArSubsv generic Sampled Values explorer roadmap

## Product decision

ArSubsv is a generic IEC 61850 Sampled Values subscriber, inspector, measurement viewer, and evidence tool.

The receive path must not contain device-family branches such as ABB, Siemens, SEL, GE, or any other manufacturer identity. Product identity may appear in imported evidence or reports, but it must never select a parser, dataset order, engineering scale, quality layout, or health result.

The intended receive pipeline is:

```text
Ethernet / VLAN / APPID
        ↓
generic IEC 61850-9-2 APDU and ASDU parser
        ↓
generic seqOfData structural inspection
        ↓
optional SCL-driven ordered dataset mapping
        ↓
explicit standard-layout or engineering-context evidence
        ↓
waveform, RMS, phasor, quality, and report
```

## Reference behavior

A proven SV engineering workflow discovers one or more streams, shows waveforms, RMS values, phase angles, phasors, individual samples, recording/replay, and reports. Configurable IEC 61869-9 datasets are interpreted and checked through imported SCL rather than manufacturer heuristics.

ArSubsv should reproduce that workflow while keeping every interpretation explainable.

## Audit findings

### Correct foundations already present

- One generic Ethernet/APDU/ASDU parser is shared across live capture and PCAP replay.
- SCL can provide an ordered payload layout and configuration comparison.
- Raw protocol values, scaling provenance, timebase provenance, quality diagnostics, and evidence reports already exist.
- Selected-stream visualization is stable and bounded.
- CT/VT primary/secondary display context is explicit and does not alter raw bytes.

### Incorrect or unsafe behavior to remove

1. **Unbound payload auto-layout**
   
   The current Subscriber recognizes selected payload pair counts and assigns names such as Ia, Ib, Ic, In, Va, Vb, Vc, and Vn. A 64-byte payload can therefore be interpreted as a fixed 4I+4V layout without SCL. This is structural guessing and can mislabel configurable IEC 61869-9 datasets.

2. **Synthetic IEC references**
   
   Generic traffic can receive generated references such as `TCTR1/AmpSv.instMag.i`. Those references look authoritative even though they were not read from SCL or the wire.

3. **Unbound engineering scaling**
   
   Packet length and observed sampling rate were previously sufficient to activate provisional A/V scaling. This is now hardened: generic unbound traffic remains raw counts.

4. **Unknown mapping treated as stream warning**
   
   A valid stream without SCL is currently classified as WARN because channel names or scaling are unresolved. Missing semantics are not a protocol failure. Protocol/stream integrity and interpretation completeness must be separate states.

5. **Waveform model fixed to eight named channels**
   
   The current waveform container is optimized for Ia/Ib/Ic/In and Va/Vb/Vc/Vn. Configurable datasets require a generic selected-element trace model before semantic aliases are added.

6. **Profile-oriented compact state**
   
   The UI emphasizes profile and confidence. The primary receive workflow should instead emphasize wire validity, mapping source, semantic completeness, and measurement provenance.

7. **Global parse health can remain sticky**
   
   Lifetime parse-error count can keep the global state BAD after the current traffic has recovered. Lifetime counters should remain evidence, while current health should use a rolling window.

## Implemented in P3A

### Generic seqOfData inspection

`SvGenericPayloadInspector` exposes every complete big-endian 32-bit word through:

- byte offset,
- raw hexadecimal bytes,
- signed INT32 representation,
- unsigned UINT32 representation,
- IEEE-754 FLOAT32 representation,
- structural position in an eight-byte group when applicable.

An eight-byte grouping shape is only structural evidence. The second word is not called quality until SCL or an explicit reviewed standard layout resolves it.

Trailing bytes are preserved and reported instead of silently discarded.

### Generic ASDU inspection

`SvGenericAsduInspector` exposes:

- `svID`,
- dataset reference,
- `smpCnt`,
- `confRev`,
- `smpSynch`,
- presence of `refrTm`,
- optional `smpRate` and `smpMod`,
- generic payload inspection.

Dataset reference presence does not mean dataset semantics are already known; SCL binding is still required.

### Scaling hardening

Engineering A/V scaling now requires all of the following:

1. SCL-derived current or voltage semantics,
2. fixed 4-current/4-voltage value-quality structure,
3. expected payload size,
4. declared or observed protection-rate evidence.

Packet shape, rate, product name, MAC, APPID, `svID`, amplitude, and manufacturer identity cannot activate scaling on their own.

## Next implementation tranches

### P3B — integrate generic explorer into live Subscriber

Replace the unbound auto-layout path with `SvGenericPayloadInspector`.

Without SCL, the Decoded table must show:

| Field | Meaning |
|---|---|
| Offset | byte position inside seqOfData |
| Word | generic word index |
| INT32 | signed big-endian representation |
| UINT32 | unsigned representation |
| FLOAT32 | alternate representation, clearly marked |
| Raw | exact bytes |
| Semantics | unresolved |

No Ia/Va labels, no A/V units, no quality health decisions, and no phasor calculation are permitted in raw generic mode.

### P3C — SCL-driven semantic mapping

Resolve the ordered path:

```text
SampledValueControl
  → datSet
  → DataSet FCDA order
  → DO/DA/BType/CDC
  → payload element decoder
```

Each displayed field must carry a mapping source:

- wire observed,
- SCL derived,
- explicit standard-layout selection,
- manual engineering context,
- device-validated evidence.

Unknown, absent, and conflicting states must remain distinct.

### P3D — generic selected-element waveform

Introduce a generic numeric trace model independent of Ia/Ib/Ic/Va/Vb/Vc.

- Any mapped numeric element can be selected for waveform display.
- Raw mode may plot an explicitly selected word as counts.
- RMS is available when a complete time window exists.
- Phase angle and phasor are available only when nominal frequency/timebase and signal semantics are resolved.
- Standard phase aliases are conveniences derived from SCL, not parser assumptions.

### P3E — explainable health layers

Expose four independent states:

```text
PROTOCOL       GOOD / BAD
STREAM         GOOD / WARN / BAD
CONFIGURATION  MATCH / WARN / BAD / NOT LOADED
MEASUREMENT    RESOLVED / PARTIAL / RAW
```

The global headline should be driven by current protocol and stream integrity. Missing SCL or unresolved engineering scale must not turn a valid stream BAD.

### P3F — evidence and workflow parity

- record/replay and COMTRADE export,
- detailed individual-sample view,
- zero-crossing and timing statistics,
- packet-interval histogram,
- optional-field verification,
- SCL orphan discovery,
- printable and JSON evidence reports,
- selected-stream stability under live refresh,
- capture/application drop attribution without claiming kernel drops that were not observed.

## Real-device evidence policy

PCAP and SCL from an ABB SMU615, Siemens merging unit, SEL device, or any other product are regression fixtures for the generic engine. They are not permission to add product-specific decoding branches.

A real-device fixture should prove:

- generic parser compatibility,
- SCL mapping correctness,
- sample-counter and timing behavior,
- quality interpretation,
- engineering-value agreement with known injection,
- report reproducibility.

Any device-specific exception must be documented as an implementation defect or explicit compatibility observation, isolated from the generic standards path, and never silently selected by manufacturer identity.
