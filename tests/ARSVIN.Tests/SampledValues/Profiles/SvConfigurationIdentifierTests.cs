using AR.Iec61850.SampledValues.Profiles;
using Xunit;

namespace ARSVIN.Tests.SampledValues.Profiles;

public sealed class SvConfigurationIdentifierTests
{
    [Fact]
    public void SvIdPunctuationIsSemanticallySignificant()
    {
        var expected = new SvExpectedStreamConfiguration { SvId = "MU-01" };
        var observed = new SvObservedStreamFacts { SvId = "MU01" };

        var result = new SvConfigurationComparer().Compare(expected, observed, SvComparisonMode.Strict);

        var finding = Assert.Single(result.Findings);
        Assert.Equal("SV_ID_MISMATCH", finding.Code);
        Assert.Equal(SvConfigurationFindingSeverity.Error, finding.Severity);
    }

    [Fact]
    public void DatasetReferencePunctuationIsNotNormalizedAsMacAddress()
    {
        var expected = new SvExpectedStreamConfiguration
        {
            DataSetReference = "MU01MUnn/LLN0$Phs-Meas"
        };
        var observed = new SvObservedStreamFacts
        {
            DataSetReference = "MU01MUnn/LLN0$PhsMeas"
        };

        var result = new SvConfigurationComparer().Compare(expected, observed, SvComparisonMode.Compatible);

        var finding = Assert.Single(result.Findings);
        Assert.Equal("SV_DATASET_MISMATCH", finding.Code);
        Assert.Equal(SvConfigurationFindingSeverity.Warning, finding.Severity);
    }

    [Fact]
    public void IdentifierComparisonTrimsOuterWhitespaceOnly()
    {
        var expected = new SvExpectedStreamConfiguration
        {
            SvId = " MU01SV01 ",
            DataSetReference = " MU01MUnn/LLN0$PhsMeas "
        };
        var observed = new SvObservedStreamFacts
        {
            SvId = "MU01SV01",
            DataSetReference = "MU01MUnn/LLN0$PhsMeas"
        };

        var result = new SvConfigurationComparer().Compare(expected, observed, SvComparisonMode.Strict);

        Assert.True(result.IsExactMatch);
    }
}
