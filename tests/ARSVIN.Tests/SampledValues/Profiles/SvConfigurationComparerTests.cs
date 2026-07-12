using AR.Iec61850.SampledValues.Profiles;
using Xunit;

namespace ARSVIN.Tests.SampledValues.Profiles;

public sealed class SvConfigurationComparerTests
{
    [Fact]
    public void StrictModeMarksConfigurationMismatchAsBlockingError()
    {
        var expected = CreateExpected();
        var observed = CreateObserved() with
        {
            AppId = 0x4002,
            AsduPerFrame = 4
        };

        var result = new SvConfigurationComparer().Compare(expected, observed, SvComparisonMode.Strict);

        Assert.True(result.HasBlockingErrors);
        Assert.Equal(2, result.ErrorCount);
        Assert.Contains(result.Findings, item => item.Code == "SV_APPID_MISMATCH");
        Assert.Contains(result.Findings, item => item.Code == "SV_ASDU_COUNT_MISMATCH");
        Assert.All(result.Findings, item => Assert.Contains("remain active", item.Message, StringComparison.Ordinal));
    }

    [Fact]
    public void CompatibleModeKeepsMismatchNonBlocking()
    {
        var observed = CreateObserved() with { DataSetReference = "OTHER/LLN0$Different" };

        var result = new SvConfigurationComparer().Compare(CreateExpected(), observed, SvComparisonMode.Compatible);

        Assert.False(result.HasBlockingErrors);
        Assert.Equal(1, result.WarningCount);
        Assert.Equal(SvConfigurationFindingSeverity.Warning, Assert.Single(result.Findings).Severity);
    }

    [Fact]
    public void EquivalentMacFormattingDoesNotCreateFalseMismatch()
    {
        var expected = CreateExpected() with { DestinationMac = "01-0C-CD-04-00-01" };

        var result = new SvConfigurationComparer().Compare(expected, CreateObserved(), SvComparisonMode.Strict);

        Assert.True(result.IsExactMatch);
    }

    [Fact]
    public void MissingDatasetSignatureIsReportedWithoutThrowing()
    {
        var observed = CreateObserved() with { DataSetSignature = Array.Empty<SvDatasetElementSignature>() };

        var result = new SvConfigurationComparer().Compare(CreateExpected(), observed, SvComparisonMode.Compatible);

        var finding = Assert.Single(result.Findings);
        Assert.Equal("SV_DATASET_SIGNATURE_MISSING", finding.Code);
        Assert.Equal(SvConfigurationFindingSeverity.Warning, finding.Severity);
    }

    private static SvExpectedStreamConfiguration CreateExpected()
        => new()
        {
            EtherType = 0x88BA,
            AppId = 0x4001,
            DestinationMac = "01:0C:CD:04:00:01",
            VlanId = 100,
            VlanPriority = 4,
            SvId = "MU01SV01",
            DataSetReference = "MU01MUnn/LLN0$PhsMeas",
            ConfigurationRevision = 7,
            AsduPerFrame = 2,
            PayloadBytesPerAsdu = 8,
            DeclaredSampleRate = 80,
            DeclaredSampleMode = 0,
            DataSetSignature =
            [
                new SvDatasetElementSignature { BType = "INT32", Cdc = "SAV" },
                new SvDatasetElementSignature { BType = "Quality", Cdc = "SAV", IsQuality = true }
            ]
        };

    private static SvObservedStreamFacts CreateObserved()
        => new()
        {
            EtherType = 0x88BA,
            AppId = 0x4001,
            DestinationMac = "01:0C:CD:04:00:01",
            VlanId = 100,
            VlanPriority = 4,
            SvId = "MU01SV01",
            DataSetReference = "MU01MUnn/LLN0$PhsMeas",
            ConfigurationRevision = 7,
            AsduPerFrame = 2,
            PayloadBytesPerAsdu = 8,
            DeclaredSampleRate = 80,
            DeclaredSampleMode = 0,
            DataSetSignature =
            [
                new SvDatasetElementSignature { BType = "INT32", Cdc = "SAV" },
                new SvDatasetElementSignature { BType = "Quality", Cdc = "SAV", IsQuality = true }
            ],
            ObservationCount = 100
        };
}
