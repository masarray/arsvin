# Build Fixes


## v0.1.0.1 — PCAP Stream Namespace Build Fix

Fixed missing `System.IO` imports in:

- `src/ARSVIN/Engine/AR.Iec61850/Capture/PcapReader.cs`
- `src/ARSVIN/Engine/AR.Iec61850/Capture/PcapWriter.cs`

This resolves WPF temporary project build errors where `Stream` could not be resolved.

## v0.1.0.2 — Single-project global usings build fix

Some embedded engine files reference `File`, `Path`, `Directory`, `Stream`, `IOException`, `InvalidDataException`, and `FileNotFoundException`. In the standalone ARSVIN single-project distribution these imports must be available consistently during WPF temporary project compilation.

Fixed by adding `src/ARSVIN/GlobalUsings.cs` with shared system imports for the embedded engine and app source.

## v0.1.0.3 — WPF Vector ambiguity buildfix

Removed `global using System.Numerics;` from the single-project global imports. WPF controls use `System.Windows.Vector`, while numeric code keeps explicit `using System.Numerics;` where needed.
