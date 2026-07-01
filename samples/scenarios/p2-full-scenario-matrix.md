# ARSVIN P2 Full Scenario Matrix

| Preset | Per-phase | Distortion | Purpose | Boundary |
|---|---:|---:|---|---|
| 3P fault | No | No | Balanced protection fault | Publisher scenario only |
| A-G fault | Yes | No | Single-phase-to-ground behavior | Not relay model |
| B-C fault | Yes | No | Phase-to-phase behavior | Not relay model |
| Negative sequence | Yes | No | Unbalance behavior | Approximation |
| Zero sequence | Yes | No | Residual/neutral publishing | Requires In/Vn dataset support |
| CT saturation | Yes | Yes | High-current saturation stress | Not calibrated CT model |
| VT fuse A | Yes | No | Single-phase voltage collapse | Approximation |
| 5th harmonic | No | Yes | Harmonic publisher stress | Approximation |
| DC offset | No | Yes | DC offset transient stress | Step approximation |
| Frequency steps | No | No | Subscriber frequency tracking | Step sequence |
| Phase jump | No | No | Angle jump behavior | Balanced sequence |
| Load reversal | No | No | Directional behavior | Balanced 180° shift |
