# GitHub Repository Settings

These settings are stored in GitHub rather than the source tree. Review them after merging licensing or public-positioning changes.

## Repository visibility

The repository is public. Keep public visibility only while the current GPL source, required notices, and corresponding release source remain available.

## About panel

Recommended description:

```text
GPL IEC 61850 Sampled Values Publisher and Subscriber for Windows — SCL setup, COMTRADE replay, live/PCAP analysis, waveform, phasor, RMS, timing health, and engineering evidence.
```

Website:

```text
https://masarray.github.io/arsvin/
```

Enable Releases and Issues. Enable Discussions only when maintainer capacity supports moderation and technical follow-up.

Recommended topics:

```text
iec61850
iec-61850
sampled-values
sampled-values-publisher
sampled-values-subscriber
sv-publisher
sv-subscriber
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

Avoid topics that imply certification, calibration, deterministic real-time performance, functional safety, cybersecurity approval, or affiliation with another organization.

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

Use **GitHub Actions** as the Pages source:

```text
.github/workflows/pages.yml
```

Expected URL:

```text
https://masarray.github.io/arsvin/
```

After deployment, verify:

- canonical URL, responsive layout, and accessible navigation;
- Open Graph and Twitter preview;
- GPL-3.0-or-later JSON-LD and visible licensing section;
- separate commercial-path wording that does not imply automatic extra rights;
- direct latest-release links;
- generated documentation and search index;
- `robots.txt` and `sitemap.xml`; and
- no stale active Apache-2.0 metadata.

## Releases

Stable asset names:

```text
ARSVIN-Publisher-win-x64.exe
ArSubsv-Subscriber-win-x64.exe
ARSVIN-Suite-Setup-win-x64.exe
ARSVIN-win-x64-portable.zip
ARSVIN-SBOM.cdx.json
SHA256SUMS.txt
```

Release notes should include:

- Publisher and Subscriber capability summary;
- installer and portable usage;
- Npcap requirement for authorized live workflows;
- unsigned-binary and SmartScreen note;
- operational and evidence boundaries;
- GPL community licensing and separate commercial path;
- known limitations; and
- documentation link.

Current packages must include GPL, NOTICE, commercial notice, copyright, trademark, third-party notices, and `docs/LICENSING.md`. They must not include a historical Apache license file as though it applied to current binaries.

## Recommended merge settings

- Enable squash merge.
- Automatically delete merged head branches.
- Require CI, CodeQL, current-license, Pages validation, and release validation where applicable.
- Require pull requests for changes to `main` when accepting external contributors.
- Require CLA affirmation and DCO sign-off for external contributions.