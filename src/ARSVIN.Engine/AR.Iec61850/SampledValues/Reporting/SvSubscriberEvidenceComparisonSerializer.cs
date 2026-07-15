using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AR.Iec61850.SampledValues.Reporting;

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
        KeyValueTable(builder,
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
        KeyValueTable(builder,
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

        Changes(builder, "Report-level changes", comparison.ReportChanges);

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
                .Append(" | ").Append(stream.Changes.Count.ToString(CultureInfo.InvariantCulture))
                .AppendLine(" |");
        }
        builder.AppendLine();

        foreach (var stream in comparison.Streams.Where(stream => stream.Changes.Count > 0))
        {
            builder.Append("## 0x").Append(stream.Identity.AppId.ToString("X4", CultureInfo.InvariantCulture))
                .Append(" — ").AppendLine(Heading(stream.Identity.SvId));
            builder.AppendLine();
            KeyValueTable(builder,
            [
                ("Kind", stream.Kind.ToString()),
                ("Severity", stream.Severity.ToString()),
                ("Logical stream key", stream.LogicalStreamKey),
                ("Comparison key", stream.ComparisonKey),
                ("Baseline stream key", Empty(stream.BaselineStreamKey)),
                ("Candidate stream key", Empty(stream.CandidateStreamKey)),
                ("Destination MAC", stream.Identity.DestinationMac),
                ("VLAN", stream.Identity.VlanId?.ToString(CultureInfo.InvariantCulture) ?? "untagged"),
                ("Dataset", stream.Identity.DataSetReference)
            ]);
            Changes(builder, "Changes", stream.Changes);
        }

        return builder.ToString();
    }

    private static void Changes(
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

    private static void KeyValueTable(
        StringBuilder builder,
        IEnumerable<(string Key, string Value)> rows)
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
