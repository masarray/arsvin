using AR.Iec61850.SampledValues;
using Xunit;

namespace ARSVIN.Tests.SampledValues;

public sealed class SampleCounterPolicyTests
{
    [Fact]
    public void IncrementWrapsAtConfiguredSamplesPerSecond()
    {
        Assert.Equal((ushort)0, SampleCounterPolicy.Increment(3999, wrap: 4000, step: 1));
        Assert.Equal((ushort)2, SampleCounterPolicy.Increment(3998, wrap: 4000, step: 4));
    }

    [Fact]
    public void SecondAlignedInitialCountUsesFractionalSecond()
    {
        var timestamp = new DateTimeOffset(2026, 6, 30, 1, 2, 3, TimeSpan.Zero).AddMilliseconds(250);

        var smpCnt = SampleCounterPolicy.InitialSampleCount(timestamp, sampleRateHz: 4000, wrap: 4000, SampleCounterMode.SecondAligned);

        Assert.Equal((ushort)1000, smpCnt);
    }
}
