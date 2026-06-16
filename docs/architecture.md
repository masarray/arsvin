# Architecture

ARSVIN is separated from ARIEC61850 intentionally.

## ARIEC61850

Reusable stack:

- SCL parsing
- Sampled Values frame encoding
- Ethernet transport
- future PTP parser/monitor/time-sync model

## ARSVIN

Product layer:

- WPF test workspace
- Manual / Ramp / State Sequencer workflows
- stream configuration UI
- operator validation and safety UX
- release packaging
- documentation and landing page

## Dependency strategy

ARSVIN references ARIEC61850 through an external checkout at:

```text
extern/ARIEC61850
```
