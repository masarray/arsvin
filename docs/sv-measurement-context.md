# SV measurement context

ArSubsv separates three different measurement concepts:

1. the raw protocol count,
2. the profile-scaled wire engineering value,
3. a primary or secondary-equivalent display value produced from explicit CT/VT context.

The Subscriber never derives a transformer ratio from vendor identity, signal amplitude, APPID, MAC address, or `svID` naming.

## User workflow

1. Select a discovered SV stream.
2. Open **CT/VT** from the responsive toolbar.
3. Declare whether the scaled value on the wire is interpreted as `PrimaryEngineering` or `SecondaryEquivalent`.
4. Choose the preferred display domain.
5. Enter current and/or voltage primary and secondary nominal values.
6. Record the evidence source and reference.
7. Apply the context.

Waveform, decoded engineering values, RMS, and phasors use the preferred domain only when the relevant ratio is valid. A missing current ratio does not prevent a verified voltage ratio from being used, and vice versa. Channels without sufficient context stay in their wire engineering domain.

## JSON evidence document

Use **Ctx Export** to save configured contexts. Use **Ctx Import** to restore them or apply them before the corresponding stream is discovered.

```json
{
  "schemaVersion": 1,
  "exportedAt": "2026-07-22T08:30:00Z",
  "streams": [
    {
      "streamKey": "logical-stream-key",
      "svId": "MU01",
      "wireDomain": "PrimaryEngineering",
      "displayDomain": "SecondaryEquivalent",
      "currentRatio": {
        "primaryNominal": 1000,
        "secondaryNominal": 1,
        "unit": "A",
        "source": "DeviceConfiguration",
        "reference": "SMU current input configuration"
      },
      "voltageRatio": {
        "primaryNominal": 20000,
        "secondaryNominal": 100,
        "unit": "V",
        "source": "DeviceConfiguration",
        "reference": "SMU voltage input configuration"
      },
      "notes": "Matched to the sanitized SCL and known-injection PCAP"
    }
  ]
}
```

## Evidence rules

- `Manual` means the values were entered by an engineer and should be checked against the test record.
- `Scl` means the ratio was obtained from an explicit, reviewable SCL source.
- `DeviceConfiguration` means the ratio was read from a device configuration or setting export.
- `DeviceValidated` means the context has been checked against real-device capture and known-injection evidence.
- Empty ratio fields are preferable to guessed values.
- Context conversion changes presentation; it does not alter the captured bytes or raw protocol values.

## Validation boundary

The context workflow makes primary/secondary interpretation explicit and repeatable. It does not turn a Windows PC into a calibrated test set, and it does not replace device-specific injection validation.
