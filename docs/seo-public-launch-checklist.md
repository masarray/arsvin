# SEO and Public Launch Checklist

This checklist keeps ARSVIN discoverable and trustworthy as a public Apache-2.0 engineering repository.

## Target positioning

Primary phrase:

```text
IEC 61850 Sampled Values Publisher for Windows
```

Secondary phrases:

```text
IEC 61850 SV Publisher
Sampled Values Publisher
SV Injector
Merging Unit Simulator
IEC 61850 Process Bus Tool
COMTRADE to Sampled Values
SCL-driven SV Publisher
```

Use these phrases naturally in the README, landing page, release notes, and documentation. Avoid claiming certified conformance unless formal testing has been completed.

## GitHub repository settings

- Set repository description to:

```text
Apache-2.0 IEC 61850 Sampled Values Publisher for Windows — SCL-driven SV publishing, COMTRADE replay, nofASDU support, TX timing health, scenario presets, and PCAP evidence export.
```

- Set website URL to:

```text
https://masarray.github.io/arsvin/
```

- Add topics:

```text
iec61850
iec-61850
sampled-values
sampled-values-publisher
sv-publisher
sv-injector
merging-unit
merging-unit-simulator
process-bus
digital-substation
substation-automation
comtrade
ptp
wpf
dotnet
windows
```

- Upload `site/assets/arsvin-social-preview.png` as the GitHub social preview image.
- Enable GitHub Pages using the existing Pages workflow.
- Use Releases for portable downloads.
- Pin or link the latest stable release in the README and landing page.

## README requirements

- Product title contains the primary phrase.
- First paragraph explains what ARSVIN is, who it is for, and the main workflow.
- A preview image appears near the top.
- Download, landing page, quick start, and docs links are visible above the fold.
- Safety boundaries are visible before the quick start.
- Feature matrix includes `nofASDU`, SCL, COMTRADE, TX Timing Health, PCAP, report, and scenarios.
- “What ARSVIN is not” is explicit and honest.

## Landing page requirements

- Title: `ARSVIN — IEC 61850 Sampled Values Publisher for Windows`.
- H1 uses the primary phrase.
- Meta description stays specific and non-generic.
- Open Graph image points to `arsvin-social-preview.png`.
- FAQ section answers common search questions.
- SoftwareApplication and FAQ structured data are present.
- Safety boundaries are visible without needing to open the README.

## Image SEO

Use descriptive file names and alt text:

```text
arsvin-iec-61850-sampled-values-publisher-preview.png
arsvin-social-preview.png
```

Alt text should describe the product and the visible workflow, for example:

```text
ARSVIN IEC 61850 Sampled Values Publisher preview with SCL setup, TX Timing Health, nofASDU, COMTRADE replay, PCAP evidence, and scenario presets
```

## Release SEO

Each public release should include:

- short summary of user-facing changes,
- portable ZIP asset name,
- upgrade notes,
- known limitations,
- safety reminder,
- screenshot or link to the landing page.

## Trust evidence

Keep these visible:

- Apache-2.0 license,
- CI badge,
- CodeQL badge,
- SECURITY.md,
- CONTRIBUTING.md,
- sample SCL and COMTRADE files,
- sample evidence report,
- generated PCAP workflow.
