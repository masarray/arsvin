using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Text.RegularExpressions;
using ARSVIN.Subscriber.Models;

namespace ARSVIN.Subscriber.ViewModels;

public sealed partial class SvStreamViewModel
{
    private bool _stabilizingPresentation;
    private bool _stabilizationInitialized;

    private void InitializeStabilization()
    {
        if (_stabilizationInitialized)
            return;

        _stabilizationInitialized = true;
        ((INotifyCollectionChanged)_waveformPoints).CollectionChanged += StabilizeWaveformCollection;
        PropertyChanged += StabilizePresentationProperty;
    }

    private void StabilizeWaveformCollection(object? sender, NotifyCollectionChangedEventArgs e)
        => StabilizeWaveformAndPhasorPresentation();

    private void StabilizePresentationProperty(object? sender, PropertyChangedEventArgs e)
    {
        if (_stabilizingPresentation)
            return;

        if (e.PropertyName == nameof(Window))
        {
            var match = Regex.Match(Window ?? string.Empty, @"(?<seconds>\d+(?:[\.,]\d+)?)\s*s\s*·\s*(?<samples>[\d,\.]+)\s*samples", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                _stabilizingPresentation = true;
                try
                {
                    Window = $"capture {match.Groups["seconds"].Value} s · {match.Groups["samples"].Value} observed samples";
                }
                finally
                {
                    _stabilizingPresentation = false;
                }
            }
        }

        if (e.PropertyName is nameof(Timebase) or nameof(WaveformState) or nameof(Issues))
            StabilizeWaveformAndPhasorPresentation();
    }

    private void StabilizeWaveformAndPhasorPresentation()
    {
        if (_stabilizingPresentation || _waveformPoints.Count == 0)
            return;

        var samplesPerCycle = ParseSamplesPerCycleForStabilization(Timebase);
        if (samplesPerCycle is not > 0)
            return;

        var required = Math.Clamp(samplesPerCycle.Value * 2, 32, 512);
        var source = _waveformPoints.ToArray();
        var contiguousTail = BuildContiguousTail(source, required);
        var isComplete = contiguousTail.Count >= required;

        if (isComplete && source.Count == required && ReferenceSequenceEqual(source, contiguousTail))
            return;

        _stabilizingPresentation = true;
        try
        {
            if (isComplete)
            {
                _waveformPoints.ReplaceAll(contiguousTail.TakeLast(required));
                WaveformState = $"2 contiguous cycles · {required:N0} points · phasor eligible";
                return;
            }

            _phasors.ReplaceAll(Array.Empty<PhasorVector>());
            _waveformPoints.ReplaceAll(contiguousTail);
            WaveformState = contiguousTail.Count == 0
                ? $"Continuity degraded · waiting for {required:N0} consecutive samples · waveform and phasor withheld"
                : $"Continuity degraded · {contiguousTail.Count:N0}/{required:N0} consecutive samples · phasor withheld";

            StreamFieldState = "WARN";
            MeasurementFieldState = "UNKNOWN";
            MeasurementFieldDetail = "A complete contiguous analysis window is not available. RMS/phasor evidence is withheld rather than reconstructed from sparse packet arrival.";
            FieldSummary = $"CAPTURE {CaptureFieldState} · PROTOCOL {ProtocolFieldState} · STREAM {StreamFieldState} · CONFIG {ConfigurationFieldState} · MEAS {MeasurementFieldState}";
        }
        finally
        {
            _stabilizingPresentation = false;
        }
    }

    private static IReadOnlyList<WaveformPoint> BuildContiguousTail(
        IReadOnlyList<WaveformPoint> source,
        int maximum)
    {
        if (source.Count == 0)
            return Array.Empty<WaveformPoint>();

        var tail = new List<WaveformPoint>(Math.Min(maximum, source.Count)) { source[^1] };
        for (var index = source.Count - 2; index >= 0 && tail.Count < maximum; index--)
        {
            var current = source[index];
            var next = tail[^1];
            if (!AreSequential(current, next))
                break;
            tail.Add(current);
        }

        tail.Reverse();
        return tail;
    }

    private static bool AreSequential(WaveformPoint current, WaveformPoint next)
    {
        if (current.SampleCount.HasValue && next.SampleCount.HasValue)
        {
            var expected = unchecked((ushort)(current.SampleCount.Value + 1));
            return next.SampleCount.Value == expected ||
                   (next.SampleCount.Value == 0 && current.SampleCount.Value > 1);
        }

        return next.Index == current.Index + 1;
    }

    private static bool ReferenceSequenceEqual(
        IReadOnlyList<WaveformPoint> first,
        IReadOnlyList<WaveformPoint> second)
    {
        if (first.Count != second.Count)
            return false;
        for (var index = 0; index < first.Count; index++)
        {
            if (!ReferenceEquals(first[index], second[index]))
                return false;
        }
        return true;
    }

    private static int? ParseSamplesPerCycleForStabilization(string value)
    {
        var match = Regex.Match(value ?? string.Empty, @"(?<value>\d+(?:[\.,]\d+)?)\s+smp/cycle", RegexOptions.IgnoreCase);
        if (!match.Success ||
            !double.TryParse(match.Groups["value"].Value.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) ||
            parsed < 1 || parsed > 4096)
            return null;
        return (int)Math.Round(parsed);
    }
}
