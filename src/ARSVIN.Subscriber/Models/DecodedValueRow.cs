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

    /// <summary>Engineering value produced by protocol/profile scaling.</summary>
    public double? EngineeringValue { get; init; }
    public string EngineeringUnit { get; init; } = string.Empty;
    public SvEngineeringScaleSource ScalingSource { get; init; } = SvEngineeringScaleSource.RawOnly;
    public SvEngineeringScaleConfidence ScalingConfidence { get; init; } = SvEngineeringScaleConfidence.Unknown;
    public string ScalingReason { get; init; } = string.Empty;

    /// <summary>Optional explicit primary/secondary interpretation supplied by measurement context.</summary>
    public SvMeasurementDomainValue? DomainValue { get; init; }
    public SvMeasurementValueDomain PreferredDisplayDomain { get; init; } = SvMeasurementValueDomain.Unknown;

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
            if (TryResolvePreferredDisplay(out var value, out var unit))
                return $"{value:0.###} {unit}".TrimEnd();
            return HasEngineeringValue
                ? $"{EngineeringValue:0.###} {EngineeringUnit}".TrimEnd()
                : Value;
        }
    }

    public string MeasurementDomainText
    {
        get
        {
            if (QualityState is not null)
                return "Quality";
            if (DomainValue is null)
                return HasEngineeringValue ? "Engineering" : "Raw";

            if (PreferredDisplayDomain == SvMeasurementValueDomain.PrimaryEngineering && DomainValue.PrimaryValue.HasValue)
                return "Primary";
            if (PreferredDisplayDomain == SvMeasurementValueDomain.SecondaryEquivalent && DomainValue.SecondaryEquivalentValue.HasValue)
                return "Secondary";
            return $"{DomainValue.WireDomain} fallback";
        }
    }

    public string ScalingText
    {
        get
        {
            if (QualityState is { } quality)
                return $"Quality · {quality.Severity} · {quality.Placement}";
            return HasEngineeringValue
                ? $"{ScalingConfidence} · {ScalingSource} · {MeasurementDomainText}"
                : "Raw counts";
        }
    }

    private bool TryResolvePreferredDisplay(out double value, out string unit)
    {
        value = 0;
        unit = EngineeringUnit;
        if (DomainValue is null)
            return false;

        if (PreferredDisplayDomain == SvMeasurementValueDomain.PrimaryEngineering &&
            DomainValue.PrimaryValue.HasValue)
        {
            value = DomainValue.PrimaryValue.Value;
            unit = DomainValue.Unit;
            return true;
        }

        if (PreferredDisplayDomain == SvMeasurementValueDomain.SecondaryEquivalent &&
            DomainValue.SecondaryEquivalentValue.HasValue)
        {
            value = DomainValue.SecondaryEquivalentValue.Value;
            unit = DomainValue.Unit;
            return true;
        }

        if (DomainValue.WireDomain is SvMeasurementValueDomain.PrimaryEngineering or SvMeasurementValueDomain.SecondaryEquivalent)
        {
            value = DomainValue.WireValue;
            unit = DomainValue.Unit;
            return true;
        }

        return false;
    }
}
