using System.Globalization;
using AR.Iec61850.SampledValues.Profiles;

namespace AR.Iec61850.SampledValues.Reporting;

public enum SvEvidenceChangeKind
{
    Added,
    Removed,
    Changed,
    Unchanged
}

public enum SvEvidenceChangeSeverity
{
    Info,
    Warning,
    Error
}

public sealed record SvSubscriberEvidenceComparison
{
    public const string CurrentSchemaVersion = "arsvin.sv-subscriber-evidence-comparison/v1";

    public string SchemaVersion { get; init; } = CurrentSchemaVersion;
    public DateTimeOffset GeneratedAt { get; init; }
    public SvEvidenceReportReference Baseline { get; init; } = new();
    public SvEvidenceReportReference Candidate { get; init; } = new();
    public SvEvidenceComparisonSummary Summary { get; init; } = new();
    public IReadOnlyList<SvEvidenceFieldChange> ReportChanges { get; init; }
        = Array.Empty<SvEvidenceFieldChange>();
    public IReadOnlyList<SvSubscriberStreamComparison> Streams { get; init; }
        = Array.Empty<SvSubscriberStreamComparison>();

    public void Validate()
    {
        if (!string.Equals(SchemaVersion, CurrentSchemaVersion, StringComparison.Ordinal))
            throw new InvalidOperationException($"Unsupported SV comparison schema '{SchemaVersion}'.");
        if (GeneratedAt == default)
            throw new InvalidOperationException("SV comparison requires a generation timestamp.");
        if (string.IsNullOrWhiteSpace(Baseline.SchemaVersion) || string.IsNullOrWhiteSpace(Candidate.SchemaVersion))
            throw new InvalidOperationException("SV comparison requires baseline and candidate schema metadata.");
        if (Streams.Any(stream => string.IsNullOrWhiteSpace(stream.ComparisonKey)))
            throw new InvalidOperationException("Every stream comparison requires a stable comparison key.");
        if (Streams.Select(stream => stream.ComparisonKey).Distinct(StringComparer.Ordinal).Count() != Streams.Count)
            throw new InvalidOperationException("SV comparison keys must be unique.");

        var classified = Summary.AddedStreamCount + Summary.RemovedStreamCount +
                         Summary.ChangedStreamCount + Summary.UnchangedStreamCount;
        if (classified != Streams.Count)
            throw new InvalidOperationException("SV comparison summary does not match the stream collection.");

        var changes = ReportChanges.Concat(Streams.SelectMany(stream => stream.Changes)).ToArray();
        if (Summary.InfoChangeCount != changes.Count(change => change.Severity == SvEvidenceChangeSeverity.Info) ||
            Summary.WarningChangeCount != changes.Count(change => change.Severity == SvEvidenceChangeSeverity.Warning) ||
            Summary.ErrorChangeCount != changes.Count(change => change.Severity == SvEvidenceChangeSeverity.Error))
        {
            throw new InvalidOperationException("SV comparison severity totals do not match the evidence.");
        }
    }
}

public sealed record SvEvidenceReportReference
{
    public string SchemaVersion { get; init; } = string.Empty;
    public DateTimeOffset GeneratedAt { get; init; }
    public string Product { get; init; } = string.Empty;
    public string Version { get; init; } = string.Empty;
    public string Commit { get; init; } = string.Empty;
    public string CaptureSource { get; init; } = string.Empty;
    public string Health { get; init; } = string.Empty;
    public int StreamCount { get; init; }
}

public sealed record SvEvidenceComparisonSummary
{
    public int BaselineStreamCount { get; init; }
    public int CandidateStreamCount { get; init; }
    public int AddedStreamCount { get; init; }
    public int RemovedStreamCount { get; init; }
    public int ChangedStreamCount { get; init; }
    public int UnchangedStreamCount { get; init; }
    public int InfoChangeCount { get; init; }
    public int WarningChangeCount { get; init; }
    public int ErrorChangeCount { get; init; }
    public bool HasRegressions => WarningChangeCount > 0 || ErrorChangeCount > 0;
}

public sealed record SvSubscriberStreamComparison
{
    public string ComparisonKey { get; init; } = string.Empty;
    public string LogicalStreamKey { get; init; } = string.Empty;
    public SvEvidenceChangeKind Kind { get; init; }
    public SvEvidenceChangeSeverity Severity { get; init; }
    public string BaselineStreamKey { get; init; } = string.Empty;
    public string CandidateStreamKey { get; init; } = string.Empty;
    public SvSubscriberStreamIdentityEvidence Identity { get; init; } = new();
    public IReadOnlyList<SvEvidenceFieldChange> Changes { get; init; }
        = Array.Empty<SvEvidenceFieldChange>();
}

public sealed record SvEvidenceFieldChange
{
    public string Category { get; init; } = string.Empty;
    public string Field { get; init; } = string.Empty;
    public SvEvidenceChangeSeverity Severity { get; init; }
    public string Baseline { get; init; } = string.Empty;
    public string Candidate { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
}

public sealed class SvSubscriberEvidenceComparator
{
    private const double RateTolerancePercent = 1.0;

    public SvSubscriberEvidenceComparison Compare(
        SvSubscriberEvidenceReport baseline,
        SvSubscriberEvidenceReport candidate,
        DateTimeOffset generatedAt)
    {
        ArgumentNullException.ThrowIfNull(baseline);
        ArgumentNullException.ThrowIfNull(candidate);
        baseline.Validate();
        candidate.Validate();
        if (generatedAt == default)
            throw new ArgumentException("Comparison requires a generation timestamp.", nameof(generatedAt));

        var reportChanges = CompareReportMetadata(baseline, candidate);
        var streams = CompareStreams(baseline.Streams, candidate.Streams);
        var allChanges = reportChanges.Concat(streams.SelectMany(stream => stream.Changes)).ToArray();
        var comparison = new SvSubscriberEvidenceComparison
        {
            GeneratedAt = generatedAt,
            Baseline = Reference(baseline),
            Candidate = Reference(candidate),
            ReportChanges = reportChanges,
            Streams = streams,
            Summary = new SvEvidenceComparisonSummary
            {
                BaselineStreamCount = baseline.Streams.Count,
                CandidateStreamCount = candidate.Streams.Count,
                AddedStreamCount = streams.Count(stream => stream.Kind == SvEvidenceChangeKind.Added),
                RemovedStreamCount = streams.Count(stream => stream.Kind == SvEvidenceChangeKind.Removed),
                ChangedStreamCount = streams.Count(stream => stream.Kind == SvEvidenceChangeKind.Changed),
                UnchangedStreamCount = streams.Count(stream => stream.Kind == SvEvidenceChangeKind.Unchanged),
                InfoChangeCount = allChanges.Count(change => change.Severity == SvEvidenceChangeSeverity.Info),
                WarningChangeCount = allChanges.Count(change => change.Severity == SvEvidenceChangeSeverity.Warning),
                ErrorChangeCount = allChanges.Count(change => change.Severity == SvEvidenceChangeSeverity.Error)
            }
        };
        comparison.Validate();
        return comparison;
    }

    private static IReadOnlyList<SvEvidenceFieldChange> CompareReportMetadata(
        SvSubscriberEvidenceReport baseline,
        SvSubscriberEvidenceReport candidate)
    {
        var changes = new List<SvEvidenceFieldChange>();
        TextChange(changes, "Report", "Schema version", baseline.SchemaVersion, candidate.SchemaVersion,
            SvEvidenceChangeSeverity.Error, "Evidence schema changed; compatibility must be reviewed.");
        TextChange(changes, "Software", "Product", baseline.Software.Product, candidate.Software.Product,
            SvEvidenceChangeSeverity.Warning, "Product identity changed.");
        TextChange(changes, "Software", "Version", baseline.Software.Version, candidate.Software.Version,
            SvEvidenceChangeSeverity.Info, "Software version changed.");
        TextChange(changes, "Software", "Commit", baseline.Software.Commit, candidate.Software.Commit,
            SvEvidenceChangeSeverity.Info, "Build commit changed.");
        TextChange(changes, "Capture", "Source", baseline.Capture.Source, candidate.Capture.Source,
            SvEvidenceChangeSeverity.Info, "Capture source changed.");
        TextChange(changes, "Capture", "SCL path", baseline.Capture.SclPath, candidate.Capture.SclPath,
            SvEvidenceChangeSeverity.Info, "SCL source changed.");
        HealthChange(changes, "Report", baseline.Summary.Health, candidate.Summary.Health);
        return changes;
    }

    private static IReadOnlyList<SvSubscriberStreamComparison> CompareStreams(
        IReadOnlyList<SvSubscriberStreamEvidence> baseline,
        IReadOnlyList<SvSubscriberStreamEvidence> candidate)
    {
        var comparisons = new List<SvSubscriberStreamComparison>();
        var candidateByKey = candidate.ToDictionary(stream => stream.Key, StringComparer.Ordinal);
        var usedCandidateKeys = new HashSet<string>(StringComparer.Ordinal);
        var unmatchedBaseline = new List<SvSubscriberStreamEvidence>();

        foreach (var baselineStream in baseline)
        {
            if (candidateByKey.TryGetValue(baselineStream.Key, out var exact))
            {
                comparisons.Add(Pair(baselineStream, exact));
                usedCandidateKeys.Add(exact.Key);
            }
            else
            {
                unmatchedBaseline.Add(baselineStream);
            }
        }

        var unmatchedCandidate = candidate.Where(stream => !usedCandidateKeys.Contains(stream.Key)).ToArray();
        var baselineLogical = unmatchedBaseline.GroupBy(LogicalKey, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);
        var candidateLogical = unmatchedCandidate.GroupBy(LogicalKey, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);

        foreach (var baselineStream in unmatchedBaseline)
        {
            var logicalKey = LogicalKey(baselineStream);
            if (baselineLogical[logicalKey].Length == 1 &&
                candidateLogical.TryGetValue(logicalKey, out var candidates) &&
                candidates.Length == 1 &&
                usedCandidateKeys.Add(candidates[0].Key))
            {
                comparisons.Add(Pair(baselineStream, candidates[0]));
            }
            else
            {
                comparisons.Add(Removed(baselineStream));
            }
        }

        foreach (var candidateStream in candidate.Where(stream => !usedCandidateKeys.Contains(stream.Key)))
            comparisons.Add(Added(candidateStream));

        return comparisons.OrderBy(stream => stream.Identity.AppId)
            .ThenBy(stream => stream.Identity.SvId, StringComparer.Ordinal)
            .ThenBy(stream => stream.Kind)
            .ThenBy(stream => stream.ComparisonKey, StringComparer.Ordinal)
            .ToArray();
    }

    private static SvSubscriberStreamComparison Pair(
        SvSubscriberStreamEvidence baseline,
        SvSubscriberStreamEvidence candidate)
    {
        var changes = new List<SvEvidenceFieldChange>();
        HealthChange(changes, "Stream", baseline.Health, candidate.Health);
        TextChange(changes, "Identity", "Source MAC", baseline.Identity.SourceMac, candidate.Identity.SourceMac,
            SvEvidenceChangeSeverity.Info, "Publisher source MAC changed while logical identity remained stable.");
        UIntChange(changes, "Identity", "confRev", baseline.Identity.ConfigurationRevision,
            candidate.Identity.ConfigurationRevision, SvEvidenceChangeSeverity.Warning, "Configuration revision changed.");
        IntChange(changes, "Identity", "ASDU per frame", baseline.Identity.AsduPerFrame,
            candidate.Identity.AsduPerFrame, SvEvidenceChangeSeverity.Warning, "ASDU packing changed.");
        UShortChange(changes, "Identity", "Declared sample rate", baseline.Identity.DeclaredSampleRate,
            candidate.Identity.DeclaredSampleRate, SvEvidenceChangeSeverity.Warning, "Declared sample rate changed.");
        UShortChange(changes, "Identity", "Declared sample mode", baseline.Identity.DeclaredSampleMode,
            candidate.Identity.DeclaredSampleMode, SvEvidenceChangeSeverity.Warning, "Declared sample mode changed.");

        IssueCounter(changes, "Sequence gaps", baseline.Runtime.SequenceGapCount, candidate.Runtime.SequenceGapCount,
            SvEvidenceChangeSeverity.Warning);
        IssueCounter(changes, "Duplicates", baseline.Runtime.DuplicateCount, candidate.Runtime.DuplicateCount,
            SvEvidenceChangeSeverity.Warning);
        IssueCounter(changes, "Out-of-order", baseline.Runtime.OutOfOrderCount, candidate.Runtime.OutOfOrderCount,
            SvEvidenceChangeSeverity.Error);
        IssueCounter(changes, "Payload issues", baseline.Runtime.PayloadIssueCount, candidate.Runtime.PayloadIssueCount,
            SvEvidenceChangeSeverity.Error);
        IssueCounter(changes, "SCL mismatches", baseline.Runtime.SclMismatchCount, candidate.Runtime.SclMismatchCount,
            SvEvidenceChangeSeverity.Warning);

        RateChange(changes, "Observed frames/s", baseline.Observation.Facts.ObservedFramesPerSecond,
            candidate.Observation.Facts.ObservedFramesPerSecond);
        RateChange(changes, "Observed samples/s", baseline.Observation.Facts.ObservedSamplesPerSecond,
            candidate.Observation.Facts.ObservedSamplesPerSecond);
        WindowChanges(changes, baseline.Observation, candidate.Observation);
        BindingChanges(changes, baseline.Observation, candidate.Observation);
        ProfileChanges(changes, baseline.Observation.ProfileDetection, candidate.Observation.ProfileDetection);
        ConfigurationChanges(changes, baseline.Observation.ConfigurationComparison,
            candidate.Observation.ConfigurationComparison);
        FactChanges(changes, baseline.Observation.Facts, candidate.Observation.Facts);
        DiagnosticChanges(changes,
            baseline.Diagnostics.Concat(baseline.Observation.Diagnostics),
            candidate.Diagnostics.Concat(candidate.Observation.Diagnostics));

        var logicalKey = LogicalKey(candidate);
        return new SvSubscriberStreamComparison
        {
            ComparisonKey = $"PAIR|{baseline.Key}|{candidate.Key}",
            LogicalStreamKey = logicalKey,
            Kind = changes.Count == 0 ? SvEvidenceChangeKind.Unchanged : SvEvidenceChangeKind.Changed,
            Severity = changes.Select(change => change.Severity)
                .DefaultIfEmpty(SvEvidenceChangeSeverity.Info).Max(),
            BaselineStreamKey = baseline.Key,
            CandidateStreamKey = candidate.Key,
            Identity = candidate.Identity,
            Changes = changes
        };
    }

    private static void WindowChanges(
        ICollection<SvEvidenceFieldChange> changes,
        SvSubscriberObservationEvidence baseline,
        SvSubscriberObservationEvidence candidate)
    {
        if (baseline.WindowSamples != candidate.WindowSamples)
        {
            var severity = baseline.WindowSamples > 0 && candidate.WindowSamples < baseline.WindowSamples / 2
                ? SvEvidenceChangeSeverity.Warning
                : SvEvidenceChangeSeverity.Info;
            Change(changes, "Observation window", "Samples", severity,
                baseline.WindowSamples.ToString(CultureInfo.InvariantCulture),
                candidate.WindowSamples.ToString(CultureInfo.InvariantCulture),
                severity == SvEvidenceChangeSeverity.Warning
                    ? "Candidate observation window contains materially fewer samples."
                    : "Observation-window sample count changed.");
        }

        if (!WithinPercent(baseline.WindowDurationSeconds, candidate.WindowDurationSeconds, 0.01))
        {
            Change(changes, "Observation window", "Duration", SvEvidenceChangeSeverity.Info,
                Number(baseline.WindowDurationSeconds), Number(candidate.WindowDurationSeconds),
                "Observation-window duration changed.");
        }

        TextChange(changes, "Observation window", "Input kinds",
            string.Join(", ", baseline.InputKinds), string.Join(", ", candidate.InputKinds),
            SvEvidenceChangeSeverity.Info, "Observation input provenance changed.");
    }

    private static void BindingChanges(
        ICollection<SvEvidenceFieldChange> changes,
        SvSubscriberObservationEvidence baseline,
        SvSubscriberObservationEvidence candidate)
    {
        if (baseline.IsBoundToScl != candidate.IsBoundToScl)
        {
            var severity = baseline.IsBoundToScl && !candidate.IsBoundToScl
                ? SvEvidenceChangeSeverity.Warning
                : SvEvidenceChangeSeverity.Info;
            Change(changes, "SCL", "Binding", severity,
                baseline.IsBoundToScl ? "bound" : "not bound",
                candidate.IsBoundToScl ? "bound" : "not bound",
                severity == SvEvidenceChangeSeverity.Warning
                    ? "Candidate stream lost its SCL binding."
                    : "Candidate stream gained an SCL binding.");
        }

        TextChange(changes, "SCL", "Control block", baseline.ControlBlockReference,
            candidate.ControlBlockReference, SvEvidenceChangeSeverity.Info,
            "SCL control-block reference changed.");
    }

    private static void ProfileChanges(
        ICollection<SvEvidenceFieldChange> changes,
        SvProfileDetectionResult? baseline,
        SvProfileDetectionResult? candidate)
    {
        TextChange(changes, "Profile", "Profile ID", baseline?.Profile.Id, candidate?.Profile.Id,
            SvEvidenceChangeSeverity.Warning, "Detected profile changed.");

        var baselineConfidence = baseline?.Confidence ?? SvProfileConfidence.Unknown;
        var candidateConfidence = candidate?.Confidence ?? SvProfileConfidence.Unknown;
        if (baselineConfidence == candidateConfidence)
            return;

        var severity = candidateConfidence == SvProfileConfidence.Conflict
            ? SvEvidenceChangeSeverity.Error
            : ConfidenceRank(candidateConfidence) < ConfidenceRank(baselineConfidence)
                ? SvEvidenceChangeSeverity.Warning
                : SvEvidenceChangeSeverity.Info;
        Change(changes, "Profile", "Confidence", severity,
            baselineConfidence.ToString(), candidateConfidence.ToString(),
            severity == SvEvidenceChangeSeverity.Error
                ? "Candidate profile classification conflicts with observed evidence."
                : severity == SvEvidenceChangeSeverity.Warning
                    ? "Candidate profile confidence decreased."
                    : "Candidate profile confidence improved.");
    }

    private static void ConfigurationChanges(
        ICollection<SvEvidenceFieldChange> changes,
        SvConfigurationComparisonResult? baseline,
        SvConfigurationComparisonResult? candidate)
    {
        var baselineSummary = baseline?.Summary ?? "Not configured";
        var candidateSummary = candidate?.Summary ?? "Not configured";
        if (string.Equals(baselineSummary, candidateSummary, StringComparison.Ordinal) &&
            baseline?.Mode == candidate?.Mode)
            return;

        var blockingIntroduced = candidate?.HasBlockingErrors == true && baseline?.HasBlockingErrors != true;
        var warningsIncreased = (candidate?.WarningCount ?? 0) > (baseline?.WarningCount ?? 0);
        var severity = blockingIntroduced
            ? SvEvidenceChangeSeverity.Error
            : warningsIncreased || (baseline is not null && candidate is null)
                ? SvEvidenceChangeSeverity.Warning
                : SvEvidenceChangeSeverity.Info;
        Change(changes, "Configuration", "Comparison", severity, baselineSummary, candidateSummary,
            blockingIntroduced
                ? "Candidate introduced blocking configuration errors."
                : severity == SvEvidenceChangeSeverity.Warning
                    ? "Candidate configuration evidence regressed."
                    : "Configuration comparison result changed.");
    }

    private static void FactChanges(
        ICollection<SvEvidenceFieldChange> changes,
        SvObservedStreamFacts baseline,
        SvObservedStreamFacts candidate)
    {
        IntChange(changes, "Observed facts", "Payload bytes per ASDU", baseline.PayloadBytesPerAsdu,
            candidate.PayloadBytesPerAsdu, SvEvidenceChangeSeverity.Warning, "Observed payload length changed.");
        IntChange(changes, "Observed facts", "Counter wrap", baseline.ObservedCounterWrap,
            candidate.ObservedCounterWrap, SvEvidenceChangeSeverity.Warning, "Observed sample-counter wrap changed.");
        DoubleChange(changes, "Observed facts", "Nominal frequency", baseline.NominalFrequencyHz,
            candidate.NominalFrequencyHz, SvEvidenceChangeSeverity.Warning, "Nominal-frequency context changed.");
        TextChange(changes, "Observed facts", "Dataset signature", Signature(baseline.DataSetSignature),
            Signature(candidate.DataSetSignature), SvEvidenceChangeSeverity.Error,
            "Observed dataset element order or types changed.");

        foreach (var key in baseline.Provenance.Keys.Concat(candidate.Provenance.Keys)
                     .Distinct(StringComparer.Ordinal))
        {
            var baselineSource = baseline.Provenance.TryGetValue(key, out var b) ? b : SvFactSource.Unknown;
            var candidateSource = candidate.Provenance.TryGetValue(key, out var c) ? c : SvFactSource.Unknown;
            if (baselineSource != candidateSource)
            {
                Change(changes, "Provenance", key, SvEvidenceChangeSeverity.Info,
                    baselineSource.ToString(), candidateSource.ToString(), "Fact provenance changed.");
            }
        }
    }

    private static void DiagnosticChanges(
        ICollection<SvEvidenceFieldChange> changes,
        IEnumerable<string> baseline,
        IEnumerable<string> candidate)
    {
        var baselineSet = baseline.Where(value => !string.IsNullOrWhiteSpace(value))
            .ToHashSet(StringComparer.Ordinal);
        var candidateSet = candidate.Where(value => !string.IsNullOrWhiteSpace(value))
            .ToHashSet(StringComparer.Ordinal);

        foreach (var value in candidateSet.Except(baselineSet, StringComparer.Ordinal)
                     .OrderBy(value => value, StringComparer.Ordinal))
        {
            Change(changes, "Diagnostics", "Added", SvEvidenceChangeSeverity.Warning,
                "-", value, "Candidate introduced a diagnostic.");
        }

        foreach (var value in baselineSet.Except(candidateSet, StringComparer.Ordinal)
                     .OrderBy(value => value, StringComparer.Ordinal))
        {
            Change(changes, "Diagnostics", "Resolved", SvEvidenceChangeSeverity.Info,
                value, "-", "A baseline diagnostic is no longer present.");
        }
    }

    private static void IssueCounter(
        ICollection<SvEvidenceFieldChange> changes,
        string field,
        int baseline,
        int candidate,
        SvEvidenceChangeSeverity regressionSeverity)
    {
        if (baseline == candidate)
            return;
        var severity = candidate > baseline ? regressionSeverity : SvEvidenceChangeSeverity.Info;
        Change(changes, "Runtime integrity", field, severity,
            baseline.ToString(CultureInfo.InvariantCulture), candidate.ToString(CultureInfo.InvariantCulture),
            candidate > baseline ? $"Candidate {field.ToLowerInvariant()} increased." : $"Candidate {field.ToLowerInvariant()} decreased.");
    }

    private static void RateChange(
        ICollection<SvEvidenceFieldChange> changes,
        string field,
        double? baseline,
        double? candidate)
    {
        if (!baseline.HasValue && !candidate.HasValue)
            return;
        if (!baseline.HasValue || !candidate.HasValue)
        {
            Change(changes, "Observed rate", field, SvEvidenceChangeSeverity.Warning,
                Number(baseline), Number(candidate), "Observed rate availability changed.");
            return;
        }
        if (WithinPercent(baseline.Value, candidate.Value, RateTolerancePercent))
            return;

        Change(changes, "Observed rate", field, SvEvidenceChangeSeverity.Warning,
            Number(baseline), Number(candidate),
            $"Observed rate changed by more than {RateTolerancePercent:0.###}%.");
    }

    private static void HealthChange(
        ICollection<SvEvidenceFieldChange> changes,
        string category,
        string? baseline,
        string? candidate)
    {
        if (string.Equals(baseline, candidate, StringComparison.OrdinalIgnoreCase))
            return;
        var candidateRank = HealthRank(candidate);
        var severity = candidateRank > HealthRank(baseline)
            ? candidateRank >= 2 ? SvEvidenceChangeSeverity.Error : SvEvidenceChangeSeverity.Warning
            : SvEvidenceChangeSeverity.Info;
        Change(changes, category, "Health", severity, Text(baseline), Text(candidate),
            severity == SvEvidenceChangeSeverity.Info ? "Health improved or changed without regression." : "Health regressed.");
    }

    private static void TextChange(
        ICollection<SvEvidenceFieldChange> changes,
        string category,
        string field,
        string? baseline,
        string? candidate,
        SvEvidenceChangeSeverity severity,
        string message)
    {
        if (!string.Equals(baseline ?? string.Empty, candidate ?? string.Empty, StringComparison.Ordinal))
            Change(changes, category, field, severity, Text(baseline), Text(candidate), message);
    }

    private static void IntChange(
        ICollection<SvEvidenceFieldChange> changes,
        string category,
        string field,
        int? baseline,
        int? candidate,
        SvEvidenceChangeSeverity severity,
        string message)
    {
        if (baseline != candidate)
            Change(changes, category, field, severity, Value(baseline), Value(candidate), message);
    }

    private static void UIntChange(
        ICollection<SvEvidenceFieldChange> changes,
        string category,
        string field,
        uint? baseline,
        uint? candidate,
        SvEvidenceChangeSeverity severity,
        string message)
    {
        if (baseline != candidate)
            Change(changes, category, field, severity, Value(baseline), Value(candidate), message);
    }

    private static void UShortChange(
        ICollection<SvEvidenceFieldChange> changes,
        string category,
        string field,
        ushort? baseline,
        ushort? candidate,
        SvEvidenceChangeSeverity severity,
        string message)
    {
        if (baseline != candidate)
            Change(changes, category, field, severity, Value(baseline), Value(candidate), message);
    }

    private static void DoubleChange(
        ICollection<SvEvidenceFieldChange> changes,
        string category,
        string field,
        double? baseline,
        double? candidate,
        SvEvidenceChangeSeverity severity,
        string message)
    {
        if (baseline != candidate)
            Change(changes, category, field, severity, Number(baseline), Number(candidate), message);
    }

    private static void Change(
        ICollection<SvEvidenceFieldChange> changes,
        string category,
        string field,
        SvEvidenceChangeSeverity severity,
        string baseline,
        string candidate,
        string message)
        => changes.Add(new SvEvidenceFieldChange
        {
            Category = category,
            Field = field,
            Severity = severity,
            Baseline = baseline,
            Candidate = candidate,
            Message = message
        });

    private static SvSubscriberStreamComparison Added(SvSubscriberStreamEvidence stream)
    {
        var logicalKey = LogicalKey(stream);
        return new SvSubscriberStreamComparison
        {
            ComparisonKey = $"ADD|{stream.Key}",
            LogicalStreamKey = logicalKey,
            Kind = SvEvidenceChangeKind.Added,
            Severity = SvEvidenceChangeSeverity.Info,
            CandidateStreamKey = stream.Key,
            Identity = stream.Identity,
            Changes =
            [
                new SvEvidenceFieldChange
                {
                    Category = "Stream",
                    Field = "Presence",
                    Severity = SvEvidenceChangeSeverity.Info,
                    Baseline = "absent",
                    Candidate = "present",
                    Message = "Candidate contains a new logical stream."
                }
            ]
        };
    }

    private static SvSubscriberStreamComparison Removed(SvSubscriberStreamEvidence stream)
    {
        var logicalKey = LogicalKey(stream);
        return new SvSubscriberStreamComparison
        {
            ComparisonKey = $"REMOVE|{stream.Key}",
            LogicalStreamKey = logicalKey,
            Kind = SvEvidenceChangeKind.Removed,
            Severity = SvEvidenceChangeSeverity.Error,
            BaselineStreamKey = stream.Key,
            Identity = stream.Identity,
            Changes =
            [
                new SvEvidenceFieldChange
                {
                    Category = "Stream",
                    Field = "Presence",
                    Severity = SvEvidenceChangeSeverity.Error,
                    Baseline = "present",
                    Candidate = "absent",
                    Message = "Candidate no longer contains this logical stream."
                }
            ]
        };
    }

    private static SvEvidenceReportReference Reference(SvSubscriberEvidenceReport report)
        => new()
        {
            SchemaVersion = report.SchemaVersion,
            GeneratedAt = report.GeneratedAt,
            Product = report.Software.Product,
            Version = report.Software.Version,
            Commit = report.Software.Commit,
            CaptureSource = report.Capture.Source,
            Health = report.Summary.Health,
            StreamCount = report.Streams.Count
        };

    private static string LogicalKey(SvSubscriberStreamEvidence stream)
    {
        var identity = stream.Identity;
        var vlan = identity.VlanId?.ToString(CultureInfo.InvariantCulture) ?? "-";
        return $"SV|{identity.AppId:X4}|{NormalizeMac(identity.DestinationMac)}|{vlan}|" +
               $"{NormalizeId(identity.SvId)}|{NormalizeId(identity.DataSetReference)}";
    }

    private static string NormalizeMac(string? value)
        => new((value ?? string.Empty).Where(Uri.IsHexDigit).Select(char.ToUpperInvariant).ToArray());

    private static string NormalizeId(string? value)
        => (value ?? string.Empty).Trim().ToUpperInvariant();

    private static int HealthRank(string? value)
        => (value ?? string.Empty).Trim().ToUpperInvariant() switch
        {
            "GOOD" => 0,
            "IDLE" => 0,
            "LISTENING" => 0,
            "WARN" => 1,
            "BAD" => 2,
            "ERROR" => 3,
            _ => 1
        };

    private static int ConfidenceRank(SvProfileConfidence value)
        => value switch
        {
            SvProfileConfidence.Confirmed => 4,
            SvProfileConfidence.Likely => 3,
            SvProfileConfidence.Possible => 2,
            SvProfileConfidence.Unknown => 1,
            SvProfileConfidence.Conflict => 0,
            _ => 0
        };

    private static bool WithinPercent(double baseline, double candidate, double tolerancePercent)
    {
        if (baseline == 0)
            return candidate == 0;
        return Math.Abs(candidate - baseline) / Math.Abs(baseline) * 100 <= tolerancePercent;
    }

    private static string Signature(IReadOnlyList<SvDatasetElementSignature> signature)
        => signature.Count == 0
            ? string.Empty
            : string.Join(",", signature.Select(item =>
                $"{item.NormalizedBType}|{item.NormalizedCdc}|{item.IsQuality}|{item.IsTimestamp}"));

    private static string Number(double? value)
        => value.HasValue ? Number(value.Value) : "unknown";

    private static string Number(double value)
        => value.ToString("0.###", CultureInfo.InvariantCulture);

    private static string Value<T>(T? value) where T : struct
        => value.HasValue ? Convert.ToString(value.Value, CultureInfo.InvariantCulture) ?? "unknown" : "unknown";

    private static string Text(string? value)
        => string.IsNullOrWhiteSpace(value) ? "unknown" : value;
}
