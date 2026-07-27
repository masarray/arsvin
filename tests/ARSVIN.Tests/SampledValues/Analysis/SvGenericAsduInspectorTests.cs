using AR.Iec61850.SampledValues;
using AR.Iec61850.SampledValues.Analysis;
using Xunit;

namespace ARSVIN.Tests.SampledValues.Analysis;

public sealed class SvGenericAsduInspectorTests
{
    [Fact]
    public void InspectKeepsWireFieldsSeparateFromDatasetSemantics()
    {
        var asdu = new SampledValueAsdu
        {
            SvId = "MU01",
            DataSetReference = "LD0/LLN0$Dataset1",
            SampleCount = 123,
            ConfigurationRevision = 7,
            SampleSynchronization = 2,
            SampleRate = 4_800,
            SampleMode = 1,
            SamplePayload = new byte[12]
        };

        var inspection = SvGenericAsduInspector.Inspect(asdu);

        Assert.Equal("MU01", inspection.SvId);
        Assert.Equal((ushort)123, inspection.SampleCount);
        Assert.Equal((uint)7, inspection.ConfigurationRevision);
        Assert.Equal((ushort)4_800, inspection.SampleRate);
        Assert.Equal((ushort)1, inspection.SampleMode);
        Assert.Equal(3, inspection.Payload.CompleteWordCount);
        Assert.Contains("bind SCL", inspection.MappingState, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("smpRate", inspection.OptionalFieldSummary);
        Assert.Contains("smpMod", inspection.OptionalFieldSummary);
    }

    [Fact]
    public void InspectReportsUnresolvedMappingWhenDatasetReferenceIsAbsent()
    {
        var asdu = new SampledValueAsdu
        {
            SvId = "MU01",
            SamplePayload = new byte[8]
        };

        var inspection = SvGenericAsduInspector.Inspect(asdu);

        Assert.Contains("unresolved", inspection.MappingState, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("No optional ASDU fields observed", inspection.OptionalFieldSummary);
    }
}
