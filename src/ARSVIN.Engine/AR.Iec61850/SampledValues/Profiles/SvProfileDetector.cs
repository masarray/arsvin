namespace AR.Iec61850.SampledValues.Profiles;

public sealed class SvProfileDetector
{
    private const int EtherTypeWeight = 5;
    private const int AsduWeight = 12;
    private const int PayloadWeight = 18;
    private const int DataSetCountWeight = 12;
    private const int DataSetSignatureWeight = 25;
    private const int SamplingRateWeight = 25;
    private const int NominalFrequencyWeight = 8;
    private const int CounterWrapWeight = 15;
    private const int MinimumPossibleWeight = 15;
    private const int MinimumLikelyWeight = 40;
    private const int MinimumConfirmedWeight = 70;

    public IReadOnlyList<SvProfileDetectionResult> Detect(
        SvObservedStreamFacts facts,
        IEnumerable<SvProfileDefinition> profiles)
    {
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(profiles);

        return profiles
            .Select(profile => Evaluate(facts, profile))
            .OrderByDescending(result => ConfidenceRank(result.Confidence))
            .ThenByDescending(result => result.ScorePercent)
            .ThenByDescending(result => result.EvaluatedWeight)
            .ThenBy(result => result.Profile.DisplayName, StringComparer.Ordinal)
            .ToArray();
    }

    public SvProfileDetectionResult? DetectBest(
        SvObservedStreamFacts facts,
        IEnumerable<SvProfileDefinition> profiles)
        => Detect(facts, profiles).FirstOrDefault();

    public SvProfileDetectionResult Evaluate(
        SvObservedStreamFacts facts,
        SvProfileDefinition profile)
    {
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(profile);
        profile.Validate();

        var evidence = new List<SvProfileMatchEvidence>();

        CompareNullable(
            "EtherType",
            profile.ExpectedEtherType,
            facts.EtherType,
            EtherTypeWeight,
            value => $"0x{value:X4}",
            evidence);

        CompareAllowed(
            "ASDU per frame",
            profile.AllowedAsduPerFrame,
            facts.AsduPerFrame,
            AsduWeight,
            evidence);

        CompareNullable(
            "Payload bytes per ASDU",
            profile.ExpectedPayloadBytesPerAsdu,
            facts.PayloadBytesPerAsdu,
            PayloadWeight,
            value => value.ToString(),
            evidence);

        CompareNullable(
            "Dataset element count",
            profile.ExpectedDataSetElementCount,
            facts.DataSetSignature.Count > 0 ? facts.DataSetSignature.Count : null,
            DataSetCountWeight,
            value => value.ToString(),
            evidence);

        CompareSignature(profile.ExpectedDataSetSignature, facts.DataSetSignature, evidence);
        CompareSampling(profile, facts, evidence);

        CompareAllowedDouble(
            "Nominal frequency",
            profile.AllowedNominalFrequenciesHz,
            facts.NominalFrequencyHz,
            NominalFrequencyWeight,
            profile.RateTolerancePercent,
            evidence);

        CompareNullable(
            "Sample-counter wrap",
            profile.ExpectedCounterWrap,
            facts.ObservedCounterWrap,
            CounterWrapWeight,
            value => value.ToString(),
            evidence);

        var evaluatedWeight = evidence
            .Where(item => item.Outcome != SvProfileEvidenceOutcome.Unknown)
            .Sum(item => item.Weight);
        var matchedWeight = evidence
            .Where(item => item.Outcome == SvProfileEvidenceOutcome.Match)
            .Sum(item => item.Weight);
        var conflictWeight = evidence
            .Where(item => item.Outcome == SvProfileEvidenceOutcome.Conflict)
            .Sum(item => item.Weight);
        var score = evaluatedWeight == 0
            ? 0
            : Math.Round((double)matchedWeight / evaluatedWeight * 100, 2);
        var hasDataSetSignatureMatch = HasMatch(evidence, "Dataset signature");
        var hasSamplingMatch = HasMatch(evidence, "Observed samples per second") ||
                               HasMatch(evidence, "Samples per cycle");

        return new SvProfileDetectionResult
        {
            Profile = profile,
            Confidence = ResolveConfidence(
                score,
                matchedWeight,
                conflictWeight,
                evaluatedWeight,
                hasDataSetSignatureMatch,
                hasSamplingMatch),
            ScorePercent = score,
            MatchedWeight = matchedWeight,
            ConflictWeight = conflictWeight,
            EvaluatedWeight = evaluatedWeight,
            Evidence = evidence
        };
    }

    private static SvProfileConfidence ResolveConfidence(
        double score,
        int matchedWeight,
        int conflictWeight,
        int evaluatedWeight,
        bool hasDataSetSignatureMatch,
        bool hasSamplingMatch)
    {
        if (evaluatedWeight == 0)
            return SvProfileConfidence.Unknown;
        if (conflictWeight >= matchedWeight && conflictWeight > 0)
            return SvProfileConfidence.Conflict;
        if (conflictWeight == 0 &&
            score >= 90 &&
            evaluatedWeight >= MinimumConfirmedWeight &&
            hasDataSetSignatureMatch &&
            hasSamplingMatch)
        {
            return SvProfileConfidence.Confirmed;
        }
        if (score >= 70 && evaluatedWeight >= MinimumLikelyWeight)
            return SvProfileConfidence.Likely;
        if (score >= 45 && evaluatedWeight >= MinimumPossibleWeight)
            return SvProfileConfidence.Possible;
        if (conflictWeight > 0)
            return SvProfileConfidence.Conflict;
        return SvProfileConfidence.Unknown;
    }

    private static int ConfidenceRank(SvProfileConfidence confidence)
        => confidence switch
        {
            SvProfileConfidence.Confirmed => 4,
            SvProfileConfidence.Likely => 3,
            SvProfileConfidence.Possible => 2,
            SvProfileConfidence.Unknown => 1,
            SvProfileConfidence.Conflict => 0,
            _ => 0
        };

    private static bool HasMatch(
        IEnumerable<SvProfileMatchEvidence> evidence,
        string field)
        => evidence.Any(item =>
            item.Field.Equals(field, StringComparison.Ordinal) &&
            item.Outcome == SvProfileEvidenceOutcome.Match);

    private static void CompareSampling(
        SvProfileDefinition profile,
        SvObservedStreamFacts facts,
        List<SvProfileMatchEvidence> evidence)
    {
        switch (profile.SamplingBasis)
        {
            case SvSamplingBasis.SamplesPerSecond when profile.ExpectedSamplesPerSecond.HasValue:
                CompareApproximate(
                    "Observed samples per second",
                    profile.ExpectedSamplesPerSecond.Value,
                    facts.ObservedSamplesPerSecond,
                    SamplingRateWeight,
                    profile.RateTolerancePercent,
                    evidence);
                break;

            case SvSamplingBasis.SamplesPerCycle when profile.ExpectedSamplesPerCycle.HasValue:
                if (!facts.ObservedSamplesPerSecond.HasValue || !facts.NominalFrequencyHz.HasValue)
                {
                    evidence.Add(Unknown(
                        "Samples per cycle",
                        SamplingRateWeight,
                        profile.ExpectedSamplesPerCycle.Value.ToString("0.###"),
                        "-",
                        "Observed rate and nominal frequency are both required."));
                    break;
                }

                var observedSamplesPerCycle = facts.ObservedSamplesPerSecond.Value / facts.NominalFrequencyHz.Value;
                CompareApproximate(
                    "Samples per cycle",
                    profile.ExpectedSamplesPerCycle.Value,
                    observedSamplesPerCycle,
                    SamplingRateWeight,
                    profile.RateTolerancePercent,
                    evidence);
                break;
        }
    }

    private static void CompareSignature(
        IReadOnlyList<SvDatasetElementSignature> expected,
        IReadOnlyList<SvDatasetElementSignature> observed,
        List<SvProfileMatchEvidence> evidence)
    {
        if (expected.Count == 0)
            return;
        if (observed.Count == 0)
        {
            evidence.Add(Unknown(
                "Dataset signature",
                DataSetSignatureWeight,
                SignatureText(expected),
                "-",
                "No dataset signature is available for this observation window."));
            return;
        }

        var expectedKeys = expected.Select(ToKey).ToArray();
        var observedKeys = observed.Select(ToKey).ToArray();
        var matches = expectedKeys.SequenceEqual(observedKeys, StringComparer.Ordinal);
        evidence.Add(new SvProfileMatchEvidence(
            "Dataset signature",
            matches ? SvProfileEvidenceOutcome.Match : SvProfileEvidenceOutcome.Conflict,
            DataSetSignatureWeight,
            SignatureText(expected),
            SignatureText(observed),
            matches
                ? "Dataset element order and types match the profile definition."
                : "Dataset element order or types conflict with the profile definition."));
    }

    private static void CompareAllowed(
        string field,
        IReadOnlyList<int> allowed,
        int? observed,
        int weight,
        List<SvProfileMatchEvidence> evidence)
    {
        if (allowed.Count == 0)
            return;
        if (!observed.HasValue)
        {
            evidence.Add(Unknown(field, weight, string.Join("/", allowed), "-", $"Observed {field} is unavailable."));
            return;
        }

        var matches = allowed.Contains(observed.Value);
        evidence.Add(new SvProfileMatchEvidence(
            field,
            matches ? SvProfileEvidenceOutcome.Match : SvProfileEvidenceOutcome.Conflict,
            weight,
            string.Join("/", allowed),
            observed.Value.ToString(),
            matches ? $"Observed {field} is allowed." : $"Observed {field} is not allowed by the profile definition."));
    }

    private static void CompareAllowedDouble(
        string field,
        IReadOnlyList<double> allowed,
        double? observed,
        int weight,
        double tolerancePercent,
        List<SvProfileMatchEvidence> evidence)
    {
        if (allowed.Count == 0)
            return;
        if (!observed.HasValue)
        {
            evidence.Add(Unknown(field, weight, string.Join("/", allowed.Select(value => value.ToString("0.###"))), "-", $"Observed {field} is unavailable."));
            return;
        }

        var matches = allowed.Any(value => IsWithinTolerance(value, observed.Value, tolerancePercent));
        evidence.Add(new SvProfileMatchEvidence(
            field,
            matches ? SvProfileEvidenceOutcome.Match : SvProfileEvidenceOutcome.Conflict,
            weight,
            string.Join("/", allowed.Select(value => value.ToString("0.###"))),
            observed.Value.ToString("0.###"),
            matches ? $"Observed {field} is allowed." : $"Observed {field} conflicts with the allowed values."));
    }

    private static void CompareApproximate(
        string field,
        double expected,
        double? observed,
        int weight,
        double tolerancePercent,
        List<SvProfileMatchEvidence> evidence)
    {
        if (!observed.HasValue)
        {
            evidence.Add(Unknown(field, weight, expected.ToString("0.###"), "-", $"Observed {field} is unavailable."));
            return;
        }

        var matches = IsWithinTolerance(expected, observed.Value, tolerancePercent);
        evidence.Add(new SvProfileMatchEvidence(
            field,
            matches ? SvProfileEvidenceOutcome.Match : SvProfileEvidenceOutcome.Conflict,
            weight,
            expected.ToString("0.###"),
            observed.Value.ToString("0.###"),
            matches
                ? $"Observed {field} is within {tolerancePercent:0.###}% tolerance."
                : $"Observed {field} is outside {tolerancePercent:0.###}% tolerance."));
    }

    private static void CompareNullable<T>(
        string field,
        T? expected,
        T? observed,
        int weight,
        Func<T, string> format,
        List<SvProfileMatchEvidence> evidence)
        where T : struct, IEquatable<T>
    {
        if (!expected.HasValue)
            return;
        if (!observed.HasValue)
        {
            evidence.Add(Unknown(field, weight, format(expected.Value), "-", $"Observed {field} is unavailable."));
            return;
        }

        var matches = expected.Value.Equals(observed.Value);
        evidence.Add(new SvProfileMatchEvidence(
            field,
            matches ? SvProfileEvidenceOutcome.Match : SvProfileEvidenceOutcome.Conflict,
            weight,
            format(expected.Value),
            format(observed.Value),
            matches ? $"Observed {field} matches." : $"Observed {field} conflicts with the profile definition."));
    }

    private static bool IsWithinTolerance(double expected, double observed, double tolerancePercent)
    {
        if (expected == 0)
            return observed == 0;
        return Math.Abs(observed - expected) / Math.Abs(expected) * 100 <= tolerancePercent;
    }

    private static SvProfileMatchEvidence Unknown(
        string field,
        int weight,
        string expected,
        string observed,
        string message)
        => new(field, SvProfileEvidenceOutcome.Unknown, weight, expected, observed, message);

    private static string SignatureText(IReadOnlyList<SvDatasetElementSignature> signature)
        => string.Join(", ", signature.Select(element => element.NormalizedBType));

    private static string ToKey(SvDatasetElementSignature element)
        => $"{element.NormalizedBType}|{element.NormalizedCdc}|{element.IsQuality}|{element.IsTimestamp}";
}

public static class SvProfileCatalog
{
    public static SvProfileDefinition GenericSclLayer2 { get; } = new()
    {
        Id = "generic-scl-layer2",
        DisplayName = "Generic SCL-driven Layer-2 SV",
        Family = "Generic Layer-2 SV",
        SamplingBasis = SvSamplingBasis.Custom,
        ExpectedEtherType = 0x88BA,
        EvidenceStatus = SvProfileEvidenceStatus.ImplementedGeneric,
        Sources =
        [
            new SvProfileSourceEvidence(
                "arsvin-engine",
                "Generic Layer-2 SV mechanisms implemented by the shared engine without a profile-specific conformance claim.",
                SvProfileEvidenceStatus.ImplementedGeneric)
        ]
    };

    public static IReadOnlyList<SvProfileDefinition> BuiltIn { get; }
        = [GenericSclLayer2];
}
