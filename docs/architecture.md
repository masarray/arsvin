# Architecture

ARSVIN currently ships as a single WPF desktop project with a clear internal separation between UI, view models, protocol primitives, publishing sessions, and documentation samples.

## Main layers

```text
src/ARSVIN/
  App.xaml / MainWindow.xaml        WPF shell and visual resources
  ViewModels/                       MVVM state, commands, user workflow orchestration
  Models/                           UI-facing model types
  Controls/                         Custom plots and visual controls
  Converters/                       WPF converters
  Engine/AR.Iec61850/               Pure IEC 61850, Ethernet, SCL, MMS, GOOSE, SV, PTP logic
  Engine/AR.Iec61850.Transports.*   Packet transport adapters such as Npcap
```

## Workflow overview

1. User opens or enters stream configuration.
2. View model validates and normalizes values.
3. SCL and COMTRADE helpers provide stream metadata and sample data.
4. Publisher profiles are built from the selected slot.
5. Dry run mode validates frame generation without live transmission.
6. Live mode uses the Npcap transport to transmit raw Ethernet frames.
7. Diagnostics and status messages are surfaced back to the WPF UI.

## Current technical debt

The engine code is already placed under an internal `Engine/AR.Iec61850` tree, but it is still compiled inside the WPF app project. For long-term public maintainability, the best next refactor is:

```text
src/AR.Iec61850/                  Pure protocol class library
src/ARSVIN/                       WPF app shell
src/ARSVIN.Transports.Npcap/      Optional live transport adapter
tests/AR.Iec61850.Tests/          Protocol unit tests
tests/ARSVIN.Tests/               App smoke/unit tests
```

This should be done gradually to avoid breaking a working engineering tool.

## Safety model

ARSVIN must keep live traffic behavior explicit. Any new feature that transmits packets should:

- Be disabled by default unless clearly user-initiated
- Be visible in UI status text
- Respect dry run behavior
- Be documented in safety notes
- Avoid hidden background network activity
