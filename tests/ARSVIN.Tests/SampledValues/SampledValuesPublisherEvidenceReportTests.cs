using AR.Iec61850.SampledValues;
using Xunit;

namespace ARSVIN.Tests.SampledValues;

public sealed class SampledValuesPublisherEvidenceReportTests
{
    [Fact]
    public void ToMarkdown_Includes_Stream_Summary_And_Safety_Boundary()
    {
        var report = new SampledValuesPublisherEvidenceReport(
            ToolName: "ARSVIN",
            ToolVersion: "0.1.0-test",
            CreatedAt: new DateTimeOffset(2026, 7, 1, 9, 0, 0, TimeSpan.FromHours(7)),
            SclPath: "samples/scl/demo.scd",
            Adapter: "Ethernet 1",
            Mode: "Manual",
            TxTiming: "TX Timing: GOOD act=1600.0/1600.0fps",
            SafetyBoundary: "Lab publisher only.",
            Streams:
            [
                new SampledValuesEvidenceStream(
                    SlotName: "IED / MU 1",
                    IsEnabled: true,
                    ControlBlockReference: "MU01/LLN0$MSVCB01",
                    SvId: "MU01SV01",
                    DataSetReference: "MU01/LLN0$PhsMeas1",
                    AppId: "0x4000",
                    SourceMac: "02:00:00:00:20:01",
                    DestinationMac: "01:0C:CD:04:00:01",
                    Vlan: "VID=100/PCP=4",
                    SampleRateHz: 12800,
                    PublicationRateHz: 1600,
                    NoAsdu: 8,
                    PayloadBytesPerAsdu: 64,
                    EstimatedEthernetBytes: 720,
                    EstimatedBandwidthBitsPerSecond: 9216000,
                    SignalSource: "Manual phasor",
                    Quality: "good",
                    SyncMode: "GlobalCompatibility",
                    Status: "ready",
                    Findings:
                    [
                        new SampledValuesEvidenceFinding("INFO", "IED / MU 1", "Frame preview", "nofASDU=8")
                    ])
            ],
            GlobalFindings:
            [
                new SampledValuesEvidenceFinding("WARNING", "Privilege", "Application may not be running as Administrator", "Npcap may require elevation")
            ]);

        var markdown = SampledValuesPublisherEvidenceReportWriter.ToMarkdown(report);

        Assert.Contains("ARSVIN Sampled Values Publisher Evidence Report", markdown);
        Assert.Contains("MU01SV01", markdown);
        Assert.Contains("nofASDU", markdown);
        Assert.Contains("TX-side publisher evidence", markdown);
        Assert.Contains("Application may not be running as Administrator", markdown);
    }
}
