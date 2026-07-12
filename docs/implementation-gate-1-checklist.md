# Gate 1 — Profile Infrastructure Checklist

## Completed

- [x] Observed facts are independent of WPF and Npcap.
- [x] Observation windows calculate frame and sample rates.
- [x] Stable-field changes become diagnostics instead of false values.
- [x] Sample-counter wrap candidates are detected from observed sequences.
- [x] Profile definitions carry evidence status and source metadata.
- [x] Sampling basis supports samples per cycle, samples per second, and custom definitions.
- [x] Profile detection explains match, conflict, and unknown evidence.
- [x] Missing observations do not improve confidence.
- [x] Strict and compatible configuration comparison are available.
- [x] Unknown and conflicting receive traffic remains observable.
- [x] Built-in catalog contains no unverified named-profile constants.
- [x] Deterministic engine tests protect the infrastructure.
- [x] Active public documentation uses neutral engineering terminology.
- [x] CI and Pages validate public terminology neutrality.

## Remaining integration

- [ ] Feed live parsed frames into the observation accumulator.
- [ ] Feed PCAP-imported frames into the same observation accumulator.
- [ ] Convert selected SCL stream models into expected configurations.
- [ ] Add compact profile confidence state to Subscriber.
- [ ] Add expandable mismatch and evidence detail.
- [ ] Add report serialization for observed facts and detection evidence.

The remaining items are integration work. They do not require profile-specific constants.
