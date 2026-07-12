# Sampled Values Conformance and Interoperability Matrix

This matrix is the working control sheet for ARSVIN profile expansion. It records what is known, what remains provisional, and what evidence is required before a behavior becomes part of a supported profile.

## Evidence status vocabulary

| Status | Meaning |
|---|---|
| Verified-standard | Confirmed from a licensed applicable standard and recorded with edition and clause. |
| Verified-official | Confirmed from an official standards-body or manufacturer document. |
| Verified-capture | Confirmed from an anonymized real-device SCL/PCAP pair. |
| Verified-lab | Confirmed by byte-exact or interoperability testing with a real counterpart. |
| Implemented-generic | Engine supports the mechanism, but no profile-specific compliance claim is made. |
| Provisional | Candidate behavior awaiting stronger evidence. |
| Unknown | Not yet researched. |
| Out-of-scope | Explicitly excluded from the current milestone. |

## Standards source register

| Source | Public edition baseline | Licensed clause review | Owner action |
|---|---|---:|---|
| IEC 61850-6 | 2009 + AMD1:2018 + AMD2:2024 CSV | Required | Obtain/access current licensed copy; map SCL requirements. |
| IEC 61850-7-2 | 2010 + AMD1:2020 CSV | Required | Map abstract SV service semantics. |
| IEC 61850-9-2 | 2011 + AMD1:2020 CSV | Required | Map Ethernet/APDU encoding and required fields. |
| IEC/IEEE 61850-9-3 | 2016 | Required for timing claims | Map PTP profile boundaries and terminology. |
| IEC 61850-10 | 2012 + AMD1:2025 CSV | Required for claim language | Map conformance terminology and test evidence boundaries. |
| IEC 61869-9 | Current licensed edition to verify | Required | Map preferred variants, scaling, sampling, and dataset requirements. |
| IEC TR 61850-90-5 | 2012 public catalogue baseline | Deferred | Separate R-SV architecture program. |
| UCAIug 9-2LE guideline | Revision to identify | Required for 9-2LE claim | Obtain authoritative copy and revision history. |

## Generic Layer-2 SV mapping

| Requirement | Current ARSVIN state | Evidence status | Required next evidence |
|---|---|---|---|
| Ethernet EtherType `0x88BA` | Publisher and Subscriber use Layer-2 SV EtherType | Implemented-generic | Clause mapping and byte-exact fixture. |
| APPID and process-bus header | Implemented | Implemented-generic | Boundary and malformed-frame vectors. |
| VLAN tag, ID, and priority | Implemented | Implemented-generic | Standard clause mapping and tagged/untagged captures. |
| `svID` / `smvID` | Parsed and emitted | Implemented-generic | Edition-specific naming and length tests. |
| Dataset reference | SCL-driven | Implemented-generic | Clause mapping and dataset-resolution fixtures. |
| `confRev` | Parsed and emitted | Implemented-generic | Change/mismatch behavior tests. |
| `smpCnt` | Parsed, emitted, monitored, and wrap-tested | Implemented-generic | Profile-specific wrap rules. |
| `smpSynch` | Compatibility modes implemented | Implemented-generic | Licensed semantics and synchronized/unsynchronized captures. |
| `smpRate` / `smpMod` | Parsed and emitted | Implemented-generic | Edition/profile-specific presence and interpretation rules. |
| Multiple ASDUs per frame | Engine supports `1..8`; UI/docs historically emphasize `1/2/4/8` | Implemented-generic | Profile-specific allowed values and `nofASDU=6` evidence. |
| Dataset-driven payload | Multiple integer, float, quality, time, enum, bit-string, and string kinds supported | Implemented-generic | Validate permitted SV bTypes, widths, order, and nesting. |
| Unknown payload element | Preflight can reject unsupported layouts | Implemented-generic | Compatible-mode raw preservation design. |
| PCAP read/write | Implemented and regression-tested | Implemented-generic | Golden external captures. |
| Observation window | Transport-independent facts, rate estimation, stable-field checks, and counter-wrap detection implemented | Implemented-generic | Integrate with live and PCAP Subscriber pipelines. |
| Profile matching | Evidence-aware weighted detector with explained matches, conflicts, and unknowns implemented | Implemented-generic | Add verified profile definitions after source review. |
| Configuration versus wire | Strict and compatible comparison engine implemented | Implemented-generic | Connect SCL bindings to observed stream facts in the UI. |

## 9-2LE-style profile research

| Topic | Current position | Status | Exit criterion |
|---|---|---|---|
| Profile name and revision | `9-2LE-style` only | Provisional | Identify authoritative guideline and revision. |
| 4 current + 4 voltage layout | Current primary workflow | Provisional | Guideline clause plus golden dataset and captures. |
| Value representation | Current workflow uses SCL-derived layout | Provisional | Byte-level guideline verification. |
| Quality ordering and encoding | Engine supports quality fields | Provisional | Guideline vectors and device capture. |
| Protection sampling variant | Product assumption exists | Provisional | Standard/guideline source plus 50 Hz and 60 Hz captures. |
| High-rate variant | Roadmap candidate | Provisional | Standard/guideline source and performance fixture. |
| Nominal-frequency behavior | Infrastructure can evaluate samples-per-cycle | Provisional | Verify formula, rate, wrap, and frame-rate behavior. |
| Scaling convention | Not yet provenance-aware | Unknown | Verify raw-to-secondary convention and expose source. |
| Publisher interoperability | Generic output works | Provisional | At least one real counterpart test per claimed variant. |
| Subscriber detection | Explainable profile detector foundation exists | Implemented-generic | Add verified definitions and golden known/unknown/mismatch vectors. |

## IEC 61869-9 profile research

| Topic | Current position | Status | Exit criterion |
|---|---|---|---|
| Preferred variant catalogue | Not implemented | Unknown | Licensed clause matrix. |
| Configurable dataset behavior | Generic SCL engine can support it | Provisional | Standard requirement and real SCL evidence. |
| Protection-oriented variant | Roadmap candidate | Provisional | Licensed rates, packing, payload, and timing requirements. |
| Measurement/high-rate variant | Roadmap candidate | Provisional | Licensed requirements and sustained-throughput tests. |
| Sampling basis | Detector supports samples-per-second and samples-per-cycle definitions | Implemented-generic | Verify profile-specific semantics. |
| `nofASDU` values | Engine mechanically supports `1..8` | Provisional | Profile-specific normative/recommended values. |
| Dataset shape and channel order | Must not be assumed fixed 4I+4V | Provisional | Standard plus device SCL matrix. |
| Scaling and units | Not yet profile-provenance-aware | Unknown | Standard clause, SCL source, and real values. |
| Synchronization | Generic `smpSynch` and PTP tooling exist | Provisional | Standard linkage and capture evidence. |
| Public support claim | Not supported yet | Unknown | All mandatory rows reach verified-standard and fixture/device evidence. |

## Engineering workflow outcomes

These are product outcomes, not protocol requirements and not references to any proprietary product implementation.

| Outcome | ARSVIN target |
|---|---|
| SCL-first system view | Import configured streams and show expected-versus-observed state. |
| Configuration versus live comparison | Compare addressing, dataset, packing, rates, and profile evidence side by side. |
| Signal tracing | Trace an SCL stream to observed traffic and decoded channels. |
| Orphan detection | List observed streams absent from SCL and expected streams absent from traffic. |
| MU parameter verification | Validate fields, rate, packing, payload, synchronization, and quality. |
| Packet interval statistics | Add intervals, jitter, gaps, bounded statistics, and evidence export. |
| Triggered recording | Add software trigger rules after profile detection is stable. |
| Redundancy verification | Keep PRP/HSR in a separate evidence-backed milestone. |
| Reusable tests and reports | Use machine-readable expected-profile fixtures and repeatable reports. |

## Fast-track implementation gates

### Gate 0 — Research register

Complete:

- source editions are recorded,
- licensed-source gaps are visible,
- proposed constants remain verified or provisional,
- no unsupported public claim is introduced.

### Gate 1 — Profile infrastructure

Complete in engine code:

- observed facts are independent of WPF and Npcap,
- an observation accumulator estimates rate and counter wrap,
- profile definitions carry source/evidence metadata,
- confidence scoring explains matches, conflicts, and missing evidence,
- strict and compatible configuration comparison are available,
- unknown streams remain observable,
- built-in production catalog contains only a generic Layer-2 fallback.

Remaining Gate 1 integration:

- feed live and PCAP observations into the accumulator,
- bind expected SCL configuration to the comparison engine,
- expose results without visual noise in Subscriber.

### Gate 2 — Formalized 9-2LE-style support

Complete when:

- the authoritative guideline revision is reviewed,
- applicable 50 Hz and 60 Hz fixtures exist,
- byte-exact Publisher and Subscriber tests pass,
- scaling provenance is visible,
- at least one real counterpart has been exercised.

### Gate 3 — IEC 61869-9 protection support

Complete when:

- licensed clauses are mapped,
- preferred variant parameters are frozen,
- SCL and PCAP fixtures pass,
- real-device evidence exists,
- performance remains stable.

### Gate 4 — High-rate and redundancy support

Complete when:

- sustained throughput is benchmarked,
- queues are bounded,
- UI rendering is decoupled from packet rate,
- PRP/HSR behavior has dedicated evidence,
- no false sample-gap diagnosis occurs on clean captures.

## Definition of public `Supported`

A profile can be labeled supported only when all are true:

- standard/guideline edition and clause register complete,
- deterministic fixtures committed,
- byte-exact generation test passing,
- subscriber decode and detection tests passing,
- mismatch and malformed tests passing,
- scaling source explicit,
- performance baseline documented,
- real-device evidence recorded,
- support matrix updated,
- claim language reviewed against IEC 61850-10 boundaries.
