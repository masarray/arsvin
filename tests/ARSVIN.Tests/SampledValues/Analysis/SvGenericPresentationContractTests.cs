using System.Reflection;
using ARSVIN.Subscriber.Models;
using ARSVIN.Subscriber.ViewModels;
using Xunit;

namespace ARSVIN.Tests.SampledValues.Analysis;

public sealed class SvGenericPresentationContractTests
{
    [Fact]
    public void UnboundPresentationUsesGenericWordsAndSuppressesSemanticPlots()
    {
        var row = new SvStreamViewModel
        {
            Bound = "Auto fixed 4I+4V value-quality layout",
            WaveformState = "2 cycles locked"
        };

        var valuesField = typeof(SvStreamViewModel).GetField("_values", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(valuesField);
        var values = Assert.IsAssignableFrom<IList<DecodedValueRow>>(valuesField!.GetValue(row));
        values.Add(new DecodedValueRow
        {
            Index = 1,
            Signal = "TCTR1/AmpSv.instMag.i",
            Kind = "Current",
            Value = "1000",
            Raw = "000003E8",
            NumericValue = 1000
        });
        values.Add(new DecodedValueRow
        {
            Index = 2,
            Signal = "TCTR1/AmpSv.instMag.i.q",
            Kind = "Quality",
            Value = "00000000",
            Raw = "00000000"
        });

        Assert.Equal("Raw seqOfData", row.GenericMappingState);
        Assert.Contains("Unresolved", row.GenericSemanticState, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(2, row.GenericValues.Count);
        Assert.Equal("Word 1 (+0x00)", row.GenericValues[0].Signal);
        Assert.Equal("1000 / 1000", row.GenericValues[0].Value);
        Assert.DoesNotContain(row.GenericValues, value => value.IsQuality);
        Assert.Empty(row.GenericWaveformPoints);
        Assert.Empty(row.GenericPhasors);
    }

    [Fact]
    public void SclBoundPresentationKeepsResolvedRows()
    {
        var row = new SvStreamViewModel
        {
            Bound = "SCL: IED1/LLN0.SMV1"
        };

        var valuesField = typeof(SvStreamViewModel).GetField("_values", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(valuesField);
        var values = Assert.IsAssignableFrom<IList<DecodedValueRow>>(valuesField!.GetValue(row));
        values.Add(new DecodedValueRow
        {
            Index = 1,
            Signal = "TCTR1/AmpSv.instMag.i",
            Kind = "Current",
            Value = "1000",
            Raw = "000003E8",
            NumericValue = 1000
        });

        Assert.Equal("SCL dataset mapping", row.GenericMappingState);
        Assert.Equal("TCTR1/AmpSv.instMag.i", Assert.Single(row.GenericValues).Signal);
    }
}
