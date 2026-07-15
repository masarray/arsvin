using AR.Iec61850.SampledValues.Profiles;
using AR.Iec61850.SampledValues.Reporting;
using Xunit;

namespace ARSVIN.Tests.SampledValues.Profiles;

public sealed class SvSubscriberEvidenceComparisonTests
{
    [Fact]
    public void SourceFailoverMatchesLogicalStreamAndSurfacesRegression()
    {
        var baseline = CreateReport(CreateStream("stream-a", "02:00:00:00:00:01", "GOOD", sequenceGaps: 0));
        var candidate = CreateReport(
            CreateStream("stream-b", "02:00:00:00:00:02", "BAD", sequenceGaps: 2),
            commit: "candidate123");

        var comparison = new SvSubscriberEvidenceComparator().Compare(
            baseline,
            candidate,
            DateTimeOffset.UnixEpoch.AddMinutes(5));

        var stream = Assert.Single(comparison.Streams);
        Assert.Equal(SvEvidenceChangeKind.Changed, stream.Kind);
        Assert.Equal(SvEvidenceChangeSeverity.Error, stream.Severity);
        Assert.Contains(stream.Changes, change => change.Field == "Source MAC");
        Assert.Contains(stream.Changes, change =>
            change.Field == "Health" && change.Severity == SvEvidenceChangeSeverity.Error);
        Assert.Contains(stream.Changes, change =>
            change.Field == "Sequence gaps" && change.Severity == SvEvidenceChangeSeverity.Warning);
        Assert.Equal(0, comparison.Summary.AddedStreamCount);
        Assert.Equal(0, comparison.Summary.RemovedStreamCount);
        Assert.True(comparison.Summary.HasRegressions);
    }

    [Fact]
    public void MissingAndNewLogicalStreamsAreClassifiedDeterministically()
    {
        var baseline = CreateReport(CreateStream("baseline", "02:00:00:00:00:01", "GOOD", svId: "MU01SV01"));
        var candidate = CreateReport(CreateStream("candidate", "02:00:00:00:00:02", "GOOD", svId: "MU02SV01"));

        var comparison = new SvSubscriberEvidenceComparator().Compare(
            baseline,
            candidate,
            DateTimeOffset.UnixEpoch.AddMinutes(5));

        Assert.Equal(2, comparison.Streams.Count);
        Assert.Equal(1, comparison.Summary.AddedStreamCount);
        Assert.Equal(1, comparison.Summary.RemovedStreamCount);
        Assert.Contains(comparison.Streams, stream =>
            stream.Kind == SvEvidenceChangeKind.Removed && stream.Severity == SvEvidenceChangeSeverity.Error);
        Assert.Contains(comparison.Streams, stream =>
            stream.Kind == SvEvidenceChangeKind.Added && stream.Severity == SvEvidenceChangeSeverity.Info);
    }

    [Fact]
    public void JsonRoundTripAndMarkdownPreserveRegressionEvidence()
    {
        var baseline = CreateReport(CreateStream("stream-a", "02:00:00:00:00:01", "GOOD", sequenceGaps: 0));
        var candidate = CreateReport(CreateStream("stream-a", "02:00:00:00:00:01", "WARN", sequenceGaps: 1));
        var comparison = new SvSubscriberEvidenceComparator().Compare(
            baseline,
            candidate,
            DateTimeOffset.UnixEpoch.AddMinutes(5));

        var json = SvSubscriberEvidenceComparisonSerializer.ToJson(comparison);
        var restored = SvSubscriberEvidenceComparisonSerializer.FromJson(json);
        var markdown = SvSubscriberEvidenceComparisonSerializer.ToMarkdown(restored);

        Assert.Contains("arsvin.sv-subscriber-evidence-comparison/v1", json, StringComparison.Ordinal);
        Assert.Contains("warning", json, StringComparison.Ordinal);
        Assert.Equal(comparison.Summary.WarningChangeCount, restored.Summary.WarningChangeCount);
        Assert.Contains("# ARSVIN Subscriber Evidence Comparison", markdown, StringComparison.Ordinal);
        Assert.Contains("REVIEW REQUIRED", markdown, StringComparison.Ordinal);
        Assert.Contains("Sequence gaps", markdown, StringComparison.Ordinal);
    }

    [Fact]
    public void RateMovementInsideToleranceDoesNotCreateFalseRegression()
    {
        var baselineStream = CreateStream("stream-a", "02:00:00:00:00:01", "GOOD", observedSamplesPerSecond: 4000);
        var candidateStream = CreateStream("stream-a", "02:00:00:00:00:01", "GOOD", observedSamplesPerSecond: 4020);
        var baseline = CreateReport(baselineStream);
        var candidate = CreateReport(candidateStream);

        var comparison = new SvSubscriberEvidenceComparator().Compare(
            baseline,
            candidate,
            DateTimeOffset.UnixEpoch.AddMinutes(5));

        var stream = Assert.Single(comparison.Streams);
        Assert.Equal(SvEvidenceChangeKind.Unchanged, stream.Kind);
        Assert.Empty(stream.Changes);
        Assert.False(comparison.Summary.HasRegressions);
    }

    private static SvSubscriberEvidenceReport CreateReport(
        SvSubscriberStreamEvidence stream,
        string commit = "baseline123")
        => new()
        {
            GeneratedAt = DateTimeOffset.UnixEpoch.AddMinutes(1),
            Software = new SvSubscriberSoftwareEvidence
            {
                Product = "ARSVIN Subscriber",
                Version = "0.4.0",
                InformationalVersion = $"0.4.0+{commit}",
                Commit = commit,
                Repository = "https://github.com/masarray/arsvin"
            },
            Capture = new SvSubscriberCaptureEvidence
            {
                Source = "LiveCapture",
                EndedAt = DateTimeOffset.UnixEpoch.AddMinutes(1),
                RawFrames = 8000,
                SvFrames = 8000
            },
            Summary = new SvSubscriberSummaryEvidence
            {
                Health = stream.Health,
                StreamCount = 1
            },
            Streams = [stream]
        };

    private static SvSubscriberStreamEvidence CreateStream(
        string key,
        string sourceMac,
        string health,
        int sequenceGaps = 0,
        string svId = "MU01SV01",
        double observedSamplesPerSecond = 4000)
    {
        var facts = new SvObservedStreamFacts
        {
            EtherType = 0x88BA,
            AppId = 0x4001,
            DestinationMac = "01:0C:CD:04:00:01",
            VlanId = 100,
            VlanPriority = 4,
            SvId = svId,
            DataSetReference = $"{svId}/LLN0$PhsMeas",
            ConfigurationRevision = 7,
            AsduPerFrame = 1,
            PayloadBytesPerAsdu = 8,
            ObservedFramesPerSecond = observedSamplesPerSecond,
            ObservedSamplesPerSecond = observedSamplesPerSecond,
            ObservedCounterWrap = 4000,
            DeclaredSampleRate = 80,
            DeclaredSampleMode = 0,
            Provenance = new Dictionary<string, SvFactSource>(StringComparer.Ordinal)
            {
                [nameof(SvObservedStreamFacts.AppId)] = SvFactSource.WireObserved,
                [nameof(SvObservedStreamFacts.ObservedSamplesPerSecond)] = SvFactSource.CaptureCalculated
            },
            ObservationCount = 8000,
            FirstTimestamp = DateTimeOffset.UnixEpoch,
            LastTimestamp = DateTimeOffset.UnixEpoch.AddSeconds(2)
        };

        return new SvSubscriberStreamEvidence
        {
            Key = key,
            Health = health,
            Identity = new SvSubscriberStreamIdentityEvidence
            {
                AppId = 0x4001,
                SourceMac = sourceMac,
                DestinationMac = facts.DestinationMac,
                VlanId = facts.VlanId,
                VlanPriority = facts.VlanPriority,
                SvId = facts.SvId,
                DataSetReference = facts.DataSetReference,
                ConfigurationRevision = facts.ConfigurationRevision,
                AsduPerFrame = 1,
                DeclaredSampleRate = facts.DeclaredSampleRate,
                DeclaredSampleMode = facts.DeclaredSampleMode
            },
            Runtime = new SvSubscriberRuntimeEvidence
            {
                FrameCount = 8000,
                AsduCount = 8000,
                ActualFramesPerSecond = observedSamplesPerSecond,
                SequenceGapCount = sequenceGaps,
                IsWaveformWindowReady = true
            },
            Observation = new SvSubscriberObservationEvidence
            {
                InputKinds = [SvObservationInputKind.LiveCapture],
                LastInputKind = SvObservationInputKind.LiveCapture,
                WindowFrames = 8000,
                WindowSamples = 8000,
                WindowDurationSeconds = 2,
                FirstTimestamp = facts.FirstTimestamp,
                LastTimestamp = facts.LastTimestamp,
                Facts = facts,
                FactProvenance = facts.Provenance
            }
        };
    }
}
