# SEO and Public Launch Checklist

This checklist keeps ARSVIN discoverable and trustworthy as a GPL-3.0-or-later IEC 61850 Sampled Values engineering repository with a separate commercial licensing path.

## Target positioning

Primary phrases:

```text
IEC 61850 Sampled Values Publisher and Subscriber for Windows
IEC 61850 Sampled Values engineering workbench
```

Secondary phrases:

```text
IEC 61850 SV Publisher
Sampled Values Subscriber
SV stream analyzer
COMTRADE to Sampled Values
SCL-driven SV Publisher
PCAP Sampled Values analysis
process bus engineering software
Sampled Values waveform phasor RMS
```

Use phrases naturally. Do not use “injector,” “merging unit simulator,” “certified,” “conformant,” “real-time accurate,” or similar wording as an unqualified product claim when it could imply calibrated equipment, formal conformance, or deterministic execution.

## GitHub repository settings

Recommended repository description:

```text
GPL IEC 61850 Sampled Values Publisher and Subscriber for Windows — SCL setup, COMTRADE replay, live/PCAP analysis, waveform, phasor, RMS, timing health, and engineering evidence.
```

Website URL:

```text
https://masarray.github.io/arsvin/
```

Recommended topics:

```text
iec61850
iec-61850
sampled-values
sampled-values-publisher
sampled-values-subscriber
sv-publisher
process-bus
digital-substation
substation-automation
comtrade
pcap
phasor
waveform
wpf
dotnet
windows
gplv3
```

- Upload `site/assets/arsvin-social-preview.png` as the GitHub social preview image.
- Enable GitHub Pages using the reviewed Actions workflow.
- Use immutable Releases for downloadable artifacts.
- Link the latest stable release from README and landing page.

## README requirements

- Product title and opening paragraph state Publisher, Subscriber, Windows, and IEC 61850 Sampled Values.
- Download, website, quick start, documentation, and licensing links are visible near the top.
- Actual application images have descriptive alternative text.
- Operational boundaries are visible before live-network instructions.
- Capabilities include SCL, COMTRADE, `nofASDU`, timing health, PCAP, reports, waveform, phasor, RMS, and scenarios.
- Transmitter evidence, receiver evidence, and IED behavior are clearly separated.
- GPL-3.0-or-later is the only current community license shown.
- The commercial notice is described as a separate negotiated path, not an automatically granted alternative license.

## Landing page requirements

- Unique title and meta description include IEC 61850 Sampled Values and the actual Publisher/Subscriber scope.
- Exactly one H1 describes the engineering workflow without exaggerated claims.
- Open Graph and Twitter images use the project-owned social preview.
- `SoftwareApplication` JSON-LD declares `GPL-3.0-or-later`.
- FAQ structured data covers Npcap, evidence limits, formal conformance, and commercial licensing.
- Canonical URL, sitemap, robots metadata, manifest, and descriptive image alt text are present.
- Operational and licensing boundaries are visible without opening another page.

## Documentation SEO

- Every published guide has a unique title, description, canonical URL, and `TechArticle` structured data.
- Sitemap includes the product homepage and every generated documentation page.
- Documentation links use stable paths and no broken local references.
- Current docs do not present Apache-2.0 as an active license; historical references are clearly labelled and link to the archive boundary.
- External product names are not used as feature-target or affiliation keywords.

## Release SEO

Each public release should include:

- a concise user-facing summary;
- stable artifact names;
- upgrade and compatibility notes;
- known limitations and evidence boundaries;
- current GPL and separate commercial-path wording;
- operational reminder for authorized live Ethernet use; and
- a screenshot or product-site link.

## Trust evidence

Keep these visible:

- GPL-3.0-or-later license badge and full license text;
- commercial licensing notice;
- CI, CodeQL, Pages, release, SBOM, checksum, and attestation evidence;
- SECURITY, SUPPORT, CONTRIBUTING, CLA, DCO, copyright, trademark, and third-party notices;
- synthetic or authorized sample SCL, COMTRADE, PCAP, and report evidence; and
- explicit limitations concerning calibration, timing, formal conformance, cybersecurity, functional safety, and IED consumption.