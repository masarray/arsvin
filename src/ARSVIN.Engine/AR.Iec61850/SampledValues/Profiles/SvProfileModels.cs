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
    public IReadOnlyList<string> Diagnostics { get; init; } = Array.Empty<string>();
}

public sealed record SvProfileDefinition
{
    public string Id { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string Family { get; init; } = string.Empty;
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
        if (RateTolerancePercent < 0 || RateTolerancePercent > 100)
            throw new InvalidOperationException($"SV profile '{Id}' has an invalid rate tolerance.");
        if (AllowedAsduPerFrame.Any(value => value <= 0))
            throw new InvalidOperationException($"SV profile '{Id}' contains an invalid ASDU-per-frame value.");
        if (ExpectedPayloadBytesPerAsdu is <= 0)
            throw new InvalidOperationException($"SV profile '{Id}' contains an invalid payload length.");
        if (ExpectedDataSetElementCount is < 0)
            throw new InvalidOperationException($"SV profile '{Id}' contains an invalid dataset element count.");
        if (ExpectedCounterWrap is <= 1)
            throw new InvalidOperationException($"SV profile '{Id}' contains an invalid sample-counter wrap.");
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
    public double ScorePercent { get; init; }
    public int MatchedWeight { get; init; }
    public int ConflictWeight { get; init; }
    public int EvaluatedWeight { get; init; }
    public IReadOnlyList<SvProfileMatchEvidence> Evidence { get; init; }
        = Array.Empty<SvProfileMatchEvidence>();

    public bool HasConflicts => Evidence.Any(item => item.Outcome == SvProfileEvidenceOutcome.Conflict);
}
