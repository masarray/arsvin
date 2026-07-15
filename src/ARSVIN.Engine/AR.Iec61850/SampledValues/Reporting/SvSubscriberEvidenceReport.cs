using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AR.Iec61850.SampledValues.Profiles;

namespace AR.Iec61850.SampledValues.Reporting;

public sealed record SvSubscriberEvidenceReport
{
    public const string CurrentSchemaVersion = "arsvin.sv-subscriber-evidence/v1";

    public string SchemaVersion { get; init; } = CurrentSchemaVersion;
    public DateTimeOffset GeneratedAt { get; init; }
    public SvSubscriberSoftwareEvidence Software { get; init; } = new();
    public SvSubscriberCaptureEvidence Capture { get; init; } = new();
    public SvSubscriberSummaryEvidence Summary { get; init; } = new();
    public IReadOnlyList<SvSubscriberStreamEvidence> Streams { get; init; }
        = Array.Empty<SvSubscriberStreamEvidence>();

    public void Validate()
    {
        if (!string.Equals(SchemaVersion, CurrentSchemaVersion, StringComparison.Ordinal))
            throw new InvalidOperationException($"Unsupported SV report schema '{SchemaVersion}'.");
        if (GeneratedAt == default)
            throw new InvalidOperationException("SV report requires a generation timestamp.");
        if (string.IsNullOrWhiteSpace(Software.Product))
            throw new InvalidOperationException("SV report requires a product name.");
        if (Summary.StreamCount != Streams.Count)
            throw new InvalidOperationException("SV report summary stream count does not match the stream evidence collection.");
        if (Streams.Any(stream => string.IsNullOrWhiteSpace(stream.Key)))
            throw new InvalidOperationException("Every SV report stream requires a stable key.");
        if (Streams.Select(stream => stream.Key).Distinct(StringComparer.Ordinal).Count() != Streams.Count)
            throw new InvalidOperationException("SV report stream keys must be unique.");
    }
}

public sealed record SvSubscriberSoftwareEvidence
{
    public string Product { get; init; } = string.Empty;
    public string Version { get; init; } = string.Empty;
    public string InformationalVersion { get; init; } = string.Empty;
    public string Commit { get; init; } = string.Empty;
    public string Repository { get; init; } = string.Empty;
}

public sealed record SvSubscriberCaptureEvidence
{
    public string Source { get; init; } = "Unknown";
    public string SclPath { get; init; } = string.Empty;
    public string Adapter { get; init; } = string.Empty;
    public string Filter { get; init; } = string.Empty;
    public DateTimeOffset? StartedAt { get; init; }
    public DateTimeOffset EndedAt { get; init; }
    public double DurationSeconds { get; init; }
    public long RawFrames { get; init; }
    public long SvFrames { get; init; }
    public long ParseErrors { get; init; }
    public long DroppedByFilter { get; init; }
}

public sealed record SvSubscriberSummaryEvidence
{
    public string Health { get; init; } = "IDLE";
    public int StreamCount { get; init; }
    public int RuntimeIssueCount { get; init; }
    public int ConfigurationFindingCount { get; init; }
}

public sealed record SvSubscriberStreamEvidence
{
    public string Key { get; init; } = string.Empty;
    public string Health { get; init; } = "IDLE";
    public string HealthDetail { get; init; } = string.Empty;
    public SvSubscriberStreamIdentityEvidence Identity { get; init; } = new();
    public SvSubscriberRuntimeEvidence Runtime { get; init; } = new();
    public SvSubscriberObservationEvidence Observation { get; init; } = new();
    public IReadOnlyList<SvSubscriberPhasorEvidence> Phasors { get; init; }
        = Array.Empty<SvSubscriberPhasorEvidence>();
    public IReadOnlyList<string> Diagnostics { get; init; }
        = Array.Empty<string>();
}

public sealed record SvSubscriberStreamIdentityEvidence
{
    public ushort AppId { get; init; }
    public string SourceMac { get; init; } = string.Empty;
    public string DestinationMac { get; init; } = string.Empty;
    public ushort? VlanId { get; init; }
    public byte? VlanPriority { get; init; }
    public string SvId { get; init; } = string.Empty;
    public string DataSetReference { get; init; } = string.Empty;
    public uint? ConfigurationRevision { get; init; }
    public int AsduPerFrame { get; init; }
    public ushort? LastSampleCount { get; init; }
    public ushort? DeclaredSampleRate { get; init; }
    public ushort? DeclaredSampleMode { get; init; }
    public byte? SampleSynchronization { get; init; }
}

public sealed record SvSubscriberRuntimeEvidence
{
    public long FrameCount { get; init; }
    public long AsduCount { get; init; }
    public double ActualFramesPerSecond { get; init; }
    public double AverageFrameGapMilliseconds { get; init; }
    public double MaximumFrameGapMilliseconds { get; init; }
    public int SequenceGapCount { get; init; }
    public int DuplicateCount { get; init; }
    public int OutOfOrderCount { get; init; }
    public int PayloadIssueCount { get; init; }
    public int SclMismatchCount { get; init; }
    public bool IsWaveformWindowReady { get; init; }
    public string LayoutBinding { get; init; } = string.Empty;
    public string QualitySummary { get; init; } = string.Empty;
    public string CursorSummary { get; init; } = string.Empty;
    public string LastSeen { get; init; } = string.Empty;
}

public sealed record SvSubscriberObservationEvidence
{
    public IReadOnlyList<SvObservationInputKind> InputKinds { get; init; }
        = Array.Empty<SvObservationInputKind>();
    public SvObservationInputKind LastInputKind { get; init; }
    public bool IsBoundToScl { get; init; }
    public string ControlBlockReference { get; init; } = string.Empty;
    public int WindowFrames { get; init; }
    public int WindowSamples { get; init; }
    public double WindowDurationSeconds { get; init; }
    public DateTimeOffset? FirstTimestamp { get; init; }
    public DateTimeOffset? LastTimestamp { get; init; }
    public SvObservedStreamFacts Facts { get; init; } = new();
    public IReadOnlyDictionary<string, SvFactSource> FactProvenance { get; init; }
        = new Dictionary<string, SvFactSource>(StringComparer.Ordinal);
    public SvProfileDetectionResult? ProfileDetection { get; init; }
    public SvExpectedStreamConfiguration? ExpectedConfiguration { get; init; }
    public SvConfigurationComparisonResult? ConfigurationComparison { get; init; }
    public IReadOnlyList<string> Diagnostics { get; init; }
        = Array.Empty<string>();
}

public sealed record SvSubscriberPhasorEvidence
{
    public string Channel { get; init; } = string.Empty;
    public string Kind { get; init; } = string.Empty;
    public double Rms { get; init; }
    public double Peak { get; init; }
    public double AngleDegrees { get; init; }
}

public static class SvSubscriberEvidenceReportSerializer
{
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    public static string ToJson(SvSubscriberEvidenceReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        report.Validate();
        return JsonSerializer.Serialize(report, JsonOptions);
    }

    public static SvSubscriberEvidenceReport FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new ArgumentException("SV report JSON cannot be empty.", nameof(json));

        var report = JsonSerializer.Deserialize<SvSubscriberEvidenceReport>(json, JsonOptions)
            ?? throw new InvalidDataException("SV report JSON did not contain a report document.");
        report.Validate();
        return report;
    }

    public static string ToMarkdown(SvSubscriberEvidenceReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        report.Validate();

        var builder = new StringBuilder();
        builder.AppendLine("# ARSVIN Subscriber Evidence Report");
        builder.AppendLine();
        builder.AppendLine("> Receiver-side engineering evidence. This document is not a formal IEC 61850 conformance certificate.");
        builder.AppendLine();
        builder.AppendLine("## Report metadata");
        builder.AppendLine();
        AppendKeyValueTable(builder,
        [
            ("Schema", report.SchemaVersion),
            ("Generated", Timestamp(report.GeneratedAt)),
            ("Product", report.Software.Product),
            ("Version", report.Software.Version),
            ("Informational version", report.Software.InformationalVersion),
            ("Commit", report.Software.Commit),
            ("Repository", report.Software.Repository),
            ("Capture source", report.Capture.Source),
            ("SCL", Empty(report.Capture.SclPath, "not loaded")),
            ("Adapter", Empty(report.Capture.Adapter)),
            ("Filter", Empty(report.Capture.Filter, "none")),
            ("Capture started", Timestamp(report.Capture.StartedAt)),
            ("Capture ended", Timestamp(report.Capture.EndedAt)),
            ("Capture duration", Number(report.Capture.DurationSeconds, "0.###") + " s")
        ]);

        builder.AppendLine("## Summary");
        builder.AppendLine();
        AppendKeyValueTable(builder,
        [
            ("Health", report.Summary.Health),
            ("Raw frames", report.Capture.RawFrames.ToString("N0", CultureInfo.InvariantCulture)),
            ("SV frames", report.Capture.SvFrames.ToString("N0", CultureInfo.InvariantCulture)),
            ("Streams", report.Summary.StreamCount.ToString("N0", CultureInfo.InvariantCulture)),
            ("Runtime issues", report.Summary.RuntimeIssueCount.ToString("N0", CultureInfo.InvariantCulture)),
            ("Configuration findings", report.Summary.ConfigurationFindingCount.ToString("N0", CultureInfo.InvariantCulture)),
            ("Parse errors", report.Capture.ParseErrors.ToString("N0", CultureInfo.InvariantCulture)),
            ("Dropped by filter", report.Capture.DroppedByFilter.ToString("N0", CultureInfo.InvariantCulture))
        ]);

        builder.AppendLine("## Streams");
        builder.AppendLine();
        builder.AppendLine("| Health | APPID | svID | Profile | Confidence | SCL match | Window | Input |" );
        builder.AppendLine("|---|---:|---|---|---|---|---:|---|");
        foreach (var stream in report.Streams)
        {
            var profile = stream.Observation.ProfileDetection;
            var comparison = stream.Observation.ConfigurationComparison;
            builder.Append("| ").Append(Cell(stream.Health))
                .Append(" | ").Append(Hex(stream.Identity.AppId))
                .Append(" | ").Append(Cell(stream.Identity.SvId))
                .Append(" | ").Append(Cell(profile?.Profile.DisplayName ?? "Unknown"))
                .Append(" | ").Append(Cell(profile?.Confidence.ToString() ?? "Unknown"))
                .Append(" | ").Append(Cell(comparison?.Summary ?? "Not configured"))
                .Append(" | ").Append(stream.Observation.WindowSamples.ToString("N0", CultureInfo.InvariantCulture))
                .Append(" samples | ").Append(Cell(InputText(stream.Observation.InputKinds)))
                .AppendLine(" |");
        }
        builder.AppendLine();

        foreach (var stream in report.Streams)
            AppendStream(builder, stream);

        return builder.ToString();
    }

    private static void AppendStream(StringBuilder builder, SvSubscriberStreamEvidence stream)
    {
        builder.Append("## ").Append(Hex(stream.Identity.AppId)).Append(" — ")
            .AppendLine(Heading(stream.Identity.SvId));
        builder.AppendLine();
        AppendKeyValueTable(builder,
        [
            ("Stream key", stream.Key),
            ("Health", stream.Health),
            ("Health detail", stream.HealthDetail),
            ("Source MAC", stream.Identity.SourceMac),
            ("Destination MAC", stream.Identity.DestinationMac),
            ("VLAN", stream.Identity.VlanId?.ToString(CultureInfo.InvariantCulture) ?? "untagged"),
            ("VLAN priority", Value(stream.Identity.VlanPriority)),
            ("Dataset", stream.Identity.DataSetReference),
            ("confRev", Value(stream.Identity.ConfigurationRevision)),
            ("nofASDU", stream.Identity.AsduPerFrame.ToString(CultureInfo.InvariantCulture)),
            ("Last smpCnt", Value(stream.Identity.LastSampleCount)),
            ("Declared sample rate", Value(stream.Identity.DeclaredSampleRate)),
            ("Declared sample mode", Value(stream.Identity.DeclaredSampleMode)),
            ("smpSynch", Value(stream.Identity.SampleSynchronization)),
            ("SCL binding", stream.Observation.IsBoundToScl ? Empty(stream.Observation.ControlBlockReference) : "not bound")
        ]);

        builder.AppendLine("### Observation window");
        builder.AppendLine();
        AppendKeyValueTable(builder,
        [
            ("Input kinds", InputText(stream.Observation.InputKinds)),
            ("Last input kind", stream.Observation.LastInputKind.ToString()),
            ("First timestamp", Timestamp(stream.Observation.FirstTimestamp)),
            ("Last timestamp", Timestamp(stream.Observation.LastTimestamp)),
            ("Duration", Number(stream.Observation.WindowDurationSeconds, "0.###") + " s"),
            ("Frames", stream.Observation.WindowFrames.ToString("N0", CultureInfo.InvariantCulture)),
            ("Samples", stream.Observation.WindowSamples.ToString("N0", CultureInfo.InvariantCulture)),
            ("Observed fps", Number(stream.Observation.Facts.ObservedFramesPerSecond, "0.###")),
            ("Observed samples/s", Number(stream.Observation.Facts.ObservedSamplesPerSecond, "0.###")),
            ("Observed counter wrap", Value(stream.Observation.Facts.ObservedCounterWrap))
        ]);

        builder.AppendLine("### Runtime integrity");
        builder.AppendLine();
        AppendKeyValueTable(builder,
        [
            ("Total frames", stream.Runtime.FrameCount.ToString("N0", CultureInfo.InvariantCulture)),
            ("Total ASDUs", stream.Runtime.AsduCount.ToString("N0", CultureInfo.InvariantCulture)),
            ("Actual fps", Number(stream.Runtime.ActualFramesPerSecond, "0.###")),
            ("Average frame gap", Number(stream.Runtime.AverageFrameGapMilliseconds, "0.###") + " ms"),
            ("Maximum frame gap", Number(stream.Runtime.MaximumFrameGapMilliseconds, "0.###") + " ms"),
            ("Sequence gaps", stream.Runtime.SequenceGapCount.ToString(CultureInfo.InvariantCulture)),
            ("Duplicates", stream.Runtime.DuplicateCount.ToString(CultureInfo.InvariantCulture)),
            ("Out-of-order", stream.Runtime.OutOfOrderCount.ToString(CultureInfo.InvariantCulture)),
            ("Payload issues", stream.Runtime.PayloadIssueCount.ToString(CultureInfo.InvariantCulture)),
            ("SCL mismatches", stream.Runtime.SclMismatchCount.ToString(CultureInfo.InvariantCulture)),
            ("Waveform window ready", stream.Runtime.IsWaveformWindowReady ? "yes" : "no"),
            ("Layout binding", stream.Runtime.LayoutBinding),
            ("Quality", stream.Runtime.QualitySummary),
            ("Cursor", stream.Runtime.CursorSummary),
            ("Last seen", stream.Runtime.LastSeen)
        ]);

        AppendObservedFacts(builder, stream.Observation.Facts);
        AppendExpectedConfiguration(builder, stream.Observation.ExpectedConfiguration);
        AppendConfigurationFindings(builder, stream.Observation.ConfigurationComparison);
        AppendProfileEvidence(builder, stream.Observation.ProfileDetection);
        AppendPhasors(builder, stream.Phasors);
        AppendDiagnostics(builder, stream.Diagnostics, stream.Observation.Diagnostics);
    }

    private static void AppendObservedFacts(StringBuilder builder, SvObservedStreamFacts facts)
    {
        builder.AppendLine("### Observed facts and provenance");
        builder.AppendLine();
        builder.AppendLine("| Fact | Value | Source |");
        builder.AppendLine("|---|---|---|");
        AppendFact(builder, facts, nameof(facts.EtherType), "EtherType", facts.EtherType.HasValue ? Hex(facts.EtherType.Value) : "unknown");
        AppendFact(builder, facts, nameof(facts.AppId), "APPID", facts.AppId.HasValue ? Hex(facts.AppId.Value) : "unknown");
        AppendFact(builder, facts, nameof(facts.DestinationMac), "Destination MAC", facts.DestinationMac);
        AppendFact(builder, facts, nameof(facts.VlanId), "VLAN ID", Value(facts.VlanId));
        AppendFact(builder, facts, nameof(facts.VlanPriority), "VLAN priority", Value(facts.VlanPriority));
        AppendFact(builder, facts, nameof(facts.SvId), "svID", facts.SvId);
        AppendFact(builder, facts, nameof(facts.DataSetReference), "Dataset reference", facts.DataSetReference);
        AppendFact(builder, facts, nameof(facts.ConfigurationRevision), "confRev", Value(facts.ConfigurationRevision));
        AppendFact(builder, facts, nameof(facts.AsduPerFrame), "ASDU per frame", Value(facts.AsduPerFrame));
        AppendFact(builder, facts, nameof(facts.PayloadBytesPerAsdu), "Payload bytes per ASDU", Value(facts.PayloadBytesPerAsdu));
        AppendFact(builder, facts, nameof(facts.DeclaredSampleRate), "Declared sample rate", Value(facts.DeclaredSampleRate));
        AppendFact(builder, facts, nameof(facts.DeclaredSampleMode), "Declared sample mode", Value(facts.DeclaredSampleMode));
        AppendFact(builder, facts, nameof(facts.ObservedFramesPerSecond), "Observed frames/s", Number(facts.ObservedFramesPerSecond, "0.###"));
        AppendFact(builder, facts, nameof(facts.ObservedSamplesPerSecond), "Observed samples/s", Number(facts.ObservedSamplesPerSecond, "0.###"));
        AppendFact(builder, facts, nameof(facts.ObservedCounterWrap), "Observed counter wrap", Value(facts.ObservedCounterWrap));
        AppendFact(builder, facts, nameof(facts.NominalFrequencyHz), "Nominal frequency", Number(facts.NominalFrequencyHz, "0.###"));
        AppendFact(builder, facts, nameof(facts.DataSetSignature), "Dataset signature", Signature(facts.DataSetSignature));
        var transitions = facts.CounterTransitions;
        AppendFact(builder, facts, nameof(facts.CounterTransitions), "Counter transitions",
            $"sequential {transitions.SequentialCount}, gaps {transitions.GapCount}, duplicates {transitions.DuplicateCount}, out-of-order/reset {transitions.OutOfOrderOrResetCount}, wraps {transitions.ConfirmedWrapCount}");
        builder.AppendLine();
    }

    private static void AppendExpectedConfiguration(StringBuilder builder, SvExpectedStreamConfiguration? expected)
    {
        builder.AppendLine("### Expected SCL configuration");
        builder.AppendLine();
        if (expected is null)
        {
            builder.AppendLine("- Not configured.");
            builder.AppendLine();
            return;
        }

        AppendKeyValueTable(builder,
        [
            ("EtherType", expected.EtherType.HasValue ? Hex(expected.EtherType.Value) : "unknown"),
            ("APPID", expected.AppId.HasValue ? Hex(expected.AppId.Value) : "unknown"),
            ("Destination MAC", expected.DestinationMac),
            ("VLAN ID", Value(expected.VlanId)),
            ("VLAN priority", Value(expected.VlanPriority)),
            ("svID", expected.SvId),
            ("Dataset", expected.DataSetReference),
            ("confRev", Value(expected.ConfigurationRevision)),
            ("ASDU per frame", Value(expected.AsduPerFrame)),
            ("Payload bytes per ASDU", Value(expected.PayloadBytesPerAsdu)),
            ("Declared sample rate", Value(expected.DeclaredSampleRate)),
            ("Declared sample mode", Value(expected.DeclaredSampleMode)),
            ("Dataset signature", Signature(expected.DataSetSignature))
        ]);
    }

    private static void AppendConfigurationFindings(StringBuilder builder, SvConfigurationComparisonResult? comparison)
    {
        builder.AppendLine("### Configuration comparison");
        builder.AppendLine();
        if (comparison is null)
        {
            builder.AppendLine("- Not configured.");
            builder.AppendLine();
            return;
        }

        builder.Append("- Mode: **").Append(comparison.Mode).AppendLine("**");
        builder.Append("- Result: **").Append(comparison.Summary).AppendLine("**");
        builder.AppendLine();
        if (comparison.Findings.Count == 0)
        {
            builder.AppendLine("- No differences found.");
            builder.AppendLine();
            return;
        }

        builder.AppendLine("| Severity | Code | Field | Expected | Observed | Message |");
        builder.AppendLine("|---|---|---|---|---|---|");
        foreach (var finding in comparison.Findings)
        {
            builder.Append("| ").Append(Cell(finding.Severity.ToString()))
                .Append(" | ").Append(Cell(finding.Code))
                .Append(" | ").Append(Cell(finding.Field))
                .Append(" | ").Append(Cell(finding.Expected))
                .Append(" | ").Append(Cell(finding.Observed))
                .Append(" | ").Append(Cell(finding.Message)).AppendLine(" |");
        }
        builder.AppendLine();
    }

    private static void AppendProfileEvidence(StringBuilder builder, SvProfileDetectionResult? detection)
    {
        builder.AppendLine("### Profile detection evidence");
        builder.AppendLine();
        if (detection is null)
        {
            builder.AppendLine("- No profile result.");
            builder.AppendLine();
            return;
        }

        AppendKeyValueTable(builder,
        [
            ("Profile", detection.Profile.DisplayName),
            ("Profile ID", detection.Profile.Id),
            ("Family", detection.Profile.Family),
            ("Confidence", detection.Confidence.ToString()),
            ("Raw confidence", detection.RawConfidence.ToString()),
            ("Score", Number(detection.ScorePercent, "0.###") + "%"),
            ("Matched weight", detection.MatchedWeight.ToString(CultureInfo.InvariantCulture)),
            ("Conflict weight", detection.ConflictWeight.ToString(CultureInfo.InvariantCulture)),
            ("Evaluated weight", detection.EvaluatedWeight.ToString(CultureInfo.InvariantCulture)),
            ("Evidence status", detection.Profile.EvidenceStatus.ToString())
        ]);

        builder.AppendLine("#### Evidence sources");
        builder.AppendLine();
        builder.AppendLine("| Source | Status | Description |");
        builder.AppendLine("|---|---|---|");
        foreach (var source in detection.Profile.Sources)
        {
            builder.Append("| ").Append(Cell(source.SourceId))
                .Append(" | ").Append(Cell(source.Status.ToString()))
                .Append(" | ").Append(Cell(source.Description)).AppendLine(" |");
        }
        builder.AppendLine();

        builder.AppendLine("#### Match evidence");
        builder.AppendLine();
        if (detection.Evidence.Count == 0)
        {
            builder.AppendLine("- No evaluated evidence fields.");
            builder.AppendLine();
            return;
        }

        builder.AppendLine("| Field | Outcome | Weight | Expected | Observed | Message |");
        builder.AppendLine("|---|---|---:|---|---|---|");
        foreach (var evidence in detection.Evidence)
        {
            builder.Append("| ").Append(Cell(evidence.Field))
                .Append(" | ").Append(Cell(evidence.Outcome.ToString()))
                .Append(" | ").Append(evidence.Weight.ToString(CultureInfo.InvariantCulture))
                .Append(" | ").Append(Cell(evidence.Expected))
                .Append(" | ").Append(Cell(evidence.Observed))
                .Append(" | ").Append(Cell(evidence.Message)).AppendLine(" |");
        }
        builder.AppendLine();
    }

    private static void AppendPhasors(StringBuilder builder, IReadOnlyList<SvSubscriberPhasorEvidence> phasors)
    {
        builder.AppendLine("### Phasors");
        builder.AppendLine();
        if (phasors.Count == 0)
        {
            builder.AppendLine("- No complete decoded phasor window.");
            builder.AppendLine();
            return;
        }

        builder.AppendLine("| Channel | Kind | RMS | Peak | Angle |");
        builder.AppendLine("|---|---|---:|---:|---:|");
        foreach (var phasor in phasors)
        {
            builder.Append("| ").Append(Cell(phasor.Channel))
                .Append(" | ").Append(Cell(phasor.Kind))
                .Append(" | ").Append(Number(phasor.Rms, "0.###"))
                .Append(" | ").Append(Number(phasor.Peak, "0.###"))
                .Append(" | ").Append(Number(phasor.AngleDegrees, "0.###")).AppendLine("° |");
        }
        builder.AppendLine();
    }

    private static void AppendDiagnostics(
        StringBuilder builder,
        IReadOnlyList<string> runtimeDiagnostics,
        IReadOnlyList<string> observationDiagnostics)
    {
        builder.AppendLine("### Diagnostics");
        builder.AppendLine();
        var diagnostics = runtimeDiagnostics.Concat(observationDiagnostics)
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (diagnostics.Length == 0)
        {
            builder.AppendLine("- No diagnostics.");
            builder.AppendLine();
            return;
        }

        foreach (var diagnostic in diagnostics)
            builder.Append("- ").AppendLine(diagnostic);
        builder.AppendLine();
    }

    private static void AppendFact(
        StringBuilder builder,
        SvObservedStreamFacts facts,
        string propertyName,
        string label,
        string value)
    {
        var source = facts.Provenance.TryGetValue(propertyName, out var factSource)
            ? factSource.ToString()
            : SvFactSource.Unknown.ToString();
        builder.Append("| ").Append(Cell(label))
            .Append(" | ").Append(Cell(Empty(value, "unknown")))
            .Append(" | ").Append(Cell(source)).AppendLine(" |");
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

    private static string InputText(IReadOnlyList<SvObservationInputKind> inputKinds)
        => inputKinds.Count == 0 ? "Unknown" : string.Join(", ", inputKinds);

    private static string Signature(IReadOnlyList<SvDatasetElementSignature> signature)
        => signature.Count == 0
            ? "unknown"
            : string.Join(", ", signature.Select(item =>
                $"{Empty(item.BType, "-")}/{Empty(item.Cdc, "-")}" +
                (item.IsQuality ? ":quality" : string.Empty) +
                (item.IsTimestamp ? ":timestamp" : string.Empty)));

    private static string Timestamp(DateTimeOffset? value)
        => value.HasValue ? Timestamp(value.Value) : "unknown";

    private static string Timestamp(DateTimeOffset value)
        => value.ToString("yyyy-MM-dd HH:mm:ss.fff zzz", CultureInfo.InvariantCulture);

    private static string Hex(ushort value)
        => $"0x{value:X4}";

    private static string Number(double? value, string format)
        => value.HasValue ? Number(value.Value, format) : "unknown";

    private static string Number(double value, string format)
        => value.ToString(format, CultureInfo.InvariantCulture);

    private static string Value<T>(T? value) where T : struct
        => value.HasValue ? Convert.ToString(value.Value, CultureInfo.InvariantCulture) ?? "unknown" : "unknown";

    private static string Empty(string? value, string fallback = "-")
        => string.IsNullOrWhiteSpace(value) ? fallback : value;

    private static string Cell(string? value)
        => Empty(value).Replace("|", "\\|", StringComparison.Ordinal)
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal);

    private static string Heading(string? value)
        => Empty(value, "Unknown stream").Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal);
}
