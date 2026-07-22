using AR.Iec61850.SampledValues.Measurements;
using Xunit;

namespace ARSVIN.Tests;

public sealed class SampledValuesMeasurementContextTests
{
    [Fact]
    public void MeasurementContextRoundTripsWithExplicitRatios()
    {
        var document = new SvMeasurementContextDocument
        {
            ExportedAt = new DateTimeOffset(2026, 7, 22, 8, 30, 0, TimeSpan.Zero),
            Streams =
            [
                new SvStreamMeasurementContext
                {
                    StreamKey = "01-0C-CD-04-00-01|4000|MU01|DS01",
                    SvId = "MU01",
                    WireDomain = SvMeasurementValueDomain.PrimaryEngineering,
                    DisplayDomain = SvMeasurementValueDomain.SecondaryEquivalent,
                    CurrentRatio = new SvMeasurementRatio
                    {
                        PrimaryNominal = 1_000,
                        SecondaryNominal = 1,
                        Unit = "A",
                        Source = SvRatioSource.DeviceConfiguration,
                        Reference = "SMU current input configuration"
                    },
                    VoltageRatio = new SvMeasurementRatio
                    {
                        PrimaryNominal = 20_000,
                        SecondaryNominal = 100,
                        Unit = "V",
                        Source = SvRatioSource.DeviceConfiguration,
                        Reference = "SMU voltage input configuration"
                    },
                    Notes = "Known injection evidence"
                }
            ]
        };

        var json = SvMeasurementContextSerializer.ToJson(document);
        var restored = SvMeasurementContextSerializer.FromJson(json);

        var context = Assert.Single(restored.Streams);
        Assert.Equal("MU01", context.SvId);
        Assert.Equal(SvMeasurementValueDomain.SecondaryEquivalent, context.DisplayDomain);
        Assert.Equal(1_000, context.CurrentRatio!.PrimaryNominal);
        Assert.Equal(100, context.VoltageRatio!.SecondaryNominal);
        Assert.Contains("I 1000/1 A", context.Summary);
    }

    [Fact]
    public void MeasurementContextRejectsDuplicateStreamKeys()
    {
        var context = new SvStreamMeasurementContext
        {
            StreamKey = "same-key",
            SvId = "MU01"
        };
        var document = new SvMeasurementContextDocument
        {
            Streams = [context, context with { SvId = "MU02" }]
        };

        var error = Assert.Throws<InvalidDataException>(() =>
            SvMeasurementContextSerializer.ToJson(document));

        Assert.Contains("Duplicate measurement context", error.Message);
    }

    [Fact]
    public void MeasurementContextRejectsWrongRatioUnit()
    {
        var document = new SvMeasurementContextDocument
        {
            Streams =
            [
                new SvStreamMeasurementContext
                {
                    StreamKey = "stream",
                    CurrentRatio = new SvMeasurementRatio
                    {
                        PrimaryNominal = 1_000,
                        SecondaryNominal = 1,
                        Unit = "V",
                        Source = SvRatioSource.Manual
                    }
                }
            ]
        };

        var error = Assert.Throws<InvalidDataException>(() =>
            SvMeasurementContextSerializer.ToJson(document));

        Assert.Contains("current ratio unit must be A", error.Message);
    }

    [Fact]
    public void ContextResolvesCurrentAndVoltageRatiosByChannelEvidence()
    {
        var current = new SvMeasurementRatio
        {
            PrimaryNominal = 1_000,
            SecondaryNominal = 1,
            Unit = "A",
            Source = SvRatioSource.Manual
        };
        var voltage = new SvMeasurementRatio
        {
            PrimaryNominal = 20_000,
            SecondaryNominal = 100,
            Unit = "V",
            Source = SvRatioSource.Manual
        };
        var context = new SvStreamMeasurementContext
        {
            StreamKey = "stream",
            CurrentRatio = current,
            VoltageRatio = voltage
        };

        Assert.Same(current, context.ResolveRatio("TCTR1/AmpSv.instMag.i"));
        Assert.Same(voltage, context.ResolveRatio("TVTR1/VolSv.instMag.i"));
        Assert.Null(context.ResolveRatio("UnknownChannel"));
    }
}
