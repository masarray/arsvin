using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
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
        if (Streams.Select(stream => stream.ComparisonKey).Distinct(StringComparer.Ordinal).Count() != Streams.Count)
            throw new InvalidOperationException("SV comparison stream keys must be unique.");

        var classified = Summary.AddedStreamCount + Summary.RemovedStreamCount +
                         Summary.ChangedStreamCount + Summary.UnchangedStreamCount;
        if (classified != Streams.Count)
            throw new InvalidOperationException("SV comparison summary does not match the stream comparison collection.");

        var allChanges = ReportChanges.Concat(Streams.SelectMany(stream => stream.Changes)).ToArray();
        if (Summary.InfoChangeCount != allChanges.Count(change => change.Severity == SvEvidenceChangeSeverity.Info) ||
            Summary.WarningChangeCount != allChanges.Count(change => change.Severity == SvEvidenceChangeSeverity.Warning) ||
            Summary.ErrorChangeCount != allChanges.Count(change => change.Severity == SvEvidenceChangeSeverity.Error))
        {
            throw new InvalidOperationException("SV comparison severity totals do not match the comparison evidence.");
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
        var streamComparisons = CompareStreams(baseline.Streams, candidate.Streams);
        var allChanges = reportChanges.Concat(streamComparisons.SelectMany(stream => stream.Changes)).ToArray();

        var result = new SvSubscriberEvidenceComparison
        {
            GeneratedAt = generatedAt,
            Baseline = ToReference(baseline),
            Candidate = ToReference(candidate),
            Summary = new SvEvidenceComparisonSummary
            {
                BaselineStreamCount = baseline.Streams.Count,
                CandidateStreamCount = candidate.Streams.Count,
                AddedStreamCount = streamComparisons.Count(stream => stream.Kind == SvEvidenceChangeKind.Added),
                RemovedStreamCount = streamComparisons.Count(stream => stream.Kind == SvEvidenceChangeKind.Removed),
                ChangedStreamCount = streamComparisons.Count(stream => stream.Kind == SvEvidenceChangeKind.Changed),
                UnchangedStreamCount = streamComparisons.Count(stream => stream.Kind == SvEvidenceChangeKind.Unchanged),
                InfoChangeCount = allChanges.Count(change => change.Severity == SvEvidenceChangeSeverity.Info),
                WarningChangeCount = allChanges.Count(change => change.Severity == SvEvidenceChangeSeverity.Warning),
                ErrorChangeCount = allChanges.Count(change => change.Severity == SvEvidenceChangeSeverity.Error)
            },
            ReportChanges = reportChanges,
            Streams = streamComparisons
        };
        result.Validate();
        return result;
    }

    private static IReadOnlyList<SvEvidenceFieldChange> CompareReportMetadata(
        SvSubscriberEvidenceReport baseline,
        SvSubscriberEvidenceReport candidate)
    {
        var changes = new List<SvEvidenceFieldChange>();
        AddTextChange(changes, "Report", "Schema version", baseline.SchemaVersion, candidate.SchemaVersion,
            SvEvidenceChangeSeverity.Error, "Evidence schema changed; compatibility must be reviewed.");
        AddTextChange(changes, "Software", "Product", baseline.Software.Product, candidate.Software.Product,
            SvEvidenceChangeSeverity.Warning, "Product identity changed between reports.");
        AddTextChange(changes, "Software", "Version", baseline.Software.Version, candidate.Software.Version,
            SvEvidenceChangeSeverity.Info, "Software version changed.");
        AddTextChange(changes, "Software", "Commit", baseline.Software.Commit, candidate.Software.Commit,
            SvEvidenceChangeSeverity.Info, "Build commit changed.");
        AddTextChange(changes, "Capture", "Source", baseline.Capture.Source, candidate.Capture.Source,
            SvEvidenceChangeSeverity.Info, "Capture source changed.");
        AddTextChange(changes, "Capture", "SCL path", baseline.Capture.SclPath, candidate.Capture.SclPath,
            SvEvidenceChangeSeverity.Info, "SCL source changed.");
        AddHealthChange(changes, "Report", baseline.Summary.Health, candidate.Summary.Health);
        return changes;
    }

    private static IReadOnlyList<SvSubscriberStreamComparison> CompareStreams(
        IReadOnlyList<SvSubscriberStreamEvidence> baselineStreams,
        IReadOnlyList<SvSubscriberStreamEvidence> candidateStreams)
    {
        var result = new List<SvSubscriberStreamComparison>();
        var candidateByKey = candidateStreams.ToDictionary(stream => stream.Key, StringComparer.Ordinal);
        var usedCandidateKeys = new HashSet<string>(StringComparer.Ordinal);
        var unmatchedBaseline = new List<SvSubscriberStreamEvidence>();

        foreach (var baseline in baselineStreams)
        {
            if (candidateByKey.TryGetValue(baseline.Key, out var exact))
            {
                result.Add(ComparePair(baseline, exact));
                usedCandidateKeys.Add(exact.Key);
            }
            else
            {
                unmatchedBaseline.Add(baseline);
            }
        }

        var unmatchedCandidate = candidateStreams
            .Where(stream => !usedCandidateKeys.Contains(stream.Key))
            .ToArray();
        var baselineLogicalGroups = unmatchedBaseline.GroupBy(LogicalKey, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);
        var candidateLogicalGroups = unmatchedCandidate.GroupBy(LogicalKey, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);

        foreach (var baseline in unmatchedBaseline)
        {
            var logicalKey = LogicalKey(baseline);
            if (baselineLogicalGroups[logicalKey].Length == 1 &&
                candidateLogicalGroups.TryGetValue(logicalKey, out var candidates) &&
                candidates.Length == 1 &&
                usedCandidateKeys.Add(candidates[0].Key))
            {
                result.Add(ComparePair(baseline, candidates[0]));
                continue;
            }

            result.Add(Removed(baseline));
        }

        foreach (var candidate in candidateStreams.Where(stream => !usedCandidateKeys.Contains(stream.Key)))
            result.Add(Added(candidate));

        return result
            .OrderBy(stream => stream.Identity.AppId)
            .ThenBy(stream => stream.Identity.SvId, StringComparer.Ordinal)
            .ThenBy(stream => stream.Kind)
            .ToArray();
    }

    private static SvSubscriberStreamComparison ComparePair(
        SvSubscriberStreamEvidence baseline,
        SvSubscriberStreamEvidence candidate)
    {
        var changes = new List<SvEvidenceFieldChange>();
        AddHealthChange(changes, "Stream", baseline.Health, candidate.Health);
        AddTextChange(changes, "Identity", "Source MAC", baseline.Identity.SourceMac, candidate.Identity.SourceMac,
            SvEvidenceChangeSeverity.Info, "Publisher source MAC changed while the logical stream identity remained stable.");
        AddNullableChange(changes, "Identity", "confRev", baseline.Identity.ConfigurationRevision,
            candidate.Identity.ConfigurationRevision, SvEvidenceChangeSeverity.Warning, "Configuration revision changed.");
        AddNullableChange(changes, "Identity", "ASDU per frame", baseline.Identity.AsduPerFrame,
            candidate.Identity.AsduPerFrame, SvEvidenceChangeSeverity.Warning, "ASDU packing changed.");
        AddNullableChange(changes, "Identity", "Declared sample rate", baseline.Identity.DeclaredSampleRate,
            candidate.Identity.DeclaredSampleRate, SvEvidenceChangeSeverity.Warning, "Declared sample rate changed.");
        AddNullableChange(changes, "Identity", "Declared sample mode", baseline.Identity.DeclaredSampleMode,
            candidate.Identity.DeclaredSampleMode, SvEvidenceChangeSeverity.Warning, "Declared sample mode changed.");

        CompareIssueCounter(changes, "Sequence gaps", baseline.Runtime.SequenceGapCount, candidate.Runtime.SequenceGapCount,
            SvEvidenceChangeSeverity.Warning);
        CompareIssueCounter(changes, "Duplicates", baseline.Runtime.DuplicateCount, candidate.Runtime.DuplicateCount,
            SvEvidenceChangeSeverity.Warning);
        CompareIssueCounter(changes, "Out-of-order", baseline.Runtime.OutOfOrderCount, candidate.Runtime.OutOfOrderCount,
            SvEvidenceChangeSeverity.Error);
        CompareIssueCounter(changes, "Payload issues", baseline.Runtime.PayloadIssueCount, candidate.Runtime.PayloadIssueCount,
            SvEvidenceChangeSeverity.Error);
        CompareIssueCounter(changes, "SCL mismatches", baseline.Runtime.SclMismatchCount, candidate.Runtime.SclMismatchCount,
            SvEvidenceChangeSeverity.Warning);
        CompareRate(changes, "Observed frames/s", baseline.Observation.Facts.ObservedFramesPerSecond,
            candidate.Observation.Facts.ObservedFramesPerSecond);
        CompareRate(changes, "Observed samples/s", baseline.Observation.Facts.ObservedSamplesPerSecond,
            candidate.Observation.Facts.ObservedSamplesPerSecond);
        CompareWindow(changes, baseline.Observation, candidate.Observation);
        CompareBinding(changes, baseline.Observation, candidate.Observation);
        CompareProfile(changes, baseline.Observation.ProfileDetection, candidate.Observation.ProfileDetection);
        CompareConfiguration(changes, baseline.Observation.ConfigurationComparison,
            candidate.Observation.ConfigurationComparison);
        CompareFacts(changes, baseline.Observation.Facts, candidate.Observation.Facts);
        CompareDiagnostics(changes, baseline.Diagnostics.Concat(baseline.Observation.Diagnostics),
            candidate.Diagnostics.Concat(candidate.Observation.Diagnostics));

        var kind = changes.Count == 0 ? SvEvidenceChangeKind.Unchanged : SvEvidenceChangeKind.Changed;
        return new SvSubscriberStreamComparison
        {
            ComparisonKey = LogicalKey(candidate),
            Kind = kind,
            Severity = MaximumSeverity(changes),
            BaselineStreamKey = baseline.Key,
            CandidateStreamKey = candidate.Key,
            Identity = candidate.Identity,
            Changes = changes
        };
    }

    private static void CompareWindow(
        ICollection<SvEvidenceFieldChange> changes,
        SvSubscriberObservationEvidence baseline,
        SvSubscriberObservationEvidence candidate)
    {
        if (baseline.WindowSamples != candidate.WindowSamples)
        {
            var severity = baseline.WindowSamples > 0 && candidate.WindowSamples < baseline.WindowSamples / 2
                ? SvEvidenceChangeSeverity.Warning
                : SvEvidenceChangeSeverity.Info;
            Add(changes, "Observation window", "Samples", severity,
                baseline.WindowSamples.ToString(CultureInfo.InvariantCulture),
                candidate.WindowSamples.ToString(CultureInfo.InvariantCulture),
                severity == SvEvidenceChangeSeverity.Warning
                    ? "Candidate observation window contains materially fewer samples."
                    : "Observation-window sample count changed.");
        }

        if (!ApproximatelyEqual(baseline.WindowDurationSeconds, candidate.WindowDurationSeconds, 0.01))
        {
            Add(changes, "Observation window", "Duration", SvEvidenceChangeSeverity.Info,
                Number(baseline.WindowDurationSeconds), Number(candidate.WindowDurationSeconds),
                "Observation-window duration changed.");
        }
    }

    private static void CompareBinding(
        ICollection<SvEvidenceFieldChange> changes,
        SvSubscriberObservationEvidence baseline,
        SvSubscriberObservationEvidence candidate)
    {
        if (baseline.IsBoundToScl != candidate.IsBoundToScl)
        {
            var severity = baseline.IsBoundToScl && !candidate.IsBoundToScl
                ? SvEvidenceChangeSeverity.Warning
                : SvEvidenceChangeSeverity.Info;
            Add(changes, "SCL", "Binding", severity,
                baseline.IsBoundToScl ? "bound" : "not bound",
                candidate.IsBoundToScl ? "bound" : "not bound",
                severity == SvEvidenceChangeSeverity.Warning
                    ? "Candidate stream lost its SCL binding."
                    : "Candidate stream gained an SCL binding.");
        }

        AddTextChange(changes, "SCL", "Control block", baseline.ControlBlockReference,
            candidate.ControlBlockReference, SvEvidenceChangeSeverity.Info,
            "SCL control-block reference changed.");
    }

    private static void CompareProfile(
        ICollection<SvEvidenceFieldChange> changes,
        SvProfileDetectionResult? baseline,
        SvProfileDetectionResult? candidate)
    {
        AddTextChange(changes, "Profile", "Profile ID", baseline?.Profile.Id ?? string.Empty,
            candidate?.Profile.Id ?? string.Empty, SvEvidenceChangeSeverity.Warning,
            "Detected profile changed.");

        var baselineConfidence = baseline?.Confidence ?? SvProfileConfidence.Unknown;
        var candidateConfidence = candidate?.Confidence ?? SvProfileConfidence.Unknown;
        if (baselineConfidence == candidateConfidence)
            return;

        var severity = candidateConfidence == SvProfileConfidence.Conflict
            ? SvEvidenceChangeSeverity.Error
            : ConfidenceRank(candidateConfidence) < ConfidenceRank(baselineConfidence)
                ? SvEvidenceChangeSeverity.Warning
                : SvEvidenceChangeSeverity.Info;
        Add(changes, "Profile", "Confidence", severity, baselineConfidence.ToString(),
            candidateConfidence.ToString(),
            severity == SvEvidenceChangeSeverity.Error
                ? "Candidate profile classification is conflicting."
                : severity == SvEvidenceChangeSeverity.Warning
                    ? "Candidate profile confidence decreased."
                    : "Candidate profile confidence improved.");
    }

    private static void CompareConfiguration(
        ICollection<SvEvidenceFieldChange> changes,
        SvConfigurationComparisonResult? baseline,
        SvConfigurationComparisonResult? candidate)
    {
        var baselineSummary = baseline?.Summary ?? "Not configured";
        var candidateSummary = candidate?.Summary ?? "Not configured";
        if (string.Equals(baselineSummary, candidateSummary, StringComparison.Ordinal) &&
            baseline?.Mode == candidate?.Mode)
            return;

        var introducedBlocking = candidate?.HasBlockingErrors == true && baseline?.HasBlockingErrors != true;
        var warningIncrease = (candidate?.WarningCount ?? 0) > (baseline?.WarningCount ?? 0);
        var severity = introducedBlocking
            ? SvEvidenceChangeSeverity.Error
            : warningIncrease || (baseline is not null && candidate is null)
                ? SvEvidenceChangeSeverity.Warning
                : SvEvidenceChangeSeverity.Info;
        Add(changes, "Configuration", "Comparison", severity, baselineSummary, candidateSummary,
            introducedBlocking
                ? "Candidate introduced blocking configuration errors."
                : severity == SvEvidenceChangeSeverity.Warning
                    ? "Candidate configuration evidence regressed."
                    : "Configuration comparison result changed.");
    }

    private static void CompareFacts(
        ICollection<SvEvidenceFieldChange> changes,
        SvObservedStreamFacts baseline,
        SvObservedStreamFacts candidate)
    {
        AddNullableChange(changes, "Observed facts", "Payload bytes per ASDU",
            baseline.PayloadBytesPerAsdu, candidate.PayloadBytesPerAsdu,
            SvEvidenceChangeSeverity.Warning, "Observed payload length changed.");
        AddNullableChange(changes, "Observed facts", "Counter wrap", baseline.ObservedCounterWrap,
            candidate.ObservedCounterWrap, SvEvidenceChangeSeverity.Warning,
            "Observed sample-counter wrap changed.");
        AddNullableChange(changes, "Observed facts", "Nominal frequency", baseline.NominalFrequencyHz,
            candidate.NominalFrequencyHz, SvEvidenceChangeSeverity.Warning,
            "Nominal-frequency context changed.");

        var baselineSignature = Signature(baseline.DataSetSignature);
        var candidateSignature = Signature(candidate.DataSetSignature);
        AddTextChange(changes, "Observed facts", "Dataset signature", baselineSignature,
            candidateSignature, SvEvidenceChangeSeverity.Error,
            "Observed dataset element order or types changed.");

        var provenanceKeys = baseline.Provenance.Keys.Concat(candidate.Provenance.Keys)
            .Distinct(StringComparer.Ordinal);
        foreach (var key in provenanceKeys)
        {
            var baselineSource = baseline.Provenance.TryGetValue(key, out var b) ? b : SvFactSource.Unknown;
            var candidateSource = candidate.Provenance.TryGetValue(key, out var c) ? c : SvFactSource.Unknown;
            if (baselineSource != candidateSource)
            {
                Add(changes, "Provenance", key, SvEvidenceChangeSeverity.Info,
                    baselineSource.ToString(), candidateSource.ToString(),
                    "Fact provenance changed.");
            }
        }
    }

    private static void CompareDiagnostics(
        ICollection<SvEvidenceFieldChange> changes,
        IEnumerable<string> baseline,
        IEnumerable<string> candidate)
    {
        var baselineSet = baseline.Where(item => !string.IsNullOrWhiteSpace(item))
            .ToHashSet(StringComparer.Ordinal);
        var candidateSet = candidate.Where(item => !string.IsNullOrWhiteSpace(item))
            .ToHashSet(StringComparer.Ordinal);

        foreach (var added in candidateSet.Except(baselineSet, StringComparer.Ordinal).Order(StringComparer.Ordinal))
            Add(changes, "Diagnostics", "Added", SvEvidenceChangeSeverity.Warning, "-", added,
                "Candidate introduced a diagnostic.");
        foreach (var removed in baselineSet.Except(candidateSet, StringComparer.Ordinal).Order(StringComparer.Ordinal))
            Add(changes, "Diagnostics", "Resolved", SvEvidenceChangeSeverity.Info, removed, "-",
                "A baseline diagnostic is no longer present.");
    }

    private static void CompareIssueCounter(
        ICollection<SvEvidenceFieldChange> changes,
        string field,
        int baseline,
        int candidate,
        SvEvidenceChangeSeverity regressionSeverity)
    {
        if (baseline == candidate)
            return;
        var severity = candidate > baseline ? regressionSeverity : SvEvidenceChangeSeverity.Info;
        Add(changes, "Runtime integrity", field, severity,
            baseline.ToString(CultureInfo.InvariantCulture),
            candidate.ToString(CultureInfo.InvariantCulture),
            candidate > baseline ? $"Candidate {field.ToLowerInvariant()} increased." : $"Candidate {field.ToLowerInvariant()} decreased.");
    }

    private static void CompareRate(
        ICollection<SvEvidenceFieldChange> changes,
        string field,
        double? baseline,
        double? candidate)
    {
        if (!baseline.HasValue && !candidate.HasValue)
            return;
        if (!baseline.HasValue || !candidate.HasValue)
        {
            Add(changes, "Observed rate", field, SvEvidenceChangeSeverity.Warning,
                Number(baseline), Number(candidate), "Observed rate availability changed.");
            return;
        }
        if (ApproximatelyEqual(baseline.Value, candidate.Value, RateTolerancePercent))
            return;

        Add(changes, "Observed rate", field, SvEvidenceChangeSeverity.Warning,
            Number(baseline), Number(candidate),
            $"Observed rate changed by more than {RateTolerancePercent:0.###}%.");
    }

    private static void AddHealthChange(
        ICollection<SvEvidenceFieldChange> changes,
        string category,
        string baseline,
        string candidate)
    {
        if (string.Equals(baseline, candidate, StringComparison.OrdinalIgnoreCase))
            return;
        var severity = HealthRank(candidate) > HealthRank(baseline)
            ? HealthRank(candidate) >= 2 ? SvEvidenceChangeSeverity.Error : SvEvidenceChangeSeverity.Warning
            : SvEvidenceChangeSeverity.Info;
        Add(changes, category, "Health", severity, baseline, candidate,
            severity == SvEvidenceChangeSeverity.Info ? "Health improved or changed without regression." : "Health regressed.");
    }

    private static void AddTextChange(
        ICollection<SvEvidenceFieldChange> changes,
        string category,
        string field,
        string baseline,
        string candidate,
        SvEvidenceChangeSeverity severity,
        string message)
    {
        if (!string.Equals(baseline ?? string.Empty, candidate ?? string.Empty, StringComparison.Ordinal))
            Add(changes, category, field, severity, Empty(baseline), Empty(candidate), message);
    }

    private static void AddNullableChange<T>(
        ICollection<SvEvidenceFieldChange> changes,
        string category,
        string field,
        T? baseline,
        T? candidate,
        SvEvidenceChangeSeverity severity,
        string message)
        where T : struct, IEquatable<T>
    {
        if (!Nullable.Equals(baseline, candidate))
            Add(changes, category, field, severity, Value(baseline), Value(candidate), message);
    }

    private static void Add(
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
        => new()
        {
            ComparisonKey = LogicalKey(stream),
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

    private static SvSubscriberStreamComparison Removed(SvSubscriberStreamEvidence stream)
        => new()
        {
            ComparisonKey = LogicalKey(stream),
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

    private static SvEvidenceReportReference ToReference(SvSubscriberEvidenceReport report)
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

    private static SvEvidenceChangeSeverity MaximumSeverity(IEnumerable<SvEvidenceFieldChange> changes)
        => changes.Select(change => change.Severity).DefaultIfEmpty(SvEvidenceChangeSeverity.Info).Max();

    private static string LogicalKey(SvSubscriberStreamEvidence stream)
    {
        var identity = stream.Identity;
        var vlan = identity.VlanId?.ToString(CultureInfo.InvariantCulture) ?? "-";
        return $"SV|{identity.AppId:X4}|{NormalizeMac(identity.DestinationMac)}|{vlan}|" +
               $"{NormalizeIdentifier(identity.SvId)}|{NormalizeIdentifier(identity.DataSetReference)}";
    }

    private static string NormalizeMac(string value)
        => new((value ?? string.Empty).Where(Uri.IsHexDigit).Select(char.ToUpperInvariant).ToArray());

    private static string NormalizeIdentifier(string value)
        => (value ?? string.Empty).Trim().ToUpperInvariant();

    private static int HealthRank(string value)
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

    private static bool ApproximatelyEqual(double baseline, double candidate, double tolerancePercent)
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

    private static string Empty(string? value)
        => string.IsNullOrWhiteSpace(value) ? "unknown" : value;
}

public static class SvSubscriberEvidenceComparisonSerializer
{
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    public static string ToJson(SvSubscriberEvidenceComparison comparison)
    {
        ArgumentNullException.ThrowIfNull(comparison);
        comparison.Validate();
        return JsonSerializer.Serialize(comparison, JsonOptions);
    }

    public static SvSubscriberEvidenceComparison FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new ArgumentException("SV comparison JSON cannot be empty.", nameof(json));
        var comparison = JsonSerializer.Deserialize<SvSubscriberEvidenceComparison>(json, JsonOptions)
            ?? throw new InvalidDataException("SV comparison JSON did not contain a comparison document.");
        comparison.Validate();
        return comparison;
    }

    public static string ToMarkdown(SvSubscriberEvidenceComparison comparison)
    {
        ArgumentNullException.ThrowIfNull(comparison);
        comparison.Validate();

        var builder = new StringBuilder();
        builder.AppendLine("# ARSVIN Subscriber Evidence Comparison");
        builder.AppendLine();
        builder.AppendLine("> Baseline-versus-candidate engineering evidence. Warnings and errors identify regressions for review; this is not a formal IEC 61850 conformance certificate.");
        builder.AppendLine();
        builder.AppendLine("## Comparison metadata");
        builder.AppendLine();
        AppendKeyValueTable(builder,
        [
            ("Schema", comparison.SchemaVersion),
            ("Generated", Timestamp(comparison.GeneratedAt)),
            ("Baseline generated", Timestamp(comparison.Baseline.GeneratedAt)),
            ("Baseline version", comparison.Baseline.Version),
            ("Baseline commit", comparison.Baseline.Commit),
            ("Baseline capture", comparison.Baseline.CaptureSource),
            ("Candidate generated", Timestamp(comparison.Candidate.GeneratedAt)),
            ("Candidate version", comparison.Candidate.Version),
            ("Candidate commit", comparison.Candidate.Commit),
            ("Candidate capture", comparison.Candidate.CaptureSource)
        ]);

        builder.AppendLine("## Summary");
        builder.AppendLine();
        AppendKeyValueTable(builder,
        [
            ("Baseline streams", comparison.Summary.BaselineStreamCount.ToString(CultureInfo.InvariantCulture)),
            ("Candidate streams", comparison.Summary.CandidateStreamCount.ToString(CultureInfo.InvariantCulture)),
            ("Added", comparison.Summary.AddedStreamCount.ToString(CultureInfo.InvariantCulture)),
            ("Removed", comparison.Summary.RemovedStreamCount.ToString(CultureInfo.InvariantCulture)),
            ("Changed", comparison.Summary.ChangedStreamCount.ToString(CultureInfo.InvariantCulture)),
            ("Unchanged", comparison.Summary.UnchangedStreamCount.ToString(CultureInfo.InvariantCulture)),
            ("Info changes", comparison.Summary.InfoChangeCount.ToString(CultureInfo.InvariantCulture)),
            ("Warnings", comparison.Summary.WarningChangeCount.ToString(CultureInfo.InvariantCulture)),
            ("Errors", comparison.Summary.ErrorChangeCount.ToString(CultureInfo.InvariantCulture)),
            ("Regression status", comparison.Summary.HasRegressions ? "REVIEW REQUIRED" : "NO REGRESSION DETECTED")
        ]);

        AppendChanges(builder, "Report-level changes", comparison.ReportChanges);

        builder.AppendLine("## Stream comparison");
        builder.AppendLine();
        builder.AppendLine("| Kind | Severity | APPID | svID | Dataset | Changes |");
        builder.AppendLine("|---|---|---:|---|---|---:|");
        foreach (var stream in comparison.Streams)
        {
            builder.Append("| ").Append(Cell(stream.Kind.ToString()))
                .Append(" | ").Append(Cell(stream.Severity.ToString()))
                .Append(" | 0x").Append(stream.Identity.AppId.ToString("X4", CultureInfo.InvariantCulture))
                .Append(" | ").Append(Cell(stream.Identity.SvId))
                .Append(" | ").Append(Cell(stream.Identity.DataSetReference))
                .Append(" | ").Append(stream.Changes.Count.ToString(CultureInfo.InvariantCulture)).AppendLine(" |");
        }
        builder.AppendLine();

        foreach (var stream in comparison.Streams.Where(stream => stream.Changes.Count > 0))
        {
            builder.Append("## 0x").Append(stream.Identity.AppId.ToString("X4", CultureInfo.InvariantCulture))
                .Append(" — ").AppendLine(Heading(stream.Identity.SvId));
            builder.AppendLine();
            AppendKeyValueTable(builder,
            [
                ("Kind", stream.Kind.ToString()),
                ("Severity", stream.Severity.ToString()),
                ("Logical key", stream.ComparisonKey),
                ("Baseline stream key", Empty(stream.BaselineStreamKey)),
                ("Candidate stream key", Empty(stream.CandidateStreamKey)),
                ("Destination MAC", stream.Identity.DestinationMac),
                ("VLAN", stream.Identity.VlanId?.ToString(CultureInfo.InvariantCulture) ?? "untagged"),
                ("Dataset", stream.Identity.DataSetReference)
            ]);
            AppendChanges(builder, "Changes", stream.Changes);
        }

        return builder.ToString();
    }

    private static void AppendChanges(
        StringBuilder builder,
        string title,
        IReadOnlyList<SvEvidenceFieldChange> changes)
    {
        builder.Append("## ").AppendLine(title);
        builder.AppendLine();
        if (changes.Count == 0)
        {
            builder.AppendLine("- No changes.");
            builder.AppendLine();
            return;
        }

        builder.AppendLine("| Severity | Category | Field | Baseline | Candidate | Message |");
        builder.AppendLine("|---|---|---|---|---|---|");
        foreach (var change in changes)
        {
            builder.Append("| ").Append(Cell(change.Severity.ToString()))
                .Append(" | ").Append(Cell(change.Category))
                .Append(" | ").Append(Cell(change.Field))
                .Append(" | ").Append(Cell(change.Baseline))
                .Append(" | ").Append(Cell(change.Candidate))
                .Append(" | ").Append(Cell(change.Message)).AppendLine(" |");
        }
        builder.AppendLine();
    }

    private static void AppendKeyValueTable(StringBuilder builder, IEnumerable<(string Key, string Value)> rows)
    {
        builder.AppendLine("| Field | Value |");
        builder.AppendLine("|---|---|");
        foreach (var row in rows)
            builder.Append("| ").Append(Cell(row.Key)).Append(" | ").Append(Cell(Empty(row.Value))).AppendLine(" |");
        builder.AppendLine();
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        return options;
    }

    private static string Timestamp(DateTimeOffset value)
        => value.ToString("yyyy-MM-dd HH:mm:ss.fff zzz", CultureInfo.InvariantCulture);

    private static string Empty(string? value)
        => string.IsNullOrWhiteSpace(value) ? "-" : value;

    private static string Cell(string? value)
        => Empty(value).Replace("|", "\\|", StringComparison.Ordinal)
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal);

    private static string Heading(string? value)
        => Empty(value).Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal);
}
