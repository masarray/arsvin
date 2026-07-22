using AR.Iec61850.SampledValues.Measurements;
using AR.Iec61850.SampledValues.Profiles;
using ARSVIN.Subscriber.Models;

namespace ARSVIN.Subscriber.ViewModels;

public sealed class SvStreamViewModel : ObservableObject
{
    private readonly BulkObservableCollection<DecodedValueRow> _values = new();
    private readonly BulkObservableCollection<WaveformPoint> _waveformPoints = new();
    private readonly BulkObservableCollection<PhasorVector> _phasors = new();
    private readonly BulkObservableCollection<string> _evidenceDetails = new();
    private string _key = string.Empty;
    private string _health = "IDLE";
    private string _healthDetail = string.Empty;
    private string _appId = string.Empty;
    private string _svId = string.Empty;
    private string _source = string.Empty;
    private string _destination = string.Empty;
    private string _vlan = string.Empty;
    private string _confRev = string.Empty;
    private string _nofAsdu = string.Empty;
    private string _sampleRate = string.Empty;
    private string _smpCnt = string.Empty;
    private string _smpSynch = string.Empty;
    private string _packets = string.Empty;
    private string _fps = string.Empty;
    private string _gap = string.Empty;
    private string _issues = string.Empty;
    private string _bound = string.Empty;
    private string _lastSeen = string.Empty;
    private string _summary = string.Empty;
    private string _dataSet = string.Empty;
    private string _sourceDestination = string.Empty;
    private string _cursorSummary = string.Empty;
    private string _qualitySummary = string.Empty;
    private string _profile = "Unknown";
    private string _confidence = "Unknown · insufficient evidence";
    private string _sclMatch = "Not configured";
    private string _window = "0.0 s · 0 samples";
    private string _comparisonMode = "Compatible";
    private string _waveformState = "Waiting for a resolved timebase";
    private string _scaling = "Raw counts";
    private string _timebase = "Unknown";

    public IReadOnlyList<DecodedValueRow> Values => _values;
    public IReadOnlyList<WaveformPoint> WaveformPoints => _waveformPoints;
    public IReadOnlyList<PhasorVector> Phasors => _phasors;
    public IReadOnlyList<string> EvidenceDetails => _evidenceDetails;

    public string Key { get => _key; set => SetProperty(ref _key, value); }
    public string Health { get => _health; set => SetProperty(ref _health, value); }
    public string HealthDetail { get => _healthDetail; set => SetProperty(ref _healthDetail, value); }
    public string AppId { get => _appId; set => SetProperty(ref _appId, value); }
    public string SvId { get => _svId; set => SetProperty(ref _svId, value); }
    public string Source { get => _source; set => SetProperty(ref _source, value); }
    public string Destination { get => _destination; set => SetProperty(ref _destination, value); }
    public string Vlan { get => _vlan; set => SetProperty(ref _vlan, value); }
    public string ConfRev { get => _confRev; set => SetProperty(ref _confRev, value); }
    public string NofAsdu { get => _nofAsdu; set => SetProperty(ref _nofAsdu, value); }
    public string SampleRate { get => _sampleRate; set => SetProperty(ref _sampleRate, value); }
    public string SmpCnt { get => _smpCnt; set => SetProperty(ref _smpCnt, value); }
    public string SmpSynch { get => _smpSynch; set => SetProperty(ref _smpSynch, value); }
    public string Packets { get => _packets; set => SetProperty(ref _packets, value); }
    public string Fps { get => _fps; set => SetProperty(ref _fps, value); }
    public string Gap { get => _gap; set => SetProperty(ref _gap, value); }
    public string Issues { get => _issues; set => SetProperty(ref _issues, value); }
    public string Bound { get => _bound; set => SetProperty(ref _bound, value); }
    public string LastSeen { get => _lastSeen; set => SetProperty(ref _lastSeen, value); }
    public string Summary { get => _summary; set => SetProperty(ref _summary, value); }
    public string DataSet { get => _dataSet; set => SetProperty(ref _dataSet, value); }
    public string SourceDestination { get => _sourceDestination; set => SetProperty(ref _sourceDestination, value); }
    public string CursorSummary { get => _cursorSummary; set => SetProperty(ref _cursorSummary, value); }
    public string QualitySummary { get => _qualitySummary; set => SetProperty(ref _qualitySummary, value); }
    public string Profile { get => _profile; set => SetProperty(ref _profile, value); }
    public string Confidence { get => _confidence; set => SetProperty(ref _confidence, value); }
    public string SclMatch { get => _sclMatch; set => SetProperty(ref _sclMatch, value); }
    public string Window { get => _window; set => SetProperty(ref _window, value); }
    public string ComparisonMode { get => _comparisonMode; set => SetProperty(ref _comparisonMode, value); }
    public string WaveformState { get => _waveformState; set => SetProperty(ref _waveformState, value); }
    public string Scaling { get => _scaling; set => SetProperty(ref _scaling, value); }
    public string Timebase { get => _timebase; set => SetProperty(ref _timebase, value); }

    public void Apply(SvStreamSnapshot snapshot, SvStreamObservationSnapshot? observation)
    {
        Key = snapshot.Key;
        AppId = $"0x{snapshot.AppId:X4}";
        SvId = string.IsNullOrWhiteSpace(snapshot.SvId) ? "-" : snapshot.SvId;
        Source = snapshot.Source;
        Destination = snapshot.Destination;
        SourceDestination = string.IsNullOrWhiteSpace(snapshot.Source) ? "-" : $"{snapshot.Source} → {snapshot.Destination}";
        Vlan = snapshot.VlanId.HasValue ? $"{snapshot.VlanId} / p{snapshot.VlanPriority ?? 0}" : "untagged";
        ConfRev = snapshot.ConfRev?.ToString() ?? "-";
        NofAsdu = snapshot.NofAsdu <= 0 ? "-" : snapshot.NofAsdu.ToString();
        SampleRate = BuildSampleRateText(snapshot);
        SmpCnt = snapshot.LastSmpCnt?.ToString() ?? "-";
        SmpSynch = snapshot.SmpSynch?.ToString() ?? "-";
        Packets = snapshot.FrameCount.ToString("N0");
        Fps = $"{snapshot.ActualFps:0.0}";
        Gap = $"avg {snapshot.AverageFrameGapMilliseconds:0.###} ms / max {snapshot.MaxFrameGapMilliseconds:0.###} ms";
        DataSet = string.IsNullOrWhiteSpace(snapshot.DataSet) ? "-" : snapshot.DataSet;
        CursorSummary = snapshot.CursorSummary;
        Scaling = snapshot.ScalingSummary;
        Timebase = BuildTimebaseText(snapshot);

        var qualityStates = ResolveQualityStates(snapshot.Values);
        QualitySummary = BuildQualitySummary(qualityStates, snapshot.QualitySummary);

        var comparison = observation?.ConfigurationComparison;
        var configurationIssues = comparison?.Findings.Count ?? 0;
        var qualityIssues = qualityStates.Count(item =>
            item.Severity is SvQualitySeverity.Warning or SvQualitySeverity.Bad or SvQualitySeverity.Unknown);
        var issueTotal = snapshot.SequenceGapCount + snapshot.DuplicateCount + snapshot.OutOfOrderCount +
                         snapshot.PayloadIssueCount + configurationIssues + qualityIssues;
        Issues = issueTotal == 0
            ? "0"
            : $"{issueTotal} (gap {snapshot.SequenceGapCount}, dup {snapshot.DuplicateCount}, order {snapshot.OutOfOrderCount}, payload {snapshot.PayloadIssueCount}, quality {qualityIssues}, SCL {configurationIssues})";

        var hasBlockingConfigurationError = comparison?.HasBlockingErrors == true;
        var hasConfigurationWarning = comparison?.WarningCount > 0;
        var hasQualityBad = qualityStates.Any(item =>
            item.Severity is SvQualitySeverity.Bad or SvQualitySeverity.Unknown);
        var hasQualityWarning = qualityStates.Any(item => item.Severity == SvQualitySeverity.Warning);
        Health = hasBlockingConfigurationError || hasQualityBad
            ? "BAD"
            : (hasConfigurationWarning || hasQualityWarning) && snapshot.Health == "GOOD"
                ? "WARN"
                : snapshot.Health;
        HealthDetail = ResolveHealthDetail(snapshot, observation, qualityStates, Health);

        var isConfigured = observation?.IsBoundToScl == true;
        Bound = isConfigured
            ? $"SCL: {observation!.ControlBlockReference}"
            : string.IsNullOrWhiteSpace(snapshot.LayoutBinding) ? "Unbound" : snapshot.LayoutBinding;
        LastSeen = snapshot.LastSeen;
        Summary = string.Join("  •  ", observation?.Diagnostics.Take(3) ?? snapshot.Diagnostics.Take(3));

        var detection = observation?.ProfileDetection;
        Profile = detection?.Profile.Family ?? "Unknown profile";
        Confidence = detection is null
            ? "Unknown · insufficient evidence"
            : $"{detection.Confidence} · {ConfidenceReason(detection)}";
        SclMatch = observation?.ConfigurationMatchSummary ?? "Not configured";
        ComparisonMode = comparison?.Mode.ToString() ?? "Compatible";

        var duration = ResolveObservationDuration(observation?.Facts);
        var samples = ResolveObservationSamples(observation?.Facts);
        Window = $"{duration:0.0} s · {samples:N0} samples";

        _values.ReplaceAll(snapshot.Values.Take(64));
        UpdateStableVisuals(snapshot);
        _evidenceDetails.ReplaceAll(BuildEvidenceDetails(snapshot, observation, qualityStates));
    }

    private void UpdateStableVisuals(SvStreamSnapshot snapshot)
    {
        if (snapshot.SamplesPerCycle is not > 0)
        {
            _waveformPoints.ReplaceAll(snapshot.WaveformPoints);
            _phasors.ReplaceAll(Array.Empty<PhasorVector>());
            WaveformState = $"{snapshot.ScalingSummary} · timebase unresolved; phasor withheld";
            return;
        }

        var expectedPoints = Math.Clamp(snapshot.SamplesPerCycle.Value * 2, 32, 512);
        var fullWindow = snapshot.IsWaveformWindowReady && snapshot.WaveformPoints.Count >= expectedPoints;
        if (fullWindow)
        {
            _waveformPoints.ReplaceAll(snapshot.WaveformPoints.TakeLast(expectedPoints));
            _phasors.ReplaceAll(snapshot.Phasors);
            WaveformState = $"2 cycles locked · {expectedPoints:N0} points · {snapshot.ScalingSummary}";
            return;
        }

        _waveformPoints.ReplaceAll(snapshot.WaveformPoints);
        _phasors.ReplaceAll(Array.Empty<PhasorVector>());
        WaveformState = $"Filling verified window · {snapshot.WaveformPoints.Count:N0}/{expectedPoints:N0} points";
    }

    private static string BuildSampleRateText(SvStreamSnapshot snapshot)
    {
        var declared = snapshot.SampleRate?.ToString() ?? "-";
        var mode = snapshot.SampleMode switch
        {
            0 => "smp/cycle",
            1 => "smp/s",
            2 => "s/smp",
            _ => "mode unknown"
        };
        return $"{declared} {mode}";
    }

    private static string BuildTimebaseText(SvStreamSnapshot snapshot)
    {
        if (snapshot.NominalFrequencyHz.HasValue && snapshot.SamplesPerCycle.HasValue)
            return $"{snapshot.NominalFrequencyHz:0.#} Hz · {snapshot.SamplesPerCycle} smp/cycle · {snapshot.TimebaseSource}";
        if (snapshot.SamplesPerCycle.HasValue)
            return $"{snapshot.SamplesPerCycle} smp/cycle · frequency unknown";
        return "Unknown · no hidden 50/60 Hz assumption";
    }

    private static double ResolveObservationDuration(SvObservedStreamFacts? facts)
    {
        if (facts?.FirstTimestamp is not { } first || facts.LastTimestamp is not { } last)
            return 0;
        return Math.Max(0, (last - first).TotalSeconds);
    }

    private static int ResolveObservationSamples(SvObservedStreamFacts? facts)
    {
        if (facts is null)
            return 0;
        return facts.ObservationCount * Math.Max(1, facts.AsduPerFrame ?? 1);
    }

    private static string ConfidenceReason(SvProfileDetectionResult detection)
        => detection.Confidence switch
        {
            SvProfileConfidence.Unknown => "insufficient evidence",
            SvProfileConfidence.Possible => "partial evidence",
            SvProfileConfidence.Likely => "strong engineering evidence",
            SvProfileConfidence.Confirmed => "mature matching evidence",
            SvProfileConfidence.Conflict => "conflicting evidence",
            _ => "insufficient evidence"
        };

    private static string ResolveHealthDetail(
        SvStreamSnapshot snapshot,
        SvStreamObservationSnapshot? observation,
        IReadOnlyList<SvQualityState> qualityStates,
        string health)
    {
        var blocking = observation?.ConfigurationComparison?.Findings
            .FirstOrDefault(item => item.Severity == SvConfigurationFindingSeverity.Error);
        if (blocking is not null)
            return blocking.Message;

        var badQuality = qualityStates.FirstOrDefault(item =>
            item.Severity is SvQualitySeverity.Bad or SvQualitySeverity.Unknown);
        if (badQuality is not null)
            return $"Sample quality requires attention: {badQuality.Summary}.";

        var warning = observation?.ConfigurationComparison?.Findings
            .FirstOrDefault(item => item.Severity == SvConfigurationFindingSeverity.Warning);
        if (warning is not null && health != "BAD")
            return warning.Message;

        var warningQuality = qualityStates.FirstOrDefault(item => item.Severity == SvQualitySeverity.Warning);
        if (warningQuality is not null && health != "BAD")
            return $"Sample quality warning: {warningQuality.Summary}.";

        return snapshot.HealthDetail;
    }

    private static IReadOnlyList<SvQualityState> ResolveQualityStates(IEnumerable<DecodedValueRow> values)
        => values.Select(value => value.QualityState)
            .Where(value => value is not null)
            .Select(value => value!)
            .ToArray();

    private static string BuildQualitySummary(
        IReadOnlyList<SvQualityState> states,
        string fallback)
    {
        if (states.Count == 0)
            return fallback;

        var good = states.Count(item => item.Severity == SvQualitySeverity.Good);
        var information = states.Count(item => item.Severity == SvQualitySeverity.Information);
        var warning = states.Count(item => item.Severity == SvQualitySeverity.Warning);
        var bad = states.Count(item => item.Severity is SvQualitySeverity.Bad or SvQualitySeverity.Unknown);
        return $"Quality channels {states.Count}: good {good}, info {information}, warning {warning}, bad/unknown {bad}";
    }

    private static IReadOnlyList<string> BuildEvidenceDetails(
        SvStreamSnapshot snapshot,
        SvStreamObservationSnapshot? observation,
        IReadOnlyList<SvQualityState> qualityStates)
    {
        var lines = new List<string>
        {
            $"MEASUREMENT · scaling {snapshot.ScalingSummary} · {snapshot.ScalingReason}",
            $"TIMEBASE · {BuildTimebaseText(snapshot)} · {snapshot.TimebaseReason}",
            $"HEALTH · current {snapshot.Health} · session totals gap {snapshot.SequenceGapCount}, duplicate {snapshot.DuplicateCount}, out-of-order {snapshot.OutOfOrderCount}, payload {snapshot.PayloadIssueCount}.",
            $"QUALITY · {BuildQualitySummary(qualityStates, snapshot.QualitySummary)}"
        };

        lines.AddRange(snapshot.Values
            .Where(value => value.IsQuality && value.QualityState is not null)
            .Select(value => $"QUALITY · {value.Signal} · {value.QualityState!.Severity} · {value.QualityState.Summary} · placement {value.QualityState.Placement} · raw {value.Raw}"));

        if (observation is null)
        {
            lines.Add("No observation evidence is available yet.");
            return lines;
        }

        if (observation.ProfileDetection is { } detection)
        {
            lines.Add($"Profile: {detection.Profile.DisplayName} · confidence {detection.Confidence} · score {detection.ScorePercent:0.#}% · evidence {detection.MatchedWeight}/{detection.EvaluatedWeight}.");
            lines.AddRange(detection.Evidence.Select(item =>
                $"PROFILE · {item.Field} · {item.Outcome} · expected {item.Expected} · observed {item.Observed} · {item.Message}"));
        }

        if (observation.ConfigurationComparison is { } comparison)
        {
            lines.Add($"SCL comparison: {comparison.Summary} · mode {comparison.Mode}.");
            lines.AddRange(comparison.Findings.Select(item =>
                $"SCL · {item.Severity} · {item.Field} · expected {item.Expected} · observed {item.Observed} · {item.Message}"));
        }
        else
        {
            lines.Add("SCL comparison: not configured.");
        }

        lines.AddRange(observation.Diagnostics.Select(item => $"OBSERVATION · {item}"));
        return lines.Distinct(StringComparer.Ordinal).ToArray();
    }
}
