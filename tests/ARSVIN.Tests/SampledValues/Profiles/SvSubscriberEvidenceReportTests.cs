using AR.Iec61850.SampledValues.Profiles;
using AR.Iec61850.SampledValues.Reporting;
using Xunit;

namespace ARSVIN.Tests.SampledValues.Profiles;

public sealed class SvSubscriberEvidenceReportTests
{
    [Fact]
    public void JsonRoundTripPreservesEvidenceContract()
    {
        var report = CreateReport();

        var json = SvSubscriberEvidenceReportSerializer.ToJson(report);
        var restored = SvSubscriberEvidenceReportSerializer.FromJson(json);

        Assert.Contains("arsvin.sv-subscriber-evidence/v1", json, StringComparison.Ordinal);
        Assert.Contains("liveCapture", json, StringComparison.Ordinal);
        Assert.Contains("wireObserved", json, StringComparison.Ordinal);
        Assert.Contains("SV_CONFREV_MISMATCH", json, StringComparison.Ordinal);
        Assert.Equal("abc1234", restored.Software.Commit);
        var stream = Assert.Single(restored.Streams);
        Assert.Equal(SvObservationInputKind.LiveCapture, Assert.Single(stream.Observation.InputKinds));
        Assert.Equal(SvFactSource.WireObserved, stream.Observation.FactProvenance[nameof(SvObservedStreamFacts.AppId)]);
        Assert.Equal("1 warning", stream.Observation.ConfigurationComparison?.Summary);
        Assert.Equal(SvProfileConfidence.Likely, stream.Observation.ProfileDetection?.Confidence);
    }

    [Fact]
    public void MarkdownIncludesProfileConfigurationProvenanceAndBuildIdentity()
    {
        var markdown = SvSubscriberEvidenceReportSerializer.ToMarkdown(CreateReport());

        Assert.Contains("## Report metadata", markdown, StringComparison.Ordinal);
        Assert.Contains("abc1234", markdown, StringComparison.Ordinal);
        Assert.Contains("### Observed facts and provenance", markdown, StringComparison.Ordinal);
        Assert.Contains("WireObserved", markdown, StringComparison.Ordinal);
        Assert.Contains("### Expected SCL configuration", markdown, StringComparison.Ordinal);
        Assert.Contains("### Configuration comparison", markdown, StringComparison.Ordinal);
        Assert.Contains("SV_CONFREV_MISMATCH", markdown, StringComparison.Ordinal);
        Assert.Contains("### Profile detection evidence", markdown, StringComparison.Ordinal);
        Assert.Contains("Generic SCL-driven Layer-2 SV", markdown, StringComparison.Ordinal);
        Assert.Contains("### Phasors", markdown, StringComparison.Ordinal);
    }

    [Fact]
    public void ValidationRejectsDuplicateStreamKeys()
    {
        var report = CreateReport();
        var duplicate = report with
        {
            Summary = report.Summary with { StreamCount = 2 },
            Streams = [report.Streams[0], report.Streams[0]]
        };

        var exception = Assert.Throws<InvalidOperationException>(() =>
            SvSubscriberEvidenceReportSerializer.ToJson(duplicate));

        Assert.Contains("unique", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static SvSubscriberEvidenceReport CreateReport()
    {
        var facts = new SvObservedStreamFacts
        {
            EtherType = 0x88BA,
            AppId = 0x4001,
            DestinationMac = "01:0C:CD:04:00:01",
            VlanId = 100,
            VlanPriority = 4,
            SvId = "MU01SV01",
            DataSetReference = "MU01MUnn/LLN0$PhsMeas",
            ConfigurationRevision = 8,
            AsduPerFrame = 1,
            PayloadBytesPerAsdu = 8,
            ObservedFramesPerSecond = 4000,
            ObservedSamplesPerSecond = 4000,
            ObservedCounterWrap = 4000,
            CounterTransitions = new SvCounterTransitionSummary
            {
                SequentialCount = 7998,
                GapCount = 1,
                DuplicateCount = 0,
                OutOfOrderOrResetCount = 0,
                ConfirmedWrapCount = 1
            },
            DeclaredSampleRate = 80,
            DeclaredSampleMode = 0,
            DataSetSignature =
            [
                new SvDatasetElementSignature { BType = "INT32", Cdc = "SAV" },
                new SvDatasetElementSignature { BType = "Quality", Cdc = "SAV", IsQuality = true }
            ],
            Provenance = new Dictionary<string, SvFactSource>(StringComparer.Ordinal)
            {
                [nameof(SvObservedStreamFacts.AppId)] = SvFactSource.WireObserved,
                [nameof(SvObservedStreamFacts.ObservedFramesPerSecond)] = SvFactSource.CaptureCalculated,
                [nameof(SvObservedStreamFacts.DataSetSignature)] = SvFactSource.SclDerived
            },
            ObservationCount = 8000,
            FirstTimestamp = DateTimeOffset.UnixEpoch,
            LastTimestamp = DateTimeOffset.UnixEpoch.AddSeconds(2)
        };
        var expected = new SvExpectedStreamConfiguration
        {
            EtherType = 0x88BA,
            AppId = 0x4001,
            DestinationMac = "01:0C:CD:04:00:01",
            VlanId = 100,
            VlanPriority = 4,
            SvId = "MU01SV01",
            DataSetReference = "MU01MUnn/LLN0$PhsMeas",
            ConfigurationRevision = 7,
            AsduPerFrame = 1,
            PayloadBytesPerAsdu = 8,
            DeclaredSampleRate = 80,
            DeclaredSampleMode = 0,
            DataSetSignature = facts.DataSetSignature
        };
        var comparison = new SvConfigurationComparisonResult
        {
            Mode = SvComparisonMode.Compatible,
            Findings =
            [
                new SvConfigurationFinding(
                    SvConfigurationFindingSeverity.Warning,
                    "SV_CONFREV_MISMATCH",
                    "confRev",
                    "7",
                    "8",
                    "Configured confRev differs from observed traffic. Capture and decoding remain active.")
            ]
        };
        var profile = new SvProfileDetectionResult
        {
            Profile = new SvProfileDefinition
            {
                Id = "generic-scl-layer2",
                DisplayName = "Generic SCL-driven Layer-2 SV",
                Family = "Generic Layer-2 SV",
                SamplingBasis = SvSamplingBasis.Custom,
                ExpectedEtherType = 0x88BA,
                EvidenceStatus = SvProfileEvidenceStatus.ImplementedGeneric,
                Sources =
                [
                    new SvProfileSourceEvidence(
                        "arsvin-engine",
                        "Generic Layer-2 SV mechanisms implemented by the shared engine.",
                        SvProfileEvidenceStatus.ImplementedGeneric)
                ]
            },
            RawConfidence = SvProfileConfidence.Likely,
            ScorePercent = 100,
            MatchedWeight = 5,
            EvaluatedWeight = 5,
            Evidence =
            [
                new SvProfileMatchEvidence(
                    "EtherType",
                    SvProfileEvidenceOutcome.Match,
                    5,
                    "0x88BA",
                    "0x88BA",
                    "EtherType matches the generic Layer-2 SV transport.")
            ]
        };

        return new SvSubscriberEvidenceReport
        {
            GeneratedAt = DateTimeOffset.UnixEpoch.AddHours(1),
            Software = new SvSubscriberSoftwareEvidence
            {
                Product = "ArSubsv",
                Version = "0.4.0.0",
                InformationalVersion = "0.4.0+abc1234",
                Commit = "abc1234",
                Repository = "https://github.com/masarray/arsvin"
            },
            Capture = new SvSubscriberCaptureEvidence
            {
                Source = "LiveCapture",
                SclPath = "station.scd",
                Adapter = "Ethernet 1",
                Filter = "0x4001",
                StartedAt = DateTimeOffset.UnixEpoch,
                EndedAt = DateTimeOffset.UnixEpoch.AddSeconds(2),
                DurationSeconds = 2,
                RawFrames = 8000,
                SvFrames = 8000
            },
            Summary = new SvSubscriberSummaryEvidence
            {
                Health = "WARN",
                StreamCount = 1,
                RuntimeIssueCount = 1,
                ConfigurationFindingCount = 1
            },
            Streams =
            [
                new SvSubscriberStreamEvidence
                {
                    Key = "SV|4001|02:00:00:00:00:01|01:0C:CD:04:00:01|100|MU01SV01|MU01MUnn/LLN0$PhsMeas",
                    Health = "WARN",
                    HealthDetail = "Configuration differs from observed traffic.",
                    Identity = new SvSubscriberStreamIdentityEvidence
                    {
                        AppId = 0x4001,
                        SourceMac = "02:00:00:00:00:01",
                        DestinationMac = "01:0C:CD:04:00:01",
                        VlanId = 100,
                        VlanPriority = 4,
                        SvId = "MU01SV01",
                        DataSetReference = "MU01MUnn/LLN0$PhsMeas",
                        ConfigurationRevision = 8,
                        AsduPerFrame = 1,
                        LastSampleCount = 3999,
                        DeclaredSampleRate = 80,
                        DeclaredSampleMode = 0,
                        SampleSynchronization = 2
                    },
                    Runtime = new SvSubscriberRuntimeEvidence
                    {
                        FrameCount = 8000,
                        AsduCount = 8000,
                        ActualFramesPerSecond = 4000,
                        SequenceGapCount = 1,
                        IsWaveformWindowReady = true,
                        LayoutBinding = "SCL: MU01MUnn/LLN0$SV$MSVCB01",
                        QualitySummary = "Quality good 8,000, non-zero 0",
                        CursorSummary = "Cursor compare ready",
                        LastSeen = "00:00:02.000"
                    },
                    Observation = new SvSubscriberObservationEvidence
                    {
                        InputKinds = [SvObservationInputKind.LiveCapture],
                        LastInputKind = SvObservationInputKind.LiveCapture,
                        IsBoundToScl = true,
                        ControlBlockReference = "MU01MUnn/LLN0$SV$MSVCB01",
                        WindowFrames = 8000,
                        WindowSamples = 8000,
                        WindowDurationSeconds = 2,
                        FirstTimestamp = facts.FirstTimestamp,
                        LastTimestamp = facts.LastTimestamp,
                        Facts = facts,
                        FactProvenance = facts.Provenance,
                        ProfileDetection = profile,
                        ExpectedConfiguration = expected,
                        ConfigurationComparison = comparison,
                        Diagnostics = ["Observed 1 forward sample-counter gap transition(s)."]
                    },
                    Phasors =
                    [
                        new SvSubscriberPhasorEvidence
                        {
                            Channel = "Ia",
                            Kind = "Current",
                            Rms = 1000,
                            Peak = 1414.2,
                            AngleDegrees = 0
                        }
                    ],
                    Diagnostics = ["Configured confRev differs from observed traffic."]
                }
            ]
        };
    }
}
