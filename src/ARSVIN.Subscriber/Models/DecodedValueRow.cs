using AR.Iec61850.SampledValues.Measurements;

namespace ARSVIN.Subscriber.Models;

public sealed class DecodedValueRow
{
    public int Index { get; init; }
    public string Signal { get; init; } = string.Empty;
    public string Kind { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
    public string Raw { get; init; } = string.Empty;

    /// <summary>Decoded protocol number before any engineering scaling.</summary>
    public double? NumericValue { get; init; }

    /// <summary>Primary engineering value when scaling evidence is sufficient.</summary>
    public double? EngineeringValue { get; init; }
    public string EngineeringUnit { get; init; } = string.Empty;
    public SvEngineeringScaleSource ScalingSource { get; init; } = SvEngineeringScaleSource.RawOnly;
    public SvEngineeringScaleConfidence ScalingConfidence { get; init; } = SvEngineeringScaleConfidence.Unknown;
    public string ScalingReason { get; init; } = string.Empty;

    public bool HasEngineeringValue => EngineeringValue.HasValue && ScalingSource != SvEngineeringScaleSource.RawOnly;
    public bool IsQuality => Kind.Contains("Quality", StringComparison.OrdinalIgnoreCase);
    public SvQualityState? QualityState => IsQuality && SvQualityDecoder.TryDecodeHex(Raw, out var quality)
        ? quality
        : null;
    public string QualitySeverity => QualityState?.Severity.ToString() ?? string.Empty;
    public string QualityPlacement => QualityState?.Placement.ToString() ?? string.Empty;

    public string DisplayValue
    {
        get
        {
            if (QualityState is { } quality)
                return quality.Summary;
            return HasEngineeringValue
                ? $"{EngineeringValue:0.###} {EngineeringUnit}"
                : Value;
        }
    }

    public string ScalingText
    {
        get
        {
            if (QualityState is { } quality)
                return $"Quality · {quality.Severity} · {quality.Placement}";
            return HasEngineeringValue
                ? $"{ScalingConfidence} · {ScalingSource}"
                : "Raw counts";
        }
    }
}
