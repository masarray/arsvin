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
        var profile = new SvProfileDefinition
        {
            Id = "invalid-asdu",
            DisplayName = "Invalid ASDU",
            AllowedAsduPerFrame = [value]
        };

        Assert.Throws<InvalidOperationException>(profile.Validate);
    }

    [Fact]
    public void InvalidRateToleranceIsRejected()
    {
        var profile = new SvProfileDefinition
        {
            Id = "invalid-tolerance",
            DisplayName = "Invalid tolerance",
            RateTolerancePercent = 101
        };

        Assert.Throws<InvalidOperationException>(profile.Validate);
    }
}
