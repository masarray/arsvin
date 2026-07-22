using AR.Iec61850.SampledValues.Measurements;
using Xunit;

namespace ARSVIN.Tests;

public sealed class SampledValuesQualityAndRatioTests
{
    [Fact]
    public void QualityDecoderRecognizesAllZeroQualityAsGood()
    {
        var state = SvQualityDecoder.DecodeNetworkBytes([0x00, 0x00, 0x00, 0x00]);

        Assert.Equal(SvQualityValidity.Good, state.Validity);
        Assert.Equal(SvQualitySeverity.Good, state.Severity);
        Assert.True(state.IsStrictlyUsable);
        Assert.Equal("Good", state.Summary);
    }

    [Fact]
    public void QualityDecoderRecognizesInvalidFailureAsBad()
    {
        // Validity bits 10b = Invalid, plus Failure bit 6.
        var state = SvQualityDecoder.DecodeWord(0x0042);

        Assert.Equal(SvQualityValidity.Invalid, state.Validity);
        Assert.True(state.Failure);
        Assert.Equal(SvQualitySeverity.Bad, state.Severity);
        Assert.False(state.IsStrictlyUsable);
        Assert.Contains("failure", state.ActiveFlags);
    }

    [Fact]
    public void QualityDecoderTreatsLegacyDerivedAsInformationNotBad()
    {
        var state = SvQualityDecoder.DecodeWord(0x2000, allowLegacyDerived: true);

        Assert.True(state.Derived);
        Assert.Equal(SvQualityValidity.Good, state.Validity);
        Assert.Equal(SvQualitySeverity.Information, state.Severity);
        Assert.True(state.IsStrictlyUsable);
    }

    [Fact]
    public void QualityDecoderRecognizesQuestionableInaccurate()
    {
        var state = SvQualityDecoder.DecodeWord(0x0203);

        Assert.Equal(SvQualityValidity.Questionable, state.Validity);
        Assert.True(state.Inaccurate);
        Assert.Equal(SvQualitySeverity.Warning, state.Severity);
        Assert.Contains("inaccurate", state.Summary);
    }

    [Fact]
    public void QualityDecoderHandlesHighAndLowWordWirePlacements()
    {
        var high = SvQualityDecoder.DecodeNetworkBytes([0x00, 0x40, 0x00, 0x00]);
        var low = SvQualityDecoder.DecodeNetworkBytes([0x00, 0x00, 0x00, 0x40]);

        Assert.Equal(SvQualityWordPlacement.HighWord, high.Placement);
        Assert.Equal(SvQualityWordPlacement.LowWord, low.Placement);
        Assert.True(high.Failure);
        Assert.True(low.Failure);
    }

    [Fact]
    public void QualityDecoderDoesNotGuessConflictingWordPlacement()
    {
        var state = SvQualityDecoder.DecodeNetworkBytes([0x00, 0x40, 0x00, 0x80]);

        Assert.True(state.IsEncodingAmbiguous);
        Assert.Equal(SvQualitySeverity.Unknown, state.Severity);
        Assert.Equal(SvQualityWordPlacement.Ambiguous, state.Placement);
    }

    [Fact]
    public void PrimaryEngineeringValueCanBeConvertedToSecondaryEquivalent()
    {
        var ratio = new SvMeasurementRatio
        {
            PrimaryNominal = 1_000,
            SecondaryNominal = 1,
            Unit = "A",
            Source = SvRatioSource.DeviceConfiguration,
            Reference = "SMU current input configuration"
        };

        var value = SvMeasurementDomainResolver.Resolve(
            800,
            "A",
            SvMeasurementValueDomain.PrimaryEngineering,
            ratio);

        Assert.True(value.PrimaryValue.HasValue);
        Assert.True(value.SecondaryEquivalentValue.HasValue);
        Assert.Equal(800.0, value.PrimaryValue.Value, 9);
        Assert.Equal(0.8, value.SecondaryEquivalentValue.Value, 9);
        Assert.Equal(SvRatioSource.DeviceConfiguration, value.RatioSource);
    }

    [Fact]
    public void MissingRatioDoesNotInventSecondaryEquivalent()
    {
        var value = SvMeasurementDomainResolver.Resolve(
            800,
            "A",
            SvMeasurementValueDomain.PrimaryEngineering,
            ratio: null);

        Assert.True(value.PrimaryValue.HasValue);
        Assert.Equal(800.0, value.PrimaryValue.Value, 9);
        Assert.Null(value.SecondaryEquivalentValue);
        Assert.Contains("no verified CT/VT ratio", value.Diagnostic);
    }

    [Fact]
    public void InvalidRatioIsRejectedWhenConversionIsRequested()
    {
        var ratio = new SvMeasurementRatio
        {
            PrimaryNominal = 0,
            SecondaryNominal = 1,
            Unit = "A"
        };

        Assert.False(ratio.IsValid);
        Assert.Throws<InvalidOperationException>(() => ratio.PrimaryToSecondary(1));
    }
}
