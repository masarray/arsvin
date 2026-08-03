using System.Collections.Specialized;
using System.Globalization;
using System.Text.RegularExpressions;
using AR.Iec61850.SampledValues.Field;
using AR.Iec61850.SampledValues.Measurements;
using ARSVIN.Subscriber.Models;

namespace ARSVIN.Subscriber.ViewModels;

public sealed partial class SvStreamViewModel
{
    private bool _refreshingFieldMode;
    private string _captureFieldState = "UNKNOWN";
    private string _protocolFieldState = "UNKNOWN";
    private string _streamFieldState = "UNKNOWN";
    private string _configurationFieldState = "UNKNOWN";
    private string _measurementFieldState = "UNKNOWN";
    private string _measurementFieldDetail = "Measurement semantics unresolved";
    private string _fieldSummary = "Waiting for field evidence";
    private string _signalState = "UNRESOLVED";

    private void InitializeFieldMode()
    {
        PropertyChanged += (_, args) =>
        {
            if (args.PropertyName is nameof(Packets) or nameof(Health) or nameof(HealthDetail) or nameof(Issues) or
                nameof(Bound) or nameof(SclMatch) or nameof(Scaling) or nameof(Timebase) or nameof(MeasurementContext) or
                nameof(AnalysisTrustState) or nameof(AnalysisTrustDetail))
                RefreshFieldMode();
        };
        ((INotifyCollectionChanged)_values).CollectionChanged += (_, _) => RefreshFieldMode();
        ((INotifyCollectionChanged)_genericWaveformPoints).CollectionChanged += (_, _) => RefreshFieldMode();
        RefreshFieldMode();
    }

    public string CaptureFieldState { get => _captureFieldState; private set => SetProperty(ref _captureFieldState, value); }
    public string ProtocolFieldState { get => _protocolFieldState; private set => SetProperty(ref _protocolFieldState, value); }
    public string StreamFieldState { get => _streamFieldState; private set => SetProperty(ref _streamFieldState, value); }
    public string ConfigurationFieldState { get => _configurationFieldState; private set => SetProperty(ref _configurationFieldState, value); }
    public string MeasurementFieldState { get => _measurementFieldState; private set => SetProperty(ref _measurementFieldState, value); }
    public string MeasurementFieldDetail { get => _measurementFieldDetail; private set => SetProperty(ref _measurementFieldDetail, value); }
    public string FieldSummary { get => _fieldSummary; private set => SetProperty(ref _fieldSummary, value); }
    public string SignalState { get => _signalState; private set => SetProperty(ref _signalState, value); }

    private void RefreshFieldMode()
    {
        if (_refreshingFieldMode)
            return;

        _refreshingFieldMode = true;
        try
        {
            var frames = ParseLong(Packets);
            var issueCounts = ParseIssueCounts(Issues);
            var representative = ResolveRepresentativeSeries(_genericWaveformPoints);
            var samplesPerCycle = ParseSamplesPerCycle(Timebase);
            var signal = IsAnalysisTrusted && representative.Count >= 16
                ? SvSignalStateAnalyzer.Analyze(representative, new SvSignalAnalysisOptions { SamplesPerCycle = samplesPerCycle })
                : null;
            var comparison = BuildDisplayComparison(SclMatch);
            var semanticMapping = Bound.StartsWith("SCL:", StringComparison.OrdinalIgnoreCase);
            var engineeringScaling = _values.Any(value => value.HasEngineeringValue &&
                !string.Equals(value.EngineeringUnit, "count", StringComparison.OrdinalIgnoreCase));
            var validatedScaling = _values.Any(value => value.ScalingConfidence == SvEngineeringScaleConfidence.DeviceValidated);

            var report = SvFieldHealthEvaluator.Evaluate(new SvFieldHealthInput
            {
                RawFrameCount = frames,
                SvFrameCount = frames,
                ParseErrorCount = 0,
                SequenceGapCount = issueCounts.Gap,
                DuplicateCount = issueCounts.Duplicate,
                OutOfOrderCount = issueCounts.OutOfOrder,
                PayloadIssueCount = issueCounts.Payload,
                ConfigurationComparison = comparison,
                IsSclBound = semanticMapping,
                HasSemanticMapping = semanticMapping,
                HasEngineeringScaling = engineeringScaling,
                IsScalingValidated = validatedScaling,
                Signal = signal
            });

            CaptureFieldState = Label(report.Capture.State);
            ProtocolFieldState = Label(report.Protocol.State);
            StreamFieldState = Label(report.Stream.State);
            ConfigurationFieldState = Label(report.Configuration.State);
            MeasurementFieldState = Label(report.Measurement.State);
            MeasurementFieldDetail = report.Measurement.Summary;
            SignalState = signal?.State.ToString().ToUpperInvariant() ?? "UNRESOLVED";

            if (AnalysisTrustState == "DEGRADED")
            {
                StreamFieldState = "WARN";
                MeasurementFieldState = "WARN";
                MeasurementFieldDetail = AnalysisTrustDetail;
                SignalState = "DEGRADED";
            }
            else if (AnalysisTrustState == "FILLING")
            {
                MeasurementFieldState = "UNKNOWN";
                MeasurementFieldDetail = AnalysisTrustDetail;
                SignalState = "FILLING";
            }
            else if (AnalysisTrustState == "UNKNOWN" && semanticMapping)
            {
                MeasurementFieldState = engineeringScaling ? "WARN" : "UNKNOWN";
                MeasurementFieldDetail = AnalysisTrustDetail;
                SignalState = "UNRESOLVED";
            }

            FieldSummary = $"CAPTURE {CaptureFieldState} · PROTOCOL {ProtocolFieldState} · STREAM {StreamFieldState} · CONFIG {ConfigurationFieldState} · MEAS {MeasurementFieldState}";

            for (var index = _evidenceDetails.Count - 1; index >= 0; index--)
            {
                var existing = _evidenceDetails[index];
                if (existing.StartsWith("FIELD ·", StringComparison.Ordinal) ||
                    existing.StartsWith("SIGNAL ·", StringComparison.Ordinal) ||
                    existing.StartsWith("ANALYSIS ·", StringComparison.Ordinal))
                    _evidenceDetails.RemoveAt(index);
            }

            var fieldLines = new[]
            {
                $"FIELD · CAPTURE · {CaptureFieldState} · {report.Capture.Summary}",
                $"FIELD · PROTOCOL · {ProtocolFieldState} · {report.Protocol.Summary}",
                $"FIELD · STREAM · {StreamFieldState} · {(AnalysisTrustState == "DEGRADED" ? AnalysisTrustDetail : report.Stream.Summary)}",
                $"FIELD · CONFIGURATION · {ConfigurationFieldState} · {report.Configuration.Summary}",
                $"FIELD · MEASUREMENT · {MeasurementFieldState} · {MeasurementFieldDetail}",
                $"ANALYSIS · {AnalysisTrustState} · {AnalysisTrustDetail}",
                signal is null ? $"SIGNAL · {SignalState}" : $"SIGNAL · {signal.Summary}"
            };
            foreach (var line in fieldLines.Reverse())
                _evidenceDetails.Insert(0, line);
        }
        finally
        {
            _refreshingFieldMode = false;
        }
    }

    private static IReadOnlyList<double> ResolveRepresentativeSeries(IEnumerable<WaveformPoint> points)
    {
        var materialized = points.ToArray();
        var candidates = new Func<WaveformPoint, double?>[]
        {
            point => point.Ia, point => point.Ib, point => point.Ic, point => point.In,
            point => point.Va, point => point.Vb, point => point.Vc, point => point.Vn
        };
        foreach (var selector in candidates)
        {
            var values = materialized.Select(selector).Where(value => value.HasValue).Select(value => value!.Value).ToArray();
            if (values.Length >= 16)
                return values;
        }
        return Array.Empty<double>();
    }

    private static double? ParseSamplesPerCycle(string value)
    {
        var match = Regex.Match(value ?? string.Empty, @"(?<value>\d+(?:[\.,]\d+)?)\s+smp/cycle", RegexOptions.IgnoreCase);
        return match.Success && double.TryParse(match.Groups["value"].Value.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }

    private static long ParseLong(string value)
        => long.TryParse(value, NumberStyles.Number, CultureInfo.CurrentCulture, out var parsed)
            ? parsed
            : long.TryParse((value ?? string.Empty).Replace(",", string.Empty, StringComparison.Ordinal), out parsed)
                ? parsed
                : 0;

    private static (int Gap, int Duplicate, int OutOfOrder, int Payload) ParseIssueCounts(string value)
    {
        static int Read(string text, string label)
        {
            var match = Regex.Match(text, $@"{Regex.Escape(label)}\s+(?<value>\d+)", RegexOptions.IgnoreCase);
            return match.Success && int.TryParse(match.Groups["value"].Value, out var parsed) ? parsed : 0;
        }
        return (Read(value, "gap"), Read(value, "dup"), Read(value, "order"), Read(value, "payload"));
    }

    private static AR.Iec61850.SampledValues.Profiles.SvConfigurationComparisonResult? BuildDisplayComparison(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Contains("Not configured", StringComparison.OrdinalIgnoreCase))
            return null;
        if (value.Contains("error", StringComparison.OrdinalIgnoreCase))
            return new AR.Iec61850.SampledValues.Profiles.SvConfigurationComparisonResult
            {
                Findings = [new(AR.Iec61850.SampledValues.Profiles.SvConfigurationFindingSeverity.Error, "FIELD_CONFIG", "SCL", "configured", "observed", value)]
            };
        if (value.Contains("warning", StringComparison.OrdinalIgnoreCase))
            return new AR.Iec61850.SampledValues.Profiles.SvConfigurationComparisonResult
            {
                Findings = [new(AR.Iec61850.SampledValues.Profiles.SvConfigurationFindingSeverity.Warning, "FIELD_CONFIG", "SCL", "configured", "observed", value)]
            };
        return new AR.Iec61850.SampledValues.Profiles.SvConfigurationComparisonResult();
    }

    private static string Label(SvFieldHealthState state) => state switch
    {
        SvFieldHealthState.Good => "GOOD",
        SvFieldHealthState.Quiet => "QUIET",
        SvFieldHealthState.Warning => "WARN",
        SvFieldHealthState.Bad => "BAD",
        _ => "UNKNOWN"
    };
}