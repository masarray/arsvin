using System.Reflection;
using AR.Iec61850.SampledValues.Measurements;
using AR.Iec61850.SampledValues.Profiles;
using AR.Iec61850.SampledValues.Reporting;
using ARSVIN.Subscriber.Models;

namespace ARSVIN.Subscriber.Reporting;

internal sealed record SvSubscriberReportContext
{
    public DateTimeOffset GeneratedAt { get; init; }
    public DateTimeOffset? CaptureStartedAt { get; init; }
    public string Health { get; init; } = "IDLE";
    public string SclPath { get; init; } = string.Empty;
    public string Adapter { get; init; } = string.Empty;
    public string Filter { get; init; } = string.Empty;
    public long RawFrames { get; init; }
    public long SvFrames { get; init; }
    public long ParseErrors { get; init; }
    public long DroppedByFilter { get; init; }
    public IReadOnlyList<SvStreamSnapshot> Streams { get; init; }
        = Array.Empty<SvStreamSnapshot>();
    public IReadOnlyDictionary<string, SvStreamObservationSnapshot> Observations { get; init; }
        = new Dictionary<string, SvStreamObservationSnapshot>(StringComparer.Ordinal);
}

internal static class SvSubscriberReportBuilder
{
    private const string RepositoryUrl = "https://github.com/masarray/arsvin";

    public static SvSubscriberEvidenceReport Build(SvSubscriberReportContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (context.GeneratedAt == default)
            throw new ArgumentException("Report context requires a generation timestamp.", nameof(context));

        var streams = context.Streams
            .OrderBy(stream => stream.AppId)
            .ThenBy(stream => stream.SvId, StringComparer.Ordinal)
            .Select(stream => BuildStream(stream, ResolveObservation(context, stream.Key)))
            .ToArray();
        var runtimeIssues = streams.Sum(stream =>
            stream.Runtime.SequenceGapCount +
            stream.Runtime.DuplicateCount +
            stream.Runtime.OutOfOrderCount +
            stream.Runtime.PayloadIssueCount);
        var configurationFindings = streams.Sum(stream =>
            stream.Observation.ConfigurationComparison?.Findings.Count ?? 0);
        var duration = context.CaptureStartedAt.HasValue
            ? Math.Max(0, (context.GeneratedAt - context.CaptureStartedAt.Value).TotalSeconds)
            : 0;

        return new SvSubscriberEvidenceReport
        {
            GeneratedAt = context.GeneratedAt,
            Software = ResolveSoftwareEvidence(),
            Capture = new SvSubscriberCaptureEvidence
            {
                Source = ResolveCaptureSource(streams),
                SclPath = context.SclPath,
                Adapter = context.Adapter,
                Filter = context.Filter,
                StartedAt = context.CaptureStartedAt,
                EndedAt = context.GeneratedAt,
                DurationSeconds = duration,
                RawFrames = context.RawFrames,
                SvFrames = context.SvFrames,
                ParseErrors = context.ParseErrors,
                DroppedByFilter = context.DroppedByFilter
            },
            Summary = new SvSubscriberSummaryEvidence
            {
                Health = context.Health,
                StreamCount = streams.Length,
                RuntimeIssueCount = runtimeIssues,
                ConfigurationFindingCount = configurationFindings
            },
            Streams = streams
        };
    }

    private static SvSubscriberStreamEvidence BuildStream(
        SvStreamSnapshot stream,
        SvStreamObservationSnapshot? observation)
    {
        var facts = observation?.Facts ?? BuildFallbackFacts(stream);
        var inputKinds = observation?.InputKinds ?? stream.ObservationInputKinds;
        var observationDiagnostics = observation?.Diagnostics ?? stream.ObservationDiagnostics;
        var profileDetection = observation?.ProfileDetection ?? stream.ProfileDetection;
        var configurationComparison = observation?.ConfigurationComparison ?? stream.ConfigurationComparison;
        var qualityStates = ResolveQualityStates(stream.Values);
        var health = ResolveHealth(stream, configurationComparison, qualityStates);
        var healthDetail = ResolveHealthDetail(stream, observation, configurationComparison, qualityStates, health);
        var diagnostics = stream.Diagnostics
            .Concat(observationDiagnostics)
            .Concat(BuildQualityDiagnostics(stream.Values))
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        return new SvSubscriberStreamEvidence
        {
            Key = stream.Key,
            Health = health,
            HealthDetail = healthDetail,
            Identity = new SvSubscriberStreamIdentityEvidence
            {
                AppId = stream.AppId,
                SourceMac = stream.Source,
                DestinationMac = stream.Destination,
                VlanId = stream.VlanId,
                VlanPriority = stream.VlanPriority,
                SvId = stream.SvId,
                DataSetReference = stream.DataSet,
                ConfigurationRevision = stream.ConfRev,
                AsduPerFrame = stream.NofAsdu,
                LastSampleCount = stream.LastSmpCnt,
                DeclaredSampleRate = stream.SampleRate,
                DeclaredSampleMode = stream.SampleMode,
                SampleSynchronization = stream.SmpSynch
            },
            Runtime = new SvSubscriberRuntimeEvidence
            {
                FrameCount = stream.FrameCount,
                AsduCount = stream.AsduCount,
                ActualFramesPerSecond = stream.ActualFps,
                AverageFrameGapMilliseconds = stream.AverageFrameGapMilliseconds,
                MaximumFrameGapMilliseconds = stream.MaxFrameGapMilliseconds,
                SequenceGapCount = stream.SequenceGapCount,
                DuplicateCount = stream.DuplicateCount,
                OutOfOrderCount = stream.OutOfOrderCount,
                PayloadIssueCount = stream.PayloadIssueCount,
                SclMismatchCount = stream.SclMismatchCount,
                IsWaveformWindowReady = stream.IsWaveformWindowReady,
                LayoutBinding = stream.LayoutBinding,
                QualitySummary = BuildQualitySummary(qualityStates, stream.QualitySummary),
                CursorSummary = stream.CursorSummary,
                LastSeen = stream.LastSeen
            },
            Observation = new SvSubscriberObservationEvidence
            {
                InputKinds = inputKinds,
                LastInputKind = observation?.LastInputKind ?? inputKinds.LastOrDefault(),
                IsBoundToScl = observation?.IsBoundToScl ?? stream.IsBoundToScl,
                ControlBlockReference = observation?.ControlBlockReference ?? stream.ControlBlockReference,
                WindowFrames = facts.ObservationCount > 0
                    ? facts.ObservationCount
                    : stream.ObservationWindowFrames,
                WindowSamples = ResolveWindowSamples(facts, stream.ObservationWindowSamples),
                WindowDurationSeconds = ResolveWindowDuration(facts, stream.ObservationWindowDurationSeconds),
                FirstTimestamp = facts.FirstTimestamp,
                LastTimestamp = facts.LastTimestamp,
                Facts = facts,
                FactProvenance = facts.Provenance.Count > 0
                    ? facts.Provenance
                    : stream.FactProvenance,
                ProfileDetection = profileDetection,
                ExpectedConfiguration = observation?.ExpectedConfiguration,
                ConfigurationComparison = configurationComparison,
                Diagnostics = observationDiagnostics
            },
            Phasors = stream.Phasors.Select(phasor => new SvSubscriberPhasorEvidence
            {
                Channel = phasor.Channel,
                Kind = phasor.Kind,
                Rms = phasor.Rms,
                Peak = phasor.Peak,
                AngleDegrees = phasor.AngleDegrees
            }).ToArray(),
            Diagnostics = diagnostics
        };
    }

    private static SvStreamObservationSnapshot? ResolveObservation(
        SvSubscriberReportContext context,
        string key)
        => context.Observations.TryGetValue(key, out var observation)
            ? observation
            : null;

    private static SvObservedStreamFacts BuildFallbackFacts(SvStreamSnapshot stream)
        => new()
        {
            EtherType = 0x88BA,
            AppId = stream.AppId,
            DestinationMac = stream.Destination,
            VlanId = stream.VlanId,
            VlanPriority = stream.VlanPriority,
            SvId = stream.SvId,
            DataSetReference = stream.DataSet,
            ConfigurationRevision = stream.ConfRev,
            AsduPerFrame = stream.NofAsdu > 0 ? stream.NofAsdu : null,
            ObservedFramesPerSecond = stream.ObservedFramesPerSecond,
            ObservedSamplesPerSecond = stream.ObservedSamplesPerSecond,
            ObservedCounterWrap = stream.ObservedCounterWrap,
            DeclaredSampleRate = stream.SampleRate,
            DeclaredSampleMode = stream.SampleMode,
            Provenance = stream.FactProvenance,
            ObservationCount = stream.ObservationWindowFrames,
            Diagnostics = stream.ObservationDiagnostics
        };

    private static int ResolveWindowSamples(SvObservedStreamFacts facts, int fallback)
    {
        if (facts.ObservationCount > 0)
            return facts.ObservationCount * Math.Max(1, facts.AsduPerFrame ?? 1);
        return Math.Max(0, fallback);
    }

    private static string ResolveHealth(
        SvStreamSnapshot stream,
        SvConfigurationComparisonResult? comparison,
        IReadOnlyList<SvQualityState> qualityStates)
    {
        var hasQualityBad = qualityStates.Any(item => item.Severity is SvQualitySeverity.Bad or SvQualitySeverity.Unknown);
        var hasQualityWarning = qualityStates.Any(item => item.Severity == SvQualitySeverity.Warning);

        if (comparison?.HasBlockingErrors == true || hasQualityBad)
            return "BAD";
        if (stream.Health == "BAD")
            return "BAD";
        if (comparison?.WarningCount > 0 || hasQualityWarning)
            return "WARN";
        return stream.Health;
    }

    private static string ResolveHealthDetail(
        SvStreamSnapshot stream,
        SvStreamObservationSnapshot? observation,
        SvConfigurationComparisonResult? comparison,
        IReadOnlyList<SvQualityState> qualityStates,
        string health)
    {
        var blocking = comparison?.Findings
            .FirstOrDefault(item => item.Severity == SvConfigurationFindingSeverity.Error);
        if (blocking is not null)
            return blocking.Message;

        var badQuality = qualityStates.FirstOrDefault(item =>
            item.Severity is SvQualitySeverity.Bad or SvQualitySeverity.Unknown);
        if (badQuality is not null)
            return $"Sample quality requires attention: {badQuality.Summary}.";

        if (stream.Health == "BAD")
            return stream.HealthDetail;

        var warning = comparison?.Findings
            .FirstOrDefault(item => item.Severity == SvConfigurationFindingSeverity.Warning);
        if (warning is not null)
            return warning.Message;

        var warningQuality = qualityStates.FirstOrDefault(item => item.Severity == SvQualitySeverity.Warning);
        if (warningQuality is not null)
            return $"Sample quality warning: {warningQuality.Summary}.";

        if (health == "GOOD")
            return "SV stream is stable, quality is usable, and the configured expectation has no blocking mismatch.";
        if (observation?.IsBoundToScl != true)
            return "SV stream is not bound to an SCL expectation.";
        return stream.HealthDetail;
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

    private static IEnumerable<string> BuildQualityDiagnostics(IEnumerable<DecodedValueRow> values)
    {
        foreach (var value in values.Where(item => item.IsQuality))
        {
            if (value.QualityState is not { } quality)
            {
                yield return $"QUALITY · {value.Signal} · undecoded · raw {value.Raw}";
                continue;
            }

            yield return $"QUALITY · {value.Signal} · {quality.Severity} · {quality.Summary} · placement {quality.Placement} · raw {value.Raw}";
        }
    }

    private static double ResolveWindowDuration(SvObservedStreamFacts facts, double fallback)
    {
        if (facts.FirstTimestamp.HasValue && facts.LastTimestamp.HasValue)
            return Math.Max(0, (facts.LastTimestamp.Value - facts.FirstTimestamp.Value).TotalSeconds);
        return Math.Max(0, fallback);
    }

    private static string ResolveCaptureSource(IReadOnlyList<SvSubscriberStreamEvidence> streams)
    {
        var kinds = streams.SelectMany(stream => stream.Observation.InputKinds)
            .Where(kind => kind != SvObservationInputKind.Unknown)
            .Distinct()
            .OrderBy(kind => kind)
            .ToArray();
        return kinds.Length switch
        {
            0 => "Unknown",
            1 => kinds[0].ToString(),
            _ => "Mixed: " + string.Join(", ", kinds)
        };
    }

    private static SvSubscriberSoftwareEvidence ResolveSoftwareEvidence()
    {
        var assembly = typeof(SvSubscriberReportBuilder).Assembly;
        var informationalVersion = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion ?? string.Empty;
        var product = assembly.GetCustomAttribute<AssemblyProductAttribute>()?.Product;
        var version = assembly.GetName().Version?.ToString() ?? "unknown";

        return new SvSubscriberSoftwareEvidence
        {
            Product = string.IsNullOrWhiteSpace(product) ? "ARSVIN Subscriber" : product,
            Version = version,
            InformationalVersion = string.IsNullOrWhiteSpace(informationalVersion)
                ? version
                : informationalVersion,
            Commit = ResolveCommit(informationalVersion),
            Repository = RepositoryUrl
        };
    }

    private static string ResolveCommit(string informationalVersion)
    {
        foreach (var environmentVariable in new[] { "ARSVIN_COMMIT_SHA", "GITHUB_SHA" })
        {
            var value = Environment.GetEnvironmentVariable(environmentVariable);
            if (!string.IsNullOrWhiteSpace(value))
                return value.Trim();
        }

        var separator = informationalVersion.LastIndexOf('+');
        if (separator >= 0 && separator + 1 < informationalVersion.Length)
        {
            var candidate = informationalVersion[(separator + 1)..].Trim();
            if (candidate.Length >= 7 && candidate.All(Uri.IsHexDigit))
                return candidate;
        }

        return "unknown";
    }
}
