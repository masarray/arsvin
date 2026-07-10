# GitHub Repository Settings

These settings are stored in GitHub rather than the source tree. Apply them manually after the pull request is merged.

## Repository visibility

Set the repository to **Public**. The repository is already public as of July 11, 2026.

## About panel

Description:

```text
Apache-2.0 IEC 61850 Sampled Values workbench for Windows — SCL-driven SV publishing, COMTRADE replay, live/PCAP subscriber analysis, waveform, phasor, RMS, timing health, and evidence export.
```

Website:

```text
https://masarray.github.io/arsvin/
```

Enable:

- Releases
- Packages only when a package feed is introduced
- Issues
- Discussions only when there is enough maintainer capacity

Topics:

```text
iec61850
iec-61850
sampled-values
sampled-values-publisher
sampled-values-subscriber
sv-publisher
sv-subscriber
sv-injector
process-bus
digital-substation
substation-automation
comtrade
pcap
phasor
wpf
dotnet
windows
```

Avoid topics that imply certification, calibration, deterministic real-time performance, or affiliation with another vendor.

## Social preview

Upload:

```text
site/assets/arsvin-social-preview.png
```

Recommended preview message:

```text
ARSVIN — IEC 61850 Sampled Values Workbench
Publisher • Subscriber • SCL • COMTRADE • PCAP • Engineering Evidence
```

## Pages

Use **GitHub Actions** as the Pages source. The existing workflow is:

```text
.github/workflows/pages.yml
```

Expected public URL:

```text
https://masarray.github.io/arsvin/
```

After deployment, verify:

- canonical URL,
- responsive layout,
- Open Graph preview,
- direct latest-release links,
- `robots.txt`,
- `sitemap.xml`.

## Releases

The release workflow publishes stable asset names:

```text
ARSVIN-Publisher-win-x64.exe
ArSubsv-Subscriber-win-x64.exe
ARSVIN-Suite-Setup-win-x64.exe
ARSVIN-win-x64-portable.zip
SHA256SUMS.txt
```

Release notes should include:

- Publisher and Subscriber capability summary,
- installer and portable usage,
- Npcap requirement for live workflows,
- unsigned-binary/SmartScreen note,
- safety warning,
- known limitations,
- link to documentation.

## Recommended merge settings

- Enable squash merge.
- Automatically delete head branches after merge.
- Require the CI and CodeQL checks before merging into `main` when branch protection is enabled.
- Require pull requests for changes to `main` when the project begins accepting external contributors.
