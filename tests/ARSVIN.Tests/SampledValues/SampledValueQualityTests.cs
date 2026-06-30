using AR.Iec61850.SampledValues;
using Xunit;

namespace ARSVIN.Tests.SampledValues;

public sealed class SampledValueQualityTests
{
    [Fact]
    public void EncodesGoodQualityAsZero()
    {
        Assert.Equal(new byte[] { 0, 0, 0, 0 }, SampledValueQuality.Good.ToBytes());
    }

    [Fact]
    public void EncodesCommonSimulationBits()
    {
        var quality = new SampledValueQuality(SampledValueValidity.Questionable, OldData: true, Test: true, OperatorBlocked: true);
        var roundTrip = SampledValueQuality.FromUInt32(quality.ToUInt32());

        Assert.Equal(SampledValueValidity.Questionable, roundTrip.Validity);
        Assert.True(roundTrip.OldData);
        Assert.True(roundTrip.Test);
        Assert.True(roundTrip.OperatorBlocked);
    }
}
