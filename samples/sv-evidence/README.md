# Sampled Values Evidence Intake

This directory defines the evidence package required to validate ARSVIN against real merging units, subscribers, SCL engineering files, and independent packet tools.

Do not commit confidential station files, credentials, customer names, internal IP plans, protection settings, or production captures without explicit authorization.

## Preferred evidence package

Create one folder per anonymized device/profile case:

```text
samples/sv-evidence/<case-id>/
├── README.md
├── source-register.md
├── stream.scd
├── capture.pcap
├── expected-profile.json
├── expected-values.json
└── SHA256SUMS.txt
```

Large, proprietary, or restricted captures should remain outside the public repository. Add a synthetic, non-sensitive fixture that reproduces the same protocol behavior and record the private evidence identifier in the case README.

Use `profile-definition-template.json` when preparing a research candidate. Unknown values must remain null or empty until verified.

## Required metadata

Record:

- evidence case ID,
- collection date,
- merging-unit manufacturer and model, or `withheld`,
- firmware version, or `withheld`,
- subscriber/test counterpart,
- SCL file type and edition markers,
- network topology,
- nominal frequency,
- observed frame rate,
- observed sample rate,
- ASDUs per frame,
- observed sample-counter wrap,
- payload bytes per ASDU,
- synchronization source and state,
- independent verification method,
- capture method,
- authorization and redaction status,
- expected profile claim,
- known deviations.

## Redaction rules

Before committing:

1. Replace station, bay, IED, and project names with neutral identifiers.
2. Remove credentials, certificates, private keys, passwords, and access tokens.
3. Remove nonessential IP and MAC addresses; preserve multicast structure only where required by the test.
4. Remove customer names, site locations, work-order references, and internal comments.
5. Confirm that the capture contains no unrelated production traffic.
6. Recalculate checksums after redaction.
7. Document every semantic change made during anonymization.

Anonymization must not silently change the behavior being tested.

## Evidence quality levels

| Level | Evidence |
|---|---|
| E0 | Engineering hypothesis only. |
| E1 | Synthetic frame or SCL fixture. |
| E2 | An independent packet decoder agrees with the synthetic fixture. |
| E3 | Anonymized real-device capture. |
| E4 | Real Publisher-to-Subscriber or MU-to-relay interoperability test. |
| E5 | Repeatable multi-implementation evidence. |

Public profile support requires licensed-source review plus at least E3 evidence. Lab-interoperable status requires E4.

## Independent verification

Where practical, record results from at least one independent path:

- an independent packet dissector,
- a separate engineering or measurement analyzer,
- a relay event or diagnostic log,
- a separate reference implementation,
- byte-level comparison against a reviewed fixture.

Independent tools are supporting evidence. They do not replace the applicable standard.

## Minimum test cases per profile

Include:

- clean expected traffic,
- missing frame/sample counter gap,
- duplicate frame,
- out-of-order frame,
- counter wrap,
- configuration revision mismatch,
- dataset mismatch,
- unexpected payload length,
- invalid/questionable/test/oldData quality where applicable,
- synchronization state changes,
- unknown/orphan stream,
- malformed frame handling.

## Capture timing guidance

For rate and jitter analysis, capture long enough to distinguish startup transients from steady-state behavior. Record the capture duration and clock source. Do not claim one-way network delay unless the measurement points share a validated time base and the method is documented.

## Expected-profile JSON example

```json
{
  "caseId": "synthetic-profile-001",
  "claimStatus": "research-candidate",
  "sourceEdition": "pending-licensed-review",
  "transport": "layer-2",
  "expected": {
    "appId": "0x4001",
    "asduPerFrame": null,
    "payloadBytesPerAsdu": null
  },
  "observed": {
    "sampleRate": null,
    "counterWrap": null
  },
  "notes": [
    "Values intentionally remain null until verified from authoritative evidence."
  ]
}
```

Do not fill unknown fields with remembered or assumed values.
