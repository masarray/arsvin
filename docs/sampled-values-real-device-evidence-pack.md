# Sampled Values Real-Device Evidence Pack

This document defines the minimum evidence needed before ARSVIN promotes a Sampled Values behavior from structural inference to device-observed or lab-interoperable status.

## Required inputs

Each evidence pack should contain sanitized, mutually consistent artifacts:

- device and firmware identity recorded outside protocol rules;
- SCD, CID, ICD, or IID file used for the test;
- zero-input PCAP;
- known current and voltage injection PCAPs;
- nominal system frequency;
- explicit CT, VT, or sensor conversion context;
- test-set values and tolerances;
- time-synchronization state;
- capture adapter and operating-system details;
- expected decoding, scaling, quality, continuity, RMS, and phasor results.

Device identity is evidence metadata. It must not become a hidden scaling or payload-layout rule.

## Capture cases

A first protection-stream pack should include:

1. zero input;
2. balanced nominal current;
3. balanced nominal voltage;
4. unbalanced phase values;
5. phase-angle reference cases;
6. normal sample-counter wrap;
7. one dropped sample;
8. one duplicate sample;
9. one deliberately late or out-of-order sample;
10. publisher restart or configuration-revision transition;
11. good, questionable, invalid, test, substituted, blocked, and derived quality cases where the device supports them;
12. synchronized and unsynchronized operation;
13. matching and intentionally mismatching SCL.

## Expected machine-readable files

```text
evidence/<device-id>/
  source-register.md
  device-context.json
  expected-stream.json
  expected-values.json
  expected-quality.json
  expected-anomalies.json
  zero-input.pcap
  known-current.pcap
  known-voltage.pcap
  sanitized-system.scd
```

Private captures may remain outside the public repository. A synthetic public fixture must reproduce every behavior protected by automated tests.

## Value domains

Expected values must state their domain explicitly:

- raw protocol count;
- primary engineering value;
- secondary-equivalent value;
- injected test-set value.

No primary-to-secondary conversion is valid without an explicit positive ratio and provenance. The report must record the ratio source and reference.

## Quality evidence

Quality is recorded per channel with:

- raw bytes;
- selected 16-bit word placement;
- validity;
- detail flags;
- process or substituted source;
- test state;
- operator-blocked state;
- profile-specific derived extension where applicable;
- resulting severity and calculation usability.

Ambiguous high-word versus low-word encoding is reported as unknown instead of guessed.

## Acceptance criteria

A real-device pack passes when:

- payload decoding is byte exact;
- channel order agrees with the bound configuration or verified profile;
- scaling provenance is explicit;
- primary and secondary values agree with the stated context and combined equipment tolerance;
- quality flags produce the expected semantic state;
- normal counter wrap is not reported as an anomaly;
- injected gaps, duplicates, and out-of-order transitions are detected;
- zero input is not visually presented as a full-scale signal;
- report JSON and Markdown preserve all relevant provenance and diagnostics;
- clean traffic produces no false blocking health state.

Passing these criteria is engineering interoperability evidence, not an accredited conformance certificate.
