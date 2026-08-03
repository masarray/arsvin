using System.Buffers.Binary;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Text.RegularExpressions;
using AR.Iec61850.SampledValues.Measurements;
using ARSVIN.Subscriber.Models;

namespace ARSVIN.Subscriber.ViewModels;

public sealed partial class SvStreamViewModel
{
    private const double CadenceTolerancePercent = 5.0;
    private readonly BulkObservableCollection<DecodedValueRow> _genericValues = new();
    private readonly BulkObservableCollection<WaveformPoint> _genericWaveformPoints = new();
    private readonly BulkObservableCollection<PhasorVector> _genericPhasors = new();
    private string _genericMappingState = "Raw seqOfData";
    private string _genericSemanticState = "Unresolved · no assumptions";
    private string _genericWaveformState = "Waiting for stream data";
    private string _continuityText = "smpCnt continuity unresolved";
    private string _cadenceText = "Capture timing unresolved";
    private string _captureWindowText = "Capture window unavailable";
    private string _analysisWindowText = "Analysis window unavailable";
    private string _analysisTrustState = "UNKNOWN";
    private string _analysisTrustDetail = "Waiting for a resolved timebase and contiguous samples.";
    private string _phasorState = "Waiting for a trusted cycle";
    private bool _isAnalysisTrusted;

    public SvStreamViewModel()
    {
        _values.CollectionChanged += SourceCollectionChanged;
        _waveformPoints.CollectionChanged += SourceCollectionChanged;
        _phasors.CollectionChanged += SourceCollectionChanged;
        PropertyChanged += StreamPropertyChanged;
        RefreshGenericPresentation();
        InitializeFieldMode();
    }

    public IReadOnlyList<DecodedValueRow> GenericValues => _genericValues;
    public IReadOnlyList<WaveformPoint> GenericWaveformPoints => _genericWaveformPoints;
    public IReadOnlyList<PhasorVector> GenericPhasors => _genericPhasors;

    public string GenericMappingState { get => _genericMappingState; private set => SetProperty(ref _genericMappingState, value); }
    public string GenericSemanticState { get => _genericSemanticState; private set => SetProperty(ref _genericSemanticState, value); }
    public string GenericWaveformState { get => _genericWaveformState; private set => SetProperty(ref _genericWaveformState, value); }
    public string ContinuityText { get => _continuityText; private set => SetProperty(ref _continuityText, value); }
    public string CadenceText { get => _cadenceText; private set => SetProperty(ref _cadenceText, value); }
    public string CaptureWindowText { get => _captureWindowText; private set => SetProperty(ref _captureWindowText, value); }
    public string AnalysisWindowText { get => _analysisWindowText; private set => SetProperty(ref _analysisWindowText, value); }
    public string AnalysisTrustState { get => _analysisTrustState; private set => SetProperty(ref _analysisTrustState, value); }
    public string AnalysisTrustDetail { get => _analysisTrustDetail; private set => SetProperty(ref _analysisTrustDetail, value); }
    public string PhasorState { get => _phasorState; private set => SetProperty(ref _phasorState, value); }
    public bool IsAnalysisTrusted { get => _isAnalysisTrusted; private set => SetProperty(ref _isAnalysisTrusted, value); }

    private void SourceCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        => RefreshGenericPresentation();

    private void StreamPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(Bound) or nameof(WaveformState) or nameof(Scaling) or
            nameof(Fps) or nameof(NofAsdu) or nameof(SmpCnt) or nameof(Timebase) or
            nameof(Window) or nameof(Issues))
            RefreshGenericPresentation();
    }

    private void RefreshGenericPresentation()
    {
        CaptureWindowText = string.IsNullOrWhiteSpace(Window) ? "Capture window unavailable" : $"Capture {Window}";

        if (!HasSclSemanticMapping())
        {
            GenericMappingState = "Raw seqOfData";
            GenericSemanticState = "Unresolved · words shown without channel, unit, or quality claims";
            GenericWaveformState = _values.Count == 0
                ? "Waiting for seqOfData"
                : "Raw words available · import SCL before waveform and phasor analysis";
            ContinuityText = "smpCnt available · semantic window not built";
            CadenceText = "Expected sample-domain cadence unresolved";
            AnalysisWindowText = "No semantic analysis window";
            AnalysisTrustState = "UNKNOWN";
            AnalysisTrustDetail = "SCL/CID mapping is required before semantic waveform and phasor analysis.";
            PhasorState = "Withheld · semantics unresolved";
            IsAnalysisTrusted = false;
            _genericValues.ReplaceAll(BuildGenericRows(_values));
            _genericWaveformPoints.ReplaceAll(Array.Empty<WaveformPoint>());
            _genericPhasors.ReplaceAll(Array.Empty<PhasorVector>());
            RefreshFieldMode();
            return;
        }

        GenericMappingState = "SCL dataset mapping";
        GenericSemanticState = "Resolved from ordered SCL elements";
        _genericValues.ReplaceAll(_values);

        var timebase = ParseTimebase(Timebase);
        if (timebase is null)
        {
            SetWithheld(
                "UNKNOWN",
                "Timebase is unresolved; waveform and phasor are withheld.",
                "smpCnt continuity cannot be qualified without samples-per-cycle.",
                "Expected sample-domain cadence unresolved",
                "No trusted analysis window");
            RefreshFieldMode();
            return;
        }

        var expectedPoints = Math.Clamp(timebase.Value.SamplesPerCycle * 2, 32, 512);
        var expectedSamplesPerSecond = timebase.Value.FrequencyHz * timebase.Value.SamplesPerCycle;
        var observedFramesPerSecond = ParseDouble(Fps);
        var nofAsdu = Math.Max(1, ParseInt(NofAsdu));
        double? observedSamplesPerSecond = observedFramesPerSecond.HasValue
            ? observedFramesPerSecond.Value * nofAsdu
            : null;
        double? cadenceErrorPercent = observedSamplesPerSecond.HasValue && expectedSamplesPerSecond > 0
            ? Math.Abs(observedSamplesPerSecond.Value - expectedSamplesPerSecond) / expectedSamplesPerSecond * 100.0
            : null;
        var captureTimingWarning = cadenceErrorPercent.HasValue && cadenceErrorPercent.Value > CadenceTolerancePercent;

        CadenceText = observedSamplesPerSecond.HasValue
            ? captureTimingWarning
                ? $"Host delivery {observedSamplesPerSecond.Value:0.0} / sample-domain {expectedSamplesPerSecond:0.0} smp/s · timing WARN"
                : $"Host delivery {observedSamplesPerSecond.Value:0.0} / sample-domain {expectedSamplesPerSecond:0.0} smp/s"
            : $"Sample-domain {expectedSamplesPerSecond:0.0} smp/s · host delivery unavailable";

        var ordered = BuildOrderedWindow(
            _waveformPoints,
            expectedPoints,
            expectedSamplesPerSecond,
            ParseUShort(SmpCnt));
        var contiguous = IsContiguous(ordered, expectedSamplesPerSecond);
        var durationMilliseconds = 2_000.0 / timebase.Value.FrequencyHz;
        AnalysisWindowText = $"{ordered.Count:N0}/{expectedPoints:N0} points · 2 cycles · {durationMilliseconds:0.0} ms";
        ContinuityText = contiguous
            ? $"smpCnt contiguous · {ordered.Count:N0} points"
            : $"smpCnt incomplete/discontinuous · {ordered.Count:N0}/{expectedPoints:N0}";

        if (ordered.Count < expectedPoints)
        {
            SetWithheld(
                "FILLING",
                $"Waiting for a complete contiguous two-cycle window ({ordered.Count:N0}/{expectedPoints:N0} points).",
                ContinuityText,
                CadenceText,
                AnalysisWindowText);
            RefreshFieldMode();
            return;
        }

        if (!contiguous)
        {
            SetWithheld(
                "DEGRADED",
                "Waveform samples are not contiguous by smpCnt; interpolation is not permitted.",
                ContinuityText,
                CadenceText,
                AnalysisWindowText);
            RefreshFieldMode();
            return;
        }

        var phasors = ComputePhasors(ordered, timebase.Value.SamplesPerCycle);
        IsAnalysisTrusted = true;
        AnalysisTrustState = captureTimingWarning ? "TIMING WARN" : "TRUSTED";
        AnalysisTrustDetail = captureTimingWarning
            ? $"Two contiguous sample-domain cycles are available, so waveform and DFT are shown. Host capture delivery differs from the resolved sample rate by {cadenceErrorPercent!.Value:0.0}%; do not use host arrival timing for latency, jitter, or real-time frequency evidence."
            : "Two contiguous sample-domain cycles are available and host capture delivery agrees with the resolved timebase.";
        GenericWaveformState = captureTimingWarning
            ? $"2 contiguous cycles · {expectedPoints:N0} points · sample-domain · host timing WARN"
            : $"2 contiguous cycles · {expectedPoints:N0} points · {Scaling}";
        PhasorState = phasors.Count == 0
            ? "Withheld · no complete analog cycle"
            : captureTimingWarning
                ? $"{phasors.Count} vectors · sample-domain DFT · host timing WARN"
                : $"{phasors.Count} vectors · contiguous one-cycle DFT";
        _genericWaveformPoints.ReplaceAll(ordered);
        _genericPhasors.ReplaceAll(phasors);
        RefreshFieldMode();
    }

    private void SetWithheld(
        string state,
        string reason,
        string continuity,
        string cadence,
        string analysisWindow)
    {
        IsAnalysisTrusted = false;
        AnalysisTrustState = state;
        AnalysisTrustDetail = reason;
        ContinuityText = continuity;
        CadenceText = cadence;
        AnalysisWindowText = analysisWindow;
        GenericWaveformState = $"Analysis withheld · {reason}";
        PhasorState = $"Withheld · {reason}";
        _genericWaveformPoints.ReplaceAll(Array.Empty<WaveformPoint>());
        _genericPhasors.ReplaceAll(Array.Empty<PhasorVector>());
    }

    private bool HasSclSemanticMapping()
        => Bound.StartsWith("SCL:", StringComparison.OrdinalIgnoreCase);

    private static (double FrequencyHz, int SamplesPerCycle)? ParseTimebase(string text)
    {
        var match = Regex.Match(
            text ?? string.Empty,
            @"(?<hz>\d+(?:[\.,]\d+)?)\s*Hz.*?(?<spc>\d+)\s*smp/cycle",
            RegexOptions.IgnoreCase);
        if (!match.Success ||
            !double.TryParse(match.Groups["hz"].Value.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var hz) ||
            !int.TryParse(match.Groups["spc"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var spc) ||
            hz <= 0 || spc <= 0)
            return null;
        return (hz, spc);
    }

    private static IReadOnlyList<WaveformPoint> BuildOrderedWindow(
        IReadOnlyList<WaveformPoint> source,
        int expectedPoints,
        double expectedSamplesPerSecond,
        ushort? latestSampleCount)
    {
        if (source.Count == 0 || !latestSampleCount.HasValue)
            return Array.Empty<WaveformPoint>();

        var wrap = ResolveCounterWrap(expectedSamplesPerSecond);
        var latest = latestSampleCount.Value;
        return source
            .Where(point => point.SampleCount.HasValue)
            .Select(point => new
            {
                Point = point,
                Distance = BackwardDistance(latest, point.SampleCount!.Value, wrap)
            })
            .Where(item => item.Distance >= 0 && item.Distance < expectedPoints)
            .GroupBy(item => item.Point.SampleCount!.Value)
            .Select(group => group.OrderBy(item => item.Distance).First())
            .OrderByDescending(item => item.Distance)
            .Take(expectedPoints)
            .Select((item, index) => CopyPoint(item.Point, index))
            .ToArray();
    }

    private static bool IsContiguous(IReadOnlyList<WaveformPoint> points, double expectedSamplesPerSecond)
    {
        if (points.Count < 2 || points.Any(point => !point.SampleCount.HasValue))
            return false;
        var wrap = ResolveCounterWrap(expectedSamplesPerSecond);
        for (var index = 1; index < points.Count; index++)
        {
            var previous = points[index - 1].SampleCount!.Value;
            var current = points[index].SampleCount!.Value;
            var expected = (previous + 1) % wrap;
            if (current != expected)
                return false;
        }
        return true;
    }

    private static int ResolveCounterWrap(double expectedSamplesPerSecond)
    {
        var candidate = (int)Math.Round(expectedSamplesPerSecond);
        return candidate is >= 2 and <= ushort.MaxValue &&
               Math.Abs(expectedSamplesPerSecond - candidate) <= 0.01
            ? candidate
            : ushort.MaxValue + 1;
    }

    private static int BackwardDistance(int latest, int current, int wrap)
        => (latest - current + wrap) % wrap;

    private static WaveformPoint CopyPoint(WaveformPoint point, int index)
        => new()
        {
            Index = index,
            SampleCount = point.SampleCount,
            CurrentUnit = point.CurrentUnit,
            VoltageUnit = point.VoltageUnit,
            ScalingSummary = point.ScalingSummary,
            Ia = point.Ia,
            Ib = point.Ib,
            Ic = point.Ic,
            In = point.In,
            Va = point.Va,
            Vb = point.Vb,
            Vc = point.Vc,
            Vn = point.Vn
        };

    private static IReadOnlyList<PhasorVector> ComputePhasors(
        IReadOnlyList<WaveformPoint> points,
        int samplesPerCycle)
    {
        if (points.Count < samplesPerCycle)
            return Array.Empty<PhasorVector>();

        var window = points.TakeLast(samplesPerCycle).ToArray();
        var currentUnit = window[^1].CurrentUnit;
        var voltageUnit = window[^1].VoltageUnit;
        var phasors = new List<PhasorVector>();
        foreach (var item in new[]
        {
            (Name: "Ia", Kind: "Current", Unit: currentUnit, Values: window.Select(point => point.Ia)),
            (Name: "Ib", Kind: "Current", Unit: currentUnit, Values: window.Select(point => point.Ib)),
            (Name: "Ic", Kind: "Current", Unit: currentUnit, Values: window.Select(point => point.Ic)),
            (Name: "In", Kind: "Current", Unit: currentUnit, Values: window.Select(point => point.In)),
            (Name: "Va", Kind: "Voltage", Unit: voltageUnit, Values: window.Select(point => point.Va)),
            (Name: "Vb", Kind: "Voltage", Unit: voltageUnit, Values: window.Select(point => point.Vb)),
            (Name: "Vc", Kind: "Voltage", Unit: voltageUnit, Values: window.Select(point => point.Vc)),
            (Name: "Vn", Kind: "Voltage", Unit: voltageUnit, Values: window.Select(point => point.Vn))
        })
        {
            var values = item.Values.ToArray();
            if (values.Any(value => !value.HasValue))
                continue;

            var numeric = values.Select(value => value!.Value).ToArray();
            var rms = Math.Sqrt(numeric.Select(value => value * value).Average());
            var peak = numeric.Select(Math.Abs).DefaultIfEmpty(0).Max();
            var mean = numeric.Average();
            var sin = 0.0;
            var cos = 0.0;
            for (var index = 0; index < numeric.Length; index++)
            {
                var theta = 2.0 * Math.PI * index / numeric.Length;
                var ac = numeric[index] - mean;
                sin += ac * Math.Sin(theta);
                cos += ac * Math.Cos(theta);
            }

            phasors.Add(new PhasorVector
            {
                Channel = item.Name,
                Kind = item.Kind,
                Unit = item.Unit,
                Rms = rms,
                Peak = peak,
                AngleDegrees = NormalizeAngle(Math.Atan2(cos, sin) * 180.0 / Math.PI)
            });
        }

        var va = phasors.FirstOrDefault(vector =>
            string.Equals(vector.Channel, "Va", StringComparison.OrdinalIgnoreCase) && vector.Rms > 0);
        if (va is null)
            return phasors;

        return phasors.Select(vector => new PhasorVector
        {
            Channel = vector.Channel,
            Kind = vector.Kind,
            Unit = vector.Unit,
            Rms = vector.Rms,
            Peak = vector.Peak,
            AngleDegrees = NormalizeAngle(vector.AngleDegrees - va.AngleDegrees),
            IsValid = vector.IsValid,
            InvalidReason = vector.InvalidReason
        }).ToArray();
    }

    private static double NormalizeAngle(double angle)
    {
        while (angle > 180) angle -= 360;
        while (angle <= -180) angle += 360;
        return angle;
    }

    private static double? ParseDouble(string text)
    {
        if (double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out var current))
            return current;
        return double.TryParse((text ?? string.Empty).Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var invariant)
            ? invariant
            : null;
    }

    private static int ParseInt(string text)
        => int.TryParse(text, NumberStyles.Integer, CultureInfo.CurrentCulture, out var value) ? value : 1;

    private static ushort? ParseUShort(string text)
        => ushort.TryParse(text, NumberStyles.Integer, CultureInfo.CurrentCulture, out var value) ? value : null;

    private static IReadOnlyList<DecodedValueRow> BuildGenericRows(IReadOnlyList<DecodedValueRow> source)
    {
        if (source.Count == 0)
            return Array.Empty<DecodedValueRow>();

        var rows = new List<DecodedValueRow>(source.Count);
        for (var index = 0; index < source.Count; index++)
        {
            var original = source[index];
            var byteOffset = index * 4;
            if (!TryReadWord(original.Raw, out var signed, out var unsigned))
            {
                rows.Add(new DecodedValueRow
                {
                    Index = index + 1,
                    Signal = $"Bytes {index + 1} (+0x{byteOffset:X2})",
                    Kind = "Raw bytes",
                    Value = original.Raw,
                    Raw = original.Raw,
                    ScalingSource = SvEngineeringScaleSource.RawOnly,
                    ScalingConfidence = SvEngineeringScaleConfidence.Unknown,
                    ScalingReason = "No SCL mapping is bound; bytes are preserved without semantic interpretation."
                });
                continue;
            }

            rows.Add(new DecodedValueRow
            {
                Index = index + 1,
                Signal = $"Word {index + 1} (+0x{byteOffset:X2})",
                Kind = index % 2 == 0 ? "INT32 / UINT32 · group word 1" : "INT32 / UINT32 · group word 2",
                Value = $"{signed} / {unsigned}",
                Raw = original.Raw,
                NumericValue = signed,
                ScalingSource = SvEngineeringScaleSource.RawOnly,
                ScalingConfidence = SvEngineeringScaleConfidence.Unknown,
                ScalingReason = "Generic 32-bit representation only. Channel, quality, unit, and scaling semantics are unresolved until SCL or explicit reviewed mapping is available."
            });
        }
        return rows;
    }

    private static bool TryReadWord(string rawHex, out int signed, out uint unsigned)
    {
        signed = 0;
        unsigned = 0;
        if (string.IsNullOrWhiteSpace(rawHex) || rawHex.Length != 8)
            return false;

        try
        {
            var bytes = Convert.FromHexString(rawHex);
            unsigned = BinaryPrimitives.ReadUInt32BigEndian(bytes);
            signed = unchecked((int)unsigned);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}