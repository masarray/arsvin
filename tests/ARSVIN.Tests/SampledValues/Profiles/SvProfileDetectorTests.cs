using AR.Iec61850.SampledValues.Profiles;
using Xunit;

namespace ARSVIN.Tests.SampledValues.Profiles;

public sealed class SvProfileDetectorTests
{
    [Fact]
    public void ExactSyntheticDefinitionProducesConfirmedExplainableResult()
    {
        var detector = new SvProfileDetector();
        var result = detector.Evaluate(CreateMatchingFacts(), CreateSyntheticProfile());

        Assert.Equal(SvProfileConfidence.Confirmed, result.Confidence);
        Assert.Equal(100, result.ScorePercent);
        Assert.False(result.HasConflicts);
        Assert.Contains(result.Evidence, item =>
            item.Field == "Dataset signature" &&
            item.Outcome == SvProfileEvidenceOutcome.Match);
        Assert.Contains(result.Evidence, item =>
            item.Field == "Observed samples per second" &&
            item.Message.Contains("within", StringComparison.Ordinal));
    }

    [Fact]
    public void ConflictingWireFactsReturnConflictWithFieldEvidence()
    {
        var facts = CreateMatchingFacts() with
        {
            AsduPerFrame = 4,
            PayloadBytesPerAsdu = 80,
            ObservedSamplesPerSecond = 2000,
            ObservedCounterWrap = 2000
        };

        var result = new SvProfileDetector().Evaluate(facts, CreateSyntheticProfile());

        Assert.Equal(SvProfileConfidence.Conflict, result.Confidence);
        Assert.True(result.HasConflicts);
        Assert.Contains(result.Evidence, item =>
            item.Field == "Payload bytes per ASDU" &&
            item.Outcome == SvProfileEvidenceOutcome.Conflict);
        Assert.Contains(result.Evidence, item =>
            item.Field == "Sample-counter wrap" &&
            item.Observed == "2000");
    }

    [Fact]
    public void MissingObservedFactsRemainUnknownAndDoNotCreateFalseMatch()
    {
        var result = new SvProfileDetector().Evaluate(new SvObservedStreamFacts(), CreateSyntheticProfile());

        Assert.Equal(SvProfileConfidence.Unknown, result.Confidence);
        Assert.Equal(0, result.EvaluatedWeight);
        Assert.All(result.Evidence, item => Assert.Equal(SvProfileEvidenceOutcome.Unknown, item.Outcome));
    }

    [Fact]
    public void SparseGenericEvidenceCannotProduceFalseConfirmation()
    {
        var facts = new SvObservedStreamFacts
        {
            EtherType = 0x88BA,
            ObservationCount = 1
        };

        var result = new SvProfileDetector().Evaluate(facts, SvProfileCatalog.GenericSclLayer2);

        Assert.Equal(100, result.ScorePercent);
        Assert.Equal(5, result.EvaluatedWeight);
        Assert.Equal(SvProfileConfidence.Unknown, result.Confidence);
    }

    [Fact]
    public void MatchingStaticFieldsWithoutDatasetOrRateRemainOnlyPossible()
    {
        var facts = CreateMatchingFacts() with
        {
            ObservedSamplesPerSecond = null,
            DataSetSignature = Array.Empty<SvDatasetElementSignature>(),
            NominalFrequencyHz = null,
            ObservedCounterWrap = null
        };

        var result = new SvProfileDetector().Evaluate(facts, CreateSyntheticProfile());

        Assert.Equal(SvProfileConfidence.Possible, result.Confidence);
        Assert.DoesNotContain(result.Evidence, item =>
            item.Field == "Dataset signature" &&
            item.Outcome == SvProfileEvidenceOutcome.Match);
    }

    [Fact]
    public void BuiltInCatalogContainsOnlyGenericEvidenceBackedFallback()
    {
        var profile = Assert.Single(SvProfileCatalog.BuiltIn);

        Assert.Equal("generic-scl-layer2", profile.Id);
        Assert.Equal(SvProfileEvidenceStatus.ImplementedGeneric, profile.EvidenceStatus);
        Assert.Null(profile.ExpectedSamplesPerSecond);
        Assert.Null(profile.ExpectedSamplesPerCycle);
        Assert.Empty(profile.AllowedAsduPerFrame);
        Assert.Single(profile.Sources);
    }

    private static SvObservedStreamFacts CreateMatchingFacts()
        => new()
        {
            EtherType = 0x88BA,
            AppId = 0x4001,
            AsduPerFrame = 2,
            PayloadBytesPerAsdu = 8,
            ObservedSamplesPerSecond = 4000,
            ObservedCounterWrap = 4000,
            NominalFrequencyHz = 50,
            DataSetSignature =
            [
                new SvDatasetElementSignature { BType = "INT32", Cdc = "SAV" },
                new SvDatasetElementSignature { BType = "Quality", Cdc = "SAV", IsQuality = true }
            ],
            ObservationCount = 100
        };

    private static SvProfileDefinition CreateSyntheticProfile()
        => new()
        {
            Id = "synthetic-protection-profile",
            DisplayName = "Synthetic protection profile",
            Family = "Test fixture",
            SamplingBasis = SvSamplingBasis.SamplesPerSecond,
            ExpectedEtherType = 0x88BA,
            AllowedAsduPerFrame = [2],
            ExpectedPayloadBytesPerAsdu = 8,
            ExpectedDataSetElementCount = 2,
            ExpectedDataSetSignature =
            [
                new SvDatasetElementSignature { BType = "INT32", Cdc = "SAV" },
                new SvDatasetElementSignature { BType = "Quality", Cdc = "SAV", IsQuality = true }
            ],
            ExpectedSamplesPerSecond = 4000,
            AllowedNominalFrequenciesHz = [50],
            ExpectedCounterWrap = 4000,
            RateTolerancePercent = 0.5,
            EvidenceStatus = SvProfileEvidenceStatus.ResearchCandidate,
            Sources =
            [
                new SvProfileSourceEvidence(
                    "test-fixture",
                    "Synthetic values used only to verify detector behavior.",
                    SvProfileEvidenceStatus.ResearchCandidate)
            ]
        };
}
