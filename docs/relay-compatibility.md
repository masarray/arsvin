# Relay Compatibility Notes

Relay acceptance of Sampled Values depends on more than packet format.

Common causes of rejection:

- wrong destination MAC,
- wrong APPID,
- wrong VLAN or priority,
- `svID` mismatch,
- dataset mismatch,
- `confRev` mismatch,
- sample rate mismatch,
- smpCnt behavior not expected,
- smpSynch is unsynchronized,
- PTP domain/profile mismatch,
- relay subscription points to a different stream.
