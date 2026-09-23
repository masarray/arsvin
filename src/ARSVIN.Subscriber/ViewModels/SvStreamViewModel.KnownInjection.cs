using System.Globalization;
using AR.Iec61850.SampledValues.Field;

namespace ARSVIN.Subscriber.ViewModels;

public sealed partial class SvStreamViewModel
{
    public void RecordKnownInjectionEvidence(
        SvKnownInjectionExpectation expectation,
        SvKnownInjectionMeasurement measurement,
        SvKnownInjectionComparison comparison)
    {
        ArgumentNullException.ThrowIfNull(expectation);
        ArgumentNullException.ThrowIfNull(measurement);
        ArgumentNullException.ThrowIfNull(comparison);

        var error = comparison.AmplitudeErrorPercent.HasValue
            ? $"{comparison.AmplitudeErrorPercent.Value:+0.###;-0.###;0}%"
            : comparison.AbsoluteAmplitudeError.ToString("+0.######;-0.######;0", CultureInfo.InvariantCulture);
        var evidence =
            $"INJECTION · {expectation.Channel} · {comparison.State.ToString().ToUpperInvariant()} · " +
            $"expected {expectation.ExpectedRms:0.######} {expectation.Unit} · " +
            $"measured {measurement.MeasuredRms:0.######} {expectation.Unit} · error {error}";

        for (var index = _evidenceDetails.Count - 1; index >= 0; index--)
        {
            if (_evidenceDetails[index].StartsWith($"INJECTION · {expectation.Channel} ·", StringComparison.Ordinal))
                _evidenceDetails.RemoveAt(index);
        }
        _evidenceDetails.Insert(0, evidence);

        MeasurementFieldState = comparison.State == SvKnownInjectionResultState.Pass
            ? "GOOD"
            : comparison.State == SvKnownInjectionResultState.Fail
                ? "BAD"
                : "WARN";
        MeasurementFieldDetail = evidence;
        FieldSummary = $"CAPTURE {CaptureFieldState} · PROTOCOL {ProtocolFieldState} · STREAM {StreamFieldState} · CONFIG {ConfigurationFieldState} · MEAS {MeasurementFieldState}";
    }
}
