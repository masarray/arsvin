using AR.Iec61850.SampledValues.Profiles;
using Xunit;

namespace ARSVIN.Tests.SampledValues.Profiles;

public sealed class SvProfileDefinitionTests
{
    [Fact]
    public void ValidGenericDefinitionPassesValidation()
    {
        SvProfileCatalog.GenericSclLayer2.Validate();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void NonPositiveAsduCountIsRejected(int value)
    {
        var profile = CreateValidDefinition() with
        {
            AllowedAsduPerFrame = [value]
        };

        Assert.Throws<InvalidOperationException>(profile.Validate);
    }

    [Fact]
    public void InvalidRateToleranceIsRejected()
    {
        var profile = CreateValidDefinition() with
        {
            RateTolerancePercent = 101
        };

        Assert.Throws<InvalidOperationException>(profile.Validate);
    }

    [Fact]
    public void EvidenceSourceIsRequired()
    {
        var profile = CreateValidDefinition() with
        {
            Sources = Array.Empty<SvProfileSourceEvidence>()
        };

        Assert.Throws<InvalidOperationException>(profile.Validate);
    }

    [Fact]
    public void SamplingBasisRequiresMatchingExpectation()
    {
        var profile = CreateValidDefinition() with
        {
            SamplingBasis = SvSamplingBasis.SamplesPerCycle,
            ExpectedSamplesPerSecond = null,
            ExpectedSamplesPerCycle = null
        };

        Assert.Throws<InvalidOperationException>(profile.Validate);
    }

    [Fact]
    public void DatasetCountMustMatchOrderedSignature()
    {
        var profile = CreateValidDefinition() with
        {
            ExpectedDataSetElementCount = 2,
            ExpectedDataSetSignature =
            [
                new SvDatasetElementSignature { BType = "INT32" }
            ]
        };

        Assert.Throws<InvalidOperationException>(profile.Validate);
    }

    private static SvProfileDefinition CreateValidDefinition()
        => new()
        {
            Id = "valid-test-profile",
            DisplayName = "Valid test profile",
            Family = "Test",
            SamplingBasis = SvSamplingBasis.SamplesPerSecond,
            ExpectedSamplesPerSecond = 4000,
            AllowedAsduPerFrame = [1],
            Sources =
            [
                new SvProfileSourceEvidence(
                    "test-source",
                    "Synthetic source metadata for deterministic validation tests.",
                    SvProfileEvidenceStatus.ResearchCandidate)
            ]
        };
}
