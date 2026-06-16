# Process Bus Lab Setup

Recommended isolated topology:

```text
ARSVIN PC
  |
  | dedicated NIC
  |
test switch / isolated process-bus VLAN
  |---------------- relay / subscriber
  |---------------- optional PTP grandmaster
  |---------------- Wireshark capture port
```

## Checklist

- Use a dedicated adapter.
- Confirm destination MAC and APPID.
- Confirm VLAN and priority.
- Confirm sample rate and dataset mapping.
- Confirm PTP domain/profile when relay requires sync.
