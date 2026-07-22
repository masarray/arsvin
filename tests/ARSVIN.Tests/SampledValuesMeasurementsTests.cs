using AR.Iec61850.SampledValues.Measurements;

namespace ARSVIN.Tests;

public sealed class SampledValuesMeasurementsTests
{
    [Fact]
    public void ScaleResolverConvertsFixedProtectionCurrentToAmperes()
    {
        var scale = SvEngineeringScaleResolver.Resolve(new SvEngineeringScaleEvidence
        {
            Channel = "Ia",
            Kind = "Current",
            IsFixedFourCurrentFourVoltageLayout = true,
            AnalogChannelCount = 8,
            PayloadBytesPerAsdu = 64,
            DeclaredSampleMode = 1,
            DeclaredSampleRate = 4_000
        });

        Assert.Equal(SvEngineeringScaleSource.Legacy92LeStyleStructuralInference, scale.Source);
        Assert.Equal(SvEngineeringScaleConfidence.Inferred, scale.Confidence);
        Assert.Equal("A", scale.Unit);
        Assert.Equal(1.0, scale.Apply(1_000), 9);
    }

    [Fact]
    public void ScaleResolverConvertsFixedProtectionVoltageToVolts()
    {
        var scale = SvEngineeringScaleResolver.Resolve(new SvEngineeringScaleEvidence
        {
            Channel = "TVTR1/VolSv.instMag.i",
            Kind = "Voltage",
            IsSclBound = true,
            IsFixedFourCurrentFourVoltageLayout = true,
            AnalogChannelCount = 8,
            PayloadBytesPerAsdu = 64
        });

        Assert.Equal(SvEngineeringScaleSource.SclBackedLegacy92LeStyle, scale.Source);
        Assert.Equal(SvEngineeringScaleConfidence.SclBacked, scale.Confidence);
        Assert.Equal("V", scale.Unit);
        Assert.Equal(100.0, scale.Apply(10_000), 9);
    }

    [Fact]
    public void ScaleResolverKeepsUnknownLayoutAsRawCounts()
    {
        var scale = SvEngineeringScaleResolver.Resolve(new SvEngineeringScaleEvidence
        {
            Channel = "Ia",
            Kind = "Current",
            AnalogChannelCount = 12,
            PayloadBytesPerAsdu = 96,
            ObservedSamplesPerSecond = 4_000
        });

        Assert.Equal(SvEngineeringScaleSource.RawOnly, scale.Source);
        Assert.Equal("count", scale.Unit);
        Assert.Equal(1_000, scale.Apply(1_000));
    }

    [Theory]
    [InlineData(4_000, 50)]
    [InlineData(4_800, 60)]
    public void TimebaseResolverDetectsLegacyProtectionFrequency(double samplesPerSecond, double expectedFrequency)
    {
        var resolution = SvTimebaseResolver.Resolve(new SvTimebaseEvidence
        {
            ObservedSamplesPerSecond = samplesPerSecond,
            IsFixedLegacyProtectionLayout = true
        });

        Assert.True(resolution.IsResolved);
        Assert.Equal(expectedFrequency, resolution.NominalFrequencyHz);
        Assert.Equal(80, resolution.SamplesPerCycle);
        Assert.Equal((ushort)samplesPerSecond, resolution.SampleCounterWrap);
    }

    [Fact]
    public void TimebaseResolverDoesNotAssumeFrequencyForUnknownProfile()
    {
        var resolution = SvTimebaseResolver.Resolve(new SvTimebaseEvidence
        {
            DeclaredSampleMode = 1,
            DeclaredSampleRate = 4_000,
            IsFixedLegacyProtectionLayout = false
        });

        Assert.Null(resolution.NominalFrequencyHz);
        Assert.Null(resolution.SamplesPerCycle);
        Assert.Equal((ushort)4_000, resolution.SampleCounterWrap);
    }

    [Fact]
    public void CounterTrackerAcceptsNormalProfileWrap()
    {
        var tracker = new SvSampleCounterTracker();
        tracker.Observe(3_998, 4_000);
        Assert.Equal(SvSampleCounterTransitionKind.Continuous, tracker.Observe(3_999, 4_000).Kind);

        var transition = tracker.Observe(0, 4_000);

        Assert.Equal(SvSampleCounterTransitionKind.NormalWrap, transition.Kind);
        Assert.False(transition.IsAnomaly);
    }

    [Fact]
    public void CounterTrackerDistinguishesGapFromOutOfOrder()
    {
        var gapTracker = new SvSampleCounterTracker();
        gapTracker.Observe(10, 4_000);
        var gap = gapTracker.Observe(13, 4_000);

        var orderTracker = new SvSampleCounterTracker();
        orderTracker.Observe(10, 4_000);
        orderTracker.Observe(11, 4_000);
        var outOfOrder = orderTracker.Observe(9, 4_000);

        Assert.Equal(SvSampleCounterTransitionKind.Gap, gap.Kind);
        Assert.Equal(2, gap.MissingSamples);
        Assert.Equal(SvSampleCounterTransitionKind.OutOfOrder, outOfOrder.Kind);
    }

    [Fact]
    public void CounterTrackerHonorsTrustedRestartHint()
    {
        var tracker = new SvSampleCounterTracker();
        tracker.Observe(2_500, 4_000);

        var transition = tracker.Observe(0, 4_000, restartHint: true);

        Assert.Equal(SvSampleCounterTransitionKind.Restart, transition.Kind);
        Assert.False(transition.IsAnomaly);
        Assert.Equal((ushort)1, tracker.Expected);
    }
}
