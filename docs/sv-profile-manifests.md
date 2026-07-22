# Declarative Sampled Values profile manifests

ARSVIN profile manifests are versioned JSON evidence documents that describe observable Sampled Values stream expectations without embedding vendor-specific branches in the decoder.

The first schema is:

```text
arsvin.sv-profile-manifest/v1
```

A manifest can describe:

- EtherType,
- allowed `noASDU` values,
- payload length per ASDU,
- ordered dataset signature,
- samples per cycle or samples per second,
- allowed nominal frequencies,
- expected sample-counter wrap,
- comparison tolerance,
- evidence maturity,
- reviewable evidence sources.

## Safety model

Loading a profile definition is not the same as proving conformance or interoperability.

Every load has an explicit trust level:

| Trust level | Maximum accepted evidence maturity |
|---|---|
| `UntrustedExternal` | `ResearchCandidate` |
| `ReviewedEngineering` | `ImplementedGeneric` |
| `TrustedRepository` | `VerifiedLab` |

A profile from an untrusted external JSON file cannot self-promote itself to `VerifiedStandard`, `VerifiedCapture`, or `VerifiedLab`. ARSVIN reduces the status and records a diagnostic. This also limits the maximum profile confidence through the existing evidence-maturity policy.

Built-in profile IDs cannot be replaced by an external manifest. Replacement is possible only when both conditions are explicit:

1. the load is `TrustedRepository`, and
2. `AllowBuiltInProfileReplacement` is enabled.

## Resource limits

The loader validates limits before adding profiles to a catalog:

- JSON size,
- profile count,
- evidence-source count,
- unique profile IDs,
- safe identifiers,
- sampling consistency,
- dataset count and ordered signature consistency,
- positive rates, frequencies, payload sizes, and counter wraps.

The default limits are deliberately conservative: 1 MiB, 64 profiles, and 16 evidence sources per profile.

## Deterministic catalog composition

`SvProfileCatalogComposer` combines built-in definitions and loaded manifests using case-insensitive stable IDs. The result is deterministic:

1. built-in profiles first,
2. external profiles sorted by ID.

A collision fails closed unless the trusted replacement policy is explicitly enabled.

## Detection behavior

Manifest profiles use the same `SvProfileDetector` as built-in profiles. There is no alternate vendor decoder. Detection remains based on observed evidence:

```text
observed wire facts
+ calculated capture facts
+ SCL-derived signature
+ trusted frequency context
→ weighted profile evidence
→ confidence limited by evidence maturity
```

Device identity, source MAC, product name, and vendor name are evidence metadata, not permission to assume payload layout or scaling.

## Example

A safe research template is provided at:

```text
samples/sv-evidence/profile-manifest-v1-example.json
```

It is intentionally marked `ResearchCandidate`. Replace the placeholder source with reviewed official documentation, sanitized SCL, byte-exact PCAP fixtures, or controlled laboratory evidence before using it for engineering classification.

## Current integration boundary

This phase provides the versioned parser, validation, trust controls, deterministic catalog composer, example manifest, and regression tests. Runtime import in ArSubsv and repository-managed 9-2LE or IEC 61869-9 fixture packs remain separate acceptance steps. This prevents an unreviewed JSON file from silently changing live decoding or measurement behavior.
