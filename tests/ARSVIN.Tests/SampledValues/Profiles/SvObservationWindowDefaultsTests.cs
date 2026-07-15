using AR.Iec61850.SampledValues.Profiles;
using Xunit;

namespace ARSVIN.Tests.SampledValues.Profiles;

public sealed class SvObservationWindowDefaultsTests
{
    [Fact]
    public void DefaultWindowSupportsTwoSecondsOfHighRateSampledValues()
    {
        Assert.Equal(TimeSpan.FromSeconds(2), SvStreamObservationManager.DefaultMaximumAge);
        Assert.True(SvStreamObservationManager.DefaultMaximumObservations >= 9_600);
    }
}
