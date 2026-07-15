namespace AR.Iec61850.SampledValues.Profiles;

public enum SvSamplingBasis
{
    Unspecified,
    SamplesPerCycle,
    SamplesPerSecond,
    Custom
}

public enum SvProfileEvidenceStatus
{
    ResearchCandidate,
    ImplementedGeneric,
    VerifiedStandard,
    VerifiedCapture,
    VerifiedLab
}

public enum SvProfileClassificationScope
{
    TrafficFamily,
    Profile
}

public enum SvProfileConfidence
{
    Unknown,
    Possible,
    Likely,
    Confirmed,
    Conflict
}

public enum SvProfileEvidenceOutcome
{
    Match,
    Conflict,
    Unknown
}

public enum SvFactProvenance
{
    Unavailable,
    WireObserved,
    CaptureCalculated,
    SclDerived,
    TrustedContext,
    ProfileInferred
}

public enum SvObservationWindowQuality
{
    Empty,
    Insufficient,
    Ready,
    Degraded
}

public sealed record SvObservationWindowPolicy
{
    public int MaximumObservations { get; init; } = 4096;
    public TimeSpan MaximumAge { get; init; } = TimeSpan.FromSeconds(5);
    public int MinimumObservations { get; init; } = 3;
    public TimeSpan MinimumDuration { get; init; } = TimeSpan.FromMilliseconds(250);
    public int ReorderTolerance { get; init; } = 8;
    public ushort WrapLowWatermark { get; init; } = 8;
    public ushort MinimumWrapPredecessor { get; init; } = 255;

    public void Validate()
    {
        if (MaximumObservations <= 0)
            throw new InvalidOperationException("SV observation windows require a positive maximum observation count.");
        if (MaximumAge <= TimeSpan.Zero)
            throw new InvalidOperationException("SV observation windows require a positive maximum age.");
        if (MinimumObservations <= 0 || MinimumObservations > MaximumObservations)
            throw new InvalidOperationException("SV observation windows contain an invalid minimum observation count.");
        if (MinimumDuration < TimeSpan.Zero || MinimumDuration > MaximumAge)
            throw new InvalidOperationException("SV observation windows contain an invalid minimum duration.");
        if (ReorderTolerance < 0)
            throw new InvalidOperationException("SV observation windows contain an invalid reorder tolerance.");
        if (MinimumWrapPredecessor <= WrapLowWatermark)
            throw new InvalidOperationException("SV wrap detection requires the predecessor threshold to exceed the low-watermark threshold.");
    }
}

public sealed record SvCounterSequenceSummary
{
    public int ContinuousTransitions { get; init; }
    public int GapTransitions { get; init; }
    public int EstimatedMissingSamples { get; init; }
    public int DuplicateTransitions { get; init; }
    public int WrapTransitions { get; init; }
    public int OutOfOrderTransitions { get; init; }
    public int ResetTransitions { get; init; }
    public int? ConfirmedWrap { get; init; }

    public bool HasAnomalies =>
        GapTransitions > 0 ||
        DuplicateTransitions > 0 ||
        OutOfOrderTransitions > 0 ||
        ResetTransitions > 0;
}

public sealed record SvFactProvenanceEntry(
    string Field,
    SvFactProvenance Provenance,
    string Explanation);

public sealed record SvProfileSourceEvidence(
    string SourceId,
    string Description,
    SvProfileEvidenceStatus Status);

public sealed record SvDatasetElementSignature
{
    public string BType { get; init; } = string.Empty;
    public string Cdc { get; init; } = string.Empty;
    public bool IsQuality { get; init; }
    public bool IsTimestamp { get; init; }

    public string NormalizedBType => Normalize(BType);
    public string NormalizedCdc => Normalize(Cdc);

    private static string Normalize(string value)
        => new((value ?? string.Empty)
            .Where(char.IsLetterOrDigit)
            .Select(char.ToUpperInvariant)
            .ToArray());
}

public sealed record SvObservedStreamFacts
{
    public ushort? EtherType { get; init; }
    public ushort? AppId { get; init; }
    public string DestinationMac { get; init; } = string.Empty;
    public ushort? VlanId { get; init; }
    public byte? VlanPriority { get; init; }
    public string SvId { get; init; } = string.Empty;
    public string DataSetReference { get; init; } = string.Empty;
    public uint? ConfigurationRevision { get; init; }
    public int? AsduPerFrame { get; init; }
    public int? PayloadBytesPerAsdu { get; init; }
    public double? ObservedFramesPerSecond { get; init; }
    public double? ObservedSamplesPerSecond { get; init; }
    public int? ObservedCounterWrap { get; init; }
    public ushort? DeclaredSampleRate { get; init; }
    public ushort? DeclaredSampleMode { get; init; }
    public double? NominalFrequencyHz { get; init; }
    public IReadOnlyList<SvDatasetElementSignature> DataSetSignature { get; init; }
        = Array.Empty<SvDatasetElementSignature>();
    public int ObservationCount { get; init; }
    public DateTimeOffset? FirstTimestamp { get; init; }
    public DateTimeOffset? LastTimestamp { get; init; }
    public TimeSpan ObservationDuration { get; init; }
    public SvObservationWindowQuality WindowQuality { get; init; }
    public SvCounterSequenceSummary CounterSequence { get; init; } = new();
    public IReadOnlyList<SvFactProvenanceEntry> Provenance { get; init; }
        = Array.Empty<SvFactProvenanceEntry>();
    public IReadOnlyList<string> Diagnostics { get; init; } = Array.Empty<string>();

    public bool IsWindowSufficient =>
        WindowQuality is SvObservationWindowQuality.Ready or SvObservationWindowQuality.Degraded;

    public SvFactProvenance GetProvenance(string field)
        => Provenance.FirstOrDefault(item =>
                item.Field.Equals(field, StringComparison.Ordinal))?.Provenance
            ?? SvFactProvenance.Unavailable;
}

public sealed record SvProfileDefinition
{
    public string Id { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string Family { get; init; } = string.Empty;
    public SvProfileClassificationScope ClassificationScope { get; init; } = SvProfileClassificationScope.Profile;
    public SvSamplingBasis SamplingBasis { get; init; }
    public ushort? ExpectedEtherType { get; init; }
    public IReadOnlyList<int> AllowedAsduPerFrame { get; init; } = Array.Empty<int>();
    public int? ExpectedPayloadBytesPerAsdu { get; init; }
    public int? ExpectedDataSetElementCount { get; init; }
    public IReadOnlyList<SvDatasetElementSignature> ExpectedDataSetSignature { get; init; }
        = Array.Empty<SvDatasetElementSignature>();
    public double? ExpectedSamplesPerCycle { get; init; }
    public double? ExpectedSamplesPerSecond { get; init; }
    public IReadOnlyList<double> AllowedNominalFrequenciesHz { get; init; } = Array.Empty<double>();
    public int? ExpectedCounterWrap { get; init; }
    public double RateTolerancePercent { get; init; } = 1.0;
    public SvProfileEvidenceStatus EvidenceStatus { get; init; } = SvProfileEvidenceStatus.ResearchCandidate;
    public IReadOnlyList<SvProfileSourceEvidence> Sources { get; init; }
        = Array.Empty<SvProfileSourceEvidence>();

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Id))
            throw new InvalidOperationException("SV profile definition requires a stable ID.");
        if (string.IsNullOrWhiteSpace(DisplayName))
            throw new InvalidOperationException($"SV profile '{Id}' requires a display name.");
        if (Sources.Count == 0)
            throw new InvalidOperationException($"SV profile '{Id}' requires at least one evidence source.");
        if (Sources.Any(source => string.IsNullOrWhiteSpace(source.SourceId) || string.IsNullOrWhiteSpace(source.Description)))
            throw new InvalidOperationException($"SV profile '{Id}' contains incomplete evidence-source metadata.");
        if (RateTolerancePercent < 0 || RateTolerancePercent > 100)
            throw new InvalidOperationException($"SV profile '{Id}' has an invalid rate tolerance.");
        if (AllowedAsduPerFrame.Any(value => value <= 0))
            throw new InvalidOperationException($"SV profile '{Id}' contains an invalid ASDU-per-frame value.");
        if (AllowedAsduPerFrame.Count != AllowedAsduPerFrame.Distinct().Count())
            throw new InvalidOperationException($"SV profile '{Id}' contains duplicate ASDU-per-frame values.");
        if (ExpectedPayloadBytesPerAsdu is <= 0)
            throw new InvalidOperationException($"SV profile '{Id}' contains an invalid payload length.");
        if (ExpectedDataSetElementCount is < 0)
            throw new InvalidOperationException($"SV profile '{Id}' contains an invalid dataset element count.");
        if (ExpectedDataSetElementCount.HasValue &&
            ExpectedDataSetSignature.Count > 0 &&
            ExpectedDataSetElementCount.Value != ExpectedDataSetSignature.Count)
        {
            throw new InvalidOperationException($"SV profile '{Id}' dataset count does not match its ordered signature.");
        }
        if (ExpectedCounterWrap is <= 1)
            throw new InvalidOperationException($"SV profile '{Id}' contains an invalid sample-counter wrap.");
        if (AllowedNominalFrequenciesHz.Any(value => value <= 0 || double.IsNaN(value) || double.IsInfinity(value)))
            throw new InvalidOperationException($"SV profile '{Id}' contains an invalid nominal frequency.");
        if (ExpectedSamplesPerCycle is <= 0 || ExpectedSamplesPerSecond is <= 0)
            throw new InvalidOperationException($"SV profile '{Id}' contains an invalid sampling expectation.");
        if (ExpectedSamplesPerCycle.HasValue && ExpectedSamplesPerSecond.HasValue)
            throw new InvalidOperationException($"SV profile '{Id}' cannot define both samples-per-cycle and samples-per-second expectations.");

        switch (SamplingBasis)
        {
            case SvSamplingBasis.SamplesPerCycle when !ExpectedSamplesPerCycle.HasValue:
                throw new InvalidOperationException($"SV profile '{Id}' requires a samples-per-cycle expectation.");
            case SvSamplingBasis.SamplesPerSecond when !ExpectedSamplesPerSecond.HasValue:
                throw new InvalidOperationException($"SV profile '{Id}' requires a samples-per-second expectation.");
            case SvSamplingBasis.SamplesPerCycle when ExpectedSamplesPerSecond.HasValue:
            case SvSamplingBasis.SamplesPerSecond when ExpectedSamplesPerCycle.HasValue:
                throw new InvalidOperationException($"SV profile '{Id}' sampling expectation conflicts with its sampling basis.");
        }
    }
}

public sealed record SvProfileMatchEvidence(
    string Field,
    SvProfileEvidenceOutcome Outcome,
    int Weight,
    string Expected,
    string Observed,
    string Message);

public sealed record SvProfileDetectionResult
{
    public SvProfileDefinition Profile { get; init; } = new();
    public SvProfileConfidence Confidence { get; init; }
    public SvProfileConfidence UncappedConfidence { get; init; }
    public double ScorePercent { get; init; }
    public int MatchedWeight { get; init; }
    public int ConflictWeight { get; init; }
    public int EvaluatedWeight { get; init; }
    public IReadOnlyList<SvProfileMatchEvidence> Evidence { get; init; }
        = Array.Empty<SvProfileMatchEvidence>();

    public bool HasConflicts => Evidence.Any(item => item.Outcome == SvProfileEvidenceOutcome.Conflict);
    public bool ConfidenceLimitedByEvidence => Confidence != UncappedConfidence;
}
