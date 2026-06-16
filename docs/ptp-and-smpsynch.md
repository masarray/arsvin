# PTP and smpSynch Strategy

ARSVIN includes a lab PTP publisher and passive PTP monitor.

The tool can set Sampled Values `smpSynch` according to selected sync policy:

- `0`: not synchronized
- `1`: local synchronized
- `2`: global synchronized

The built-in PTP publisher is for lab traffic generation only. It is not a certified PTP grandmaster or calibrated timing reference.
