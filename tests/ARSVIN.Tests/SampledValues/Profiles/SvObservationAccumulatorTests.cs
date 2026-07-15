using AR.Iec61850.SampledValues.Profiles;
using Xunit;

namespace ARSVIN.Tests.SampledValues.Profiles;

public sealed class SvObservationAccumulatorTests
{
    [Fact]
    public void BuildFactsCalculatesRatesAndConfirmedCounterWrapWithoutTransportDependencies()
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
        Assert.Equal(1, facts.CounterTransitions.ConfirmedWrapCount);
        Assert.Equal(SvFactSource.CaptureCalculated, facts.Provenance[nameof(SvObservedStreamFacts.ObservedCounterWrap)]);
        Assert.Empty(facts.Diagnostics);
    }

    [Fact]
    public void OutOfOrderCounterIsNotMisclassifiedAsWrap()
    {
        var accumulator = new SvObservationAccumulator();
        accumulator.Add(CreateObservation(0, [100, 101]));
        accumulator.Add(CreateObservation(1, [99, 102]));

        var facts = accumulator.BuildFacts();

        Assert.Null(facts.ObservedCounterWrap);
        Assert.Equal(1, facts.CounterTransitions.OutOfOrderOrResetCount);
        Assert.Contains(facts.Diagnostics, item => item.Contains("not classified as wraps", StringComparison.Ordinal));
    }

    [Fact]
    public void DuplicateAndGapTransitionsAreReportedSeparately()
    {
        var accumulator = new SvObservationAccumulator();
        accumulator.Add(CreateObservation(0, [10, 10]));
        accumulator.Add(CreateObservation(1, [13, 14]));

        var facts = accumulator.BuildFacts();

        Assert.Equal(1, facts.CounterTransitions.DuplicateCount);
        Assert.Equal(1, facts.CounterTransitions.GapCount);
        Assert.Contains(facts.Diagnostics, item => item.Contains("duplicate", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(facts.Diagnostics, item => item.Contains("gap", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void AccumulatorKeepsOnlyConfiguredMaximumObservationCount()
    {
        var accumulator = new SvObservationAccumulator(maximumObservations: 3, maximumAge: TimeSpan.FromMinutes(1));

        for (var index = 0; index < 6; index++)
            accumulator.Add(CreateObservation(index, [(ushort)index]));

        var facts = accumulator.BuildFacts();

        Assert.Equal(3, accumulator.Count);
        Assert.Equal(3, facts.ObservationCount);
        Assert.Equal(DateTimeOffset.UnixEpoch.AddMilliseconds(3), facts.FirstTimestamp);
    }

    [Fact]
    public void AccumulatorDropsObservationsOutsideMaximumAge()
    {
        var accumulator = new SvObservationAccumulator(maximumObservations: 100, maximumAge: TimeSpan.FromMilliseconds(2));
        accumulator.Add(CreateObservation(0, [0]));
        accumulator.Add(CreateObservation(1, [1]));
        accumulator.Add(CreateObservation(4, [4]));

        var facts = accumulator.BuildFacts();

        Assert.Single(new[] { facts.ObservationCount });
        Assert.Equal(1, facts.ObservationCount);
        Assert.Equal(DateTimeOffset.UnixEpoch.AddMilliseconds(4), facts.FirstTimestamp);
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
