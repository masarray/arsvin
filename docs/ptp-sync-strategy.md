# PTP Sync Strategy

Some relays do not accept Sampled Values unless the process-bus timing state is valid. ARSVIN must therefore become sync-aware, not just packet-aware.

## Decision

PTP implementation is split:

```text
ARIEC61850
  reusable PTP protocol, parser, serializer, monitor, and sync-state model

ARSVIN
  product UI, warnings, external GM visibility, smpSynch policy, relay compatibility workflow
```

## Import from PtpLabClock

The existing `PtpLabClock` work should be audited and extracted into clean modules:

```text
AR.Iec61850.TimeSync.Ptp
  PtpEthernetConstants
  PtpHeader
  PtpAnnounceMessage
  PtpSyncMessage
  PtpFollowUpMessage
  PtpPdelay*
  PtpParser
  PtpSerializer
  PtpClockIdentity
  PtpPortIdentity

AR.Iec61850.TimeSync.Runtime
  PassivePtpMonitor
  PtpHealthEvaluator
  PtpDomainProfileDetector
  PtpGrandmasterState
  PtpSyncState
```

## License gate

Before code is copied from `PtpLabClock`, confirm the license. If ARSVIN and ARIEC61850 are Apache-2.0, the imported PTP engine must also be Apache-2.0 or dual-licensed by the owner.

## smpSynch policy

```text
0 = no valid sync
1 = local/internal sync
2 = global/external PTP sync
```

ARSVIN should not silently claim global sync unless the PTP monitor has evidence from the same adapter and matching domain/profile.

## Lab PTP publisher

A software PTP publisher may be added as **Lab Mode** only. It should not be described as a certified grandmaster or relay-grade timing source.
