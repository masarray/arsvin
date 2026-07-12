using AR.Iec61850.SampledValues.Profiles;
using Xunit;

namespace ARSVIN.Tests.SampledValues.Profiles;

public sealed class SvObservationAccumulatorTests
{
    [Fact]
    public void BuildFactsCalculatesRatesAndCounterWrapWithoutTransportDependencies()
    {
        var accumulator = new SvObservationAccumulator();
        accumulator.Add(CreateObservation(0, [3998, 3999]));
        accumulator.Add(CreateObservation(1, [0, 1]));
        accumulator.Add(CreateObservation(2, [2, 3]));

        var facts = accumulator.BuildFacts();

        Assert.Equal(3, facts.ObservationCount);
        Assert.Equal((ushort)0x88BA, facts.EtherType);
        Assert.Equal((ushort)0x4001, facts.AppId);
        Assert.Equal(2, facts.AsduPerFrame);
        Assert.Equal(64, facts.PayloadBytesPerAsdu);
        Assert.Equal(1000, facts.ObservedFramesPerSecond!.Value, precision: 6);
        Assert.Equal(2000, facts.ObservedSamplesPerSecond!.Value, precision: 6);
        Assert.Equal(4000, facts.ObservedCounterWrap);
        Assert.Empty(facts.Diagnostics);
    }

    [Fact]
    public void BuildFactsMarksChangingConfigurationAsUnknownInsteadOfThrowing()
    {
        var accumulator = new SvObservationAccumulator();
        accumulator.Add(CreateObservation(0, [10], configurationRevision: 1));
        accumulator.Add(CreateObservation(1, [11], configurationRevision: 2));

        var facts = accumulator.BuildFacts();

        Assert.Null(facts.ConfigurationRevision);
        Assert.Contains(facts.Diagnostics, item => item.Contains("confRev changed", StringComparison.Ordinal));
        Assert.Equal(2, facts.ObservationCount);
    }

    [Fact]
    public void EmptyAccumulatorReturnsObservableUnknownFacts()
    {
        var facts = new SvObservationAccumulator().BuildFacts();

        Assert.Equal(0, facts.ObservationCount);
        Assert.Null(facts.AppId);
        Assert.Empty(facts.Diagnostics);
    }

    private static SvFrameObservation CreateObservation(
        int milliseconds,
        IReadOnlyList<ushort> sampleCounts,
        uint configurationRevision = 7)
        => new()
        {
            Timestamp = DateTimeOffset.UnixEpoch.AddMilliseconds(milliseconds),
            EtherType = 0x88BA,
            AppId = 0x4001,
            DestinationMac = "01:0C:CD:04:00:01",
            VlanId = 100,
            VlanPriority = 4,
            SvId = "MU01SV01",
            DataSetReference = "MU01MUnn/LLN0$PhsMeas",
            ConfigurationRevision = configurationRevision,
            PayloadBytesPerAsdu = 64,
            SampleCounts = sampleCounts,
            DeclaredSampleRate = 80,
            DeclaredSampleMode = 0,
            NominalFrequencyHz = 50,
            DataSetSignature =
            [
                new SvDatasetElementSignature { BType = "INT32", Cdc = "SAV" },
                new SvDatasetElementSignature { BType = "Quality", Cdc = "SAV", IsQuality = true }
            ]
        };
}
