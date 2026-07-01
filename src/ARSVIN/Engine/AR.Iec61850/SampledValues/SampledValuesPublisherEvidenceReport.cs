using System.Globalization;
using System.Text;

namespace AR.Iec61850.SampledValues;

public sealed record SampledValuesEvidenceFinding(
    string Severity,
    string Area,
    string Message,
    string Detail);

public sealed record SampledValuesEvidenceStream(
    string SlotName,
    bool IsEnabled,
    string ControlBlockReference,
    string SvId,
    string DataSetReference,
    string AppId,
    string SourceMac,
    string DestinationMac,
    string Vlan,
    double SampleRateHz,
    double PublicationRateHz,
    ushort NoAsdu,
    int PayloadBytesPerAsdu,
    int EstimatedEthernetBytes,
    double EstimatedBandwidthBitsPerSecond,
    string SignalSource,
    string Quality,
    string SyncMode,
    string Status,
    IReadOnlyList<SampledValuesEvidenceFinding> Findings);

public sealed record SampledValuesPublisherEvidenceReport(
    string ToolName,
    string ToolVersion,
    DateTimeOffset CreatedAt,
    string SclPath,
    string Adapter,
    string Mode,
    string TxTiming,
    string SafetyBoundary,
    IReadOnlyList<SampledValuesEvidenceStream> Streams,
    IReadOnlyList<SampledValuesEvidenceFinding> GlobalFindings);

public static class SampledValuesPublisherEvidenceReportWriter
{
    public static string ToMarkdown(SampledValuesPublisherEvidenceReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        var builder = new StringBuilder();
        builder.AppendLine("# ARSVIN Sampled Values Publisher Evidence Report");
        builder.AppendLine();
        builder.AppendLine($"Generated: {report.CreatedAt:yyyy-MM-dd HH:mm:ss zzz}");
        builder.AppendLine($"Tool: {Text(report.ToolName)} {Text(report.ToolVersion)}");
        builder.AppendLine($"SCL: `{Text(report.SclPath)}`");
        builder.AppendLine($"Adapter: {Text(report.Adapter)}");
        builder.AppendLine($"Mode: {Text(report.Mode)}");
        builder.AppendLine($"TX timing: {Text(report.TxTiming)}");
        builder.AppendLine();
        builder.AppendLine("> This report is TX-side publisher evidence. It is not a network analyzer capture, not a calibrated measurement certificate, and not IEC 61850 conformance certification.");
        builder.AppendLine();

        builder.AppendLine("## Summary");
        builder.AppendLine();
        builder.AppendLine($"- Enabled publishers: {report.Streams.Count(stream => stream.IsEnabled)}");
        builder.AppendLine($"- Fatal findings: {CountSeverity(report, "ERROR")}");
        builder.AppendLine($"- Warnings: {CountSeverity(report, "WARNING")}");
        builder.AppendLine($"- Info: {CountSeverity(report, "INFO")}");
        builder.AppendLine($"- Safety boundary: {Text(report.SafetyBoundary)}");
        builder.AppendLine();

        builder.AppendLine("## Publisher streams");
        builder.AppendLine();
        builder.AppendLine("| Slot | Status | svID | APPID | Destination | VLAN | nofASDU | Sample rate | Publish rate | Payload | Bandwidth | Quality | Source |");
        builder.AppendLine("|---|---|---|---|---|---|---:|---:|---:|---:|---:|---|---|");
        foreach (var stream in report.Streams)
        {
            builder.Append("| ");
            builder.Append(Cell(stream.SlotName));
            builder.Append(" | ");
            builder.Append(Cell(stream.Status));
            builder.Append(" | ");
            builder.Append(Cell(stream.SvId));
            builder.Append(" | ");
            builder.Append(Cell(stream.AppId));
            builder.Append(" | ");
            builder.Append(Cell(stream.DestinationMac));
            builder.Append(" | ");
            builder.Append(Cell(stream.Vlan));
            builder.Append(" | ");
            builder.Append(stream.NoAsdu.ToString(CultureInfo.InvariantCulture));
            builder.Append(" | ");
            builder.Append(stream.SampleRateHz.ToString("0.###", CultureInfo.InvariantCulture));
            builder.Append(" | ");
            builder.Append(stream.PublicationRateHz.ToString("0.###", CultureInfo.InvariantCulture));
            builder.Append(" | ");
            builder.Append(stream.PayloadBytesPerAsdu.ToString(CultureInfo.InvariantCulture));
            builder.Append(" | ");
            builder.Append((stream.EstimatedBandwidthBitsPerSecond / 1000.0).ToString("0.###", CultureInfo.InvariantCulture));
            builder.Append(" kbps | ");
            builder.Append(Cell(stream.Quality));
            builder.Append(" | ");
            builder.Append(Cell(stream.SignalSource));
            builder.AppendLine(" |");
        }
        builder.AppendLine();

        foreach (var stream in report.Streams)
        {
            builder.AppendLine($"## {Text(stream.SlotName)}");
            builder.AppendLine();
            builder.AppendLine($"- Control block: `{Text(stream.ControlBlockReference)}`");
            builder.AppendLine($"- Dataset: `{Text(stream.DataSetReference)}`");
            builder.AppendLine($"- Source MAC: `{Text(stream.SourceMac)}`");
            builder.AppendLine($"- Destination MAC: `{Text(stream.DestinationMac)}`");
            builder.AppendLine($"- Estimated Ethernet frame bytes: {stream.EstimatedEthernetBytes}");
            builder.AppendLine($"- Synchronization mode: {Text(stream.SyncMode)}");
            builder.AppendLine();
            AppendFindings(builder, stream.Findings, "Findings");
        }

        AppendFindings(builder, report.GlobalFindings, "Global findings");

        builder.AppendLine("## Recommended external verification");
        builder.AppendLine();
        builder.AppendLine("- Export generated PCAP from ARSVIN and open it in Wireshark.");
        builder.AppendLine("- Confirm APPID, destination MAC, VLAN, svID, datSet, confRev, smpCnt, smpSynch, smpRate, smpMod, and nofASDU.");
        builder.AppendLine("- For relay/KM loop testing, compare the receiver subscription configuration against this report.");
        builder.AppendLine("- Treat Windows/Npcap timing as lab-grade unless externally verified with a suitable process-bus test set or time analyzer.");
        builder.AppendLine();
        return builder.ToString();
    }

    private static void AppendFindings(StringBuilder builder, IReadOnlyList<SampledValuesEvidenceFinding> findings, string title)
    {
        builder.AppendLine($"### {title}");
        builder.AppendLine();
        if (findings.Count == 0)
        {
            builder.AppendLine("No findings.");
            builder.AppendLine();
            return;
        }

        builder.AppendLine("| Severity | Area | Message | Detail |");
        builder.AppendLine("|---|---|---|---|");
        foreach (var finding in findings)
            builder.AppendLine($"| {Cell(finding.Severity)} | {Cell(finding.Area)} | {Cell(finding.Message)} | {Cell(finding.Detail)} |");
        builder.AppendLine();
    }

    private static int CountSeverity(SampledValuesPublisherEvidenceReport report, string severity)
    {
        var streamCount = report.Streams.Sum(stream => stream.Findings.Count(finding => string.Equals(finding.Severity, severity, StringComparison.OrdinalIgnoreCase)));
        var globalCount = report.GlobalFindings.Count(finding => string.Equals(finding.Severity, severity, StringComparison.OrdinalIgnoreCase));
        return streamCount + globalCount;
    }

    private static string Text(string? value) => string.IsNullOrWhiteSpace(value) ? "-" : value.Trim();

    private static string Cell(string? value)
        => Text(value).Replace("|", "\\|", StringComparison.Ordinal).Replace(Environment.NewLine, " ", StringComparison.Ordinal);
}
