# Live Preflight Diagnostics

ARSVIN uses a **KM Looptest friendly** preflight model.

The preflight check is designed to help the engineer see obvious configuration mistakes without making simple point-to-point testing painful.

## Behavior

- **Warnings do not block live publish**
- **Fatal errors block live publish**
- **Arm Live is not required**
- The **Check** button is optional
- Live publish automatically runs the same check before sending traffic

This keeps ARSVIN useful for quick lab work, KM Looptest, and relay readability checks.

## Fatal errors

Fatal errors are problems that prevent ARSVIN from building or sending a valid stream, such as:

- no enabled publisher slot
- no selected NIC adapter for live publish
- no selected SV stream / dataset layout
- invalid source MAC
- invalid destination MAC
- invalid APPID
- invalid VLAN setting
- invalid sample rate
- invalid dLSB
- unsupported SV payload layout
- COMTRADE replay selected but no COMTRADE file is loaded

## Warnings

Warnings are allowed in lab / point-to-point use, including:

- VLAN disabled
- non-common multicast destination MAC
- duplicate APPID between publisher slots
- source MAC differs from adapter MAC
- application may not be running as Administrator
- global compatibility `smpSynch=2` selected
- lab PTP traffic enabled

Warnings are intentionally not blocking because ARSVIN is commonly used for quick subscriber readability checks.

## Recommended workflow for KM Looptest

1. Select the NIC adapter connected to the KM Looptest / relay point-to-point port.
2. Open SCL and select the SV stream expected by the subscriber.
3. Confirm APPID, destination MAC, VLAN, sample rate, and `smpSynch` mode.
4. Press **Check** if you want diagnostics.
5. Press **Start**.

If only warnings are shown, ARSVIN still allows live publish.
