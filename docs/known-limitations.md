# Known Limitations

ARSVIN is intentionally scoped as a lightweight Sampled Values publisher simulator and process-bus traffic tester.

Current boundaries include:

- Standard Windows scheduling can introduce timing variation
- The tool is not a calibrated current / voltage source
- The tool is not a certified protection test set
- The tool is not a certified PTP grandmaster
- `smpSynch` compatibility modes can help some relays subscribe, but they do not prove true time accuracy
- The application is designed for point-to-point and isolated lab use, not broad production network deployment
