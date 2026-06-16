# COMTRADE Replay

ARSVIN can import COMTRADE `.cfg` records and replay analog channels as IEC 61850 Sampled Values.

Supported DAT types:

- ASCII
- BINARY
- BINARY32
- FLOAT32

Supported replay scope:

- analog channels
- default channel mapping for Va, Vb, Vc, Vn, Ia, Ib, Ic, In
- one-shot or loop replay

Digital channel replay, CFF single-file support, and manual remapping are future work.
