# Live Network Guardrails

Live mode transmits raw Ethernet Sampled Values traffic through the selected network adapter. This is an active network operation.

## Use live mode only when

- the network, connected equipment, and test are explicitly authorized;
- the topology is an isolated laboratory network, controlled process-bus test bench, or direct point-to-point link;
- the selected adapter has been independently verified;
- the expected receiver is prepared for the test traffic;
- APPID, VLAN, destination MAC, sample rate, payload layout, and `smpSynch` behavior have been reviewed; and
- a responsible test plan, observation method, stop condition, and recovery step are in place.

## Before live transmission

Verify:

- dry-run or offline generation completed as expected;
- Npcap is installed and working;
- required local permissions are available;
- destination multicast MAC, APPID, and VLAN are correct;
- dataset, sample rate, `nofASDU`, counter wrap, and payload mapping are understood;
- the chosen synchronization indication matches the intended test; and
- evidence recording and independent packet observation are ready.

Compatibility-oriented `smpSynch` modes may support laboratory behavior checks. They do not prove PTP accuracy, time traceability, merging-unit performance, or receiver acceptance.

## Evidence limit

Publisher evidence records what ARSVIN generated. A capture from the selected computer or an independent analyzer records what that observation point received. Neither proves that a protection IED consumed or acted on the stream.

## Preflight behavior

Warnings and readiness results are software guardrails. Fatal errors block transmission when ARSVIN cannot build or send the selected stream. Advisory results do not establish network authorization, isolation, functional safety, process safety, or protection-system suitability.

ARSVIN is not a certified protection commissioning authority, calibrated acceptance-test source, deterministic real-time platform, or formal IEC 61850 conformance tool.