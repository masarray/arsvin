# Buildfix: SV Quality Parameter Call Sites

This buildfix resolves CS7036 in `SvPublisherViewModel.cs` where two legacy call sites did not pass the new `SampledValueQuality quality` argument introduced by the publisher quality-bit runtime.

Fixed calls:

- `BuildInstantaneousChannelValue(..., currentDlsb, voltageDlsb, quality)`
- `BuildChannelValue(..., currentDlsb, voltageDlsb, quality)`

If your local build still reports CS7036 at these locations, make sure the full ZIP was extracted with overwrite enabled or manually apply the patch below.
