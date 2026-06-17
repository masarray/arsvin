# Live Mode Safety

Live mode transmits raw Ethernet Sampled Values traffic on the selected network adapter.

## Use live mode only when

- you are on an isolated lab network, process-bus test bench, or direct point-to-point setup
- the selected adapter is the intended adapter
- the target relay / subscriber is expected to receive test traffic
- the APPID, VLAN, destination MAC, and `smpSynch` compatibility behavior have been reviewed

## Before starting live publish

Confirm the following:

- correct adapter selected
- Npcap installed and working
- application running with sufficient privileges
- correct destination multicast MAC
- correct APPID and VLAN
- expected sample rate
- chosen `smpSynch` mode is understood: compatibility modes help relay readability, but do not prove real PTP accuracy

## Important

ARSVIN is for lab publishing, relay readability checks, and traffic experiments.

It is not intended to be used as a certified protection commissioning authority or calibrated acceptance-test source.

## Preflight behavior

Warnings are advisory and do not block KM Looptest / point-to-point lab publishing. Fatal errors still block live publish when ARSVIN cannot build or send a valid stream.
