using AR.Iec61850.SampledValues.Measurements;
using ARSVIN.Subscriber.Models;
using ARSVIN.Subscriber.ViewModels;

namespace ARSVIN.Tests.SampledValues;

public sealed class FieldModeViewModelTests
{
    [Fact]
    public void RawUnboundStreamRemainsOperationalAndMeasurementUnknown()
    {
        var viewModel = new SvStreamViewModel();
        viewModel.Apply(new SvStreamSnapshot
        {
            Key = "stream-1",
            Health = "GOOD",
            AppId = 0x4001,
            SvId = "MU01",
            FrameCount = 100,
            ActualFps = 4800,
            LayoutBinding = "Unbound raw payload",
            ScalingSummary = "Raw counts",
            Values = [new DecodedValueRow { Index = 1, Signal = "Element 1", Kind = "INT32", Value = "1", Raw = "00000001" }]
        }, null);

        Assert.Equal("GOOD", viewModel.CaptureFieldState);
        Assert.Equal("GOOD", viewModel.ProtocolFieldState);
        Assert.Equal("GOOD", viewModel.StreamFieldState);
        Assert.Equal("UNKNOWN", viewModel.ConfigurationFieldState);
        Assert.Contains(viewModel.MeasurementFieldState, new[] { "UNKNOWN", "WARN" });
    }

    [Fact]
    public void NoiseDominatedWaveformIsReportedWithoutZeroingSamples()
    {
        var random = new Random(615);
        var waveform = Enumerable.Range(0, 160).Select(index => new WaveformPoint
        {
            Index = index,
            SampleCount = (ushort)index,
            Ia = random.NextDouble() * 2 - 1,
            CurrentUnit = "count"
        }).ToArray();
        var viewModel = new SvStreamViewModel();
        viewModel.Apply(new SvStreamSnapshot
        {
            Key = "stream-quiet",
            Health = "GOOD",
            AppId = 0x4001,
            SvId = "MU01",
            FrameCount = 160,
            SamplesPerCycle = 80,
            NominalFrequencyHz = 60,
            LayoutBinding = "SCL: MU01/LLN0.MSVCB",
            ScalingSummary = "Raw counts",
            WaveformPoints = waveform
        }, null);

        Assert.Equal("NOISEDOMINATED", viewModel.SignalState);
        Assert.Contains(viewModel.EvidenceDetails, line => line.StartsWith("FIELD · MEASUREMENT", StringComparison.Ordinal));
        Assert.Contains(viewModel.WaveformPoints, point => point.Ia != 0);
    }
}
