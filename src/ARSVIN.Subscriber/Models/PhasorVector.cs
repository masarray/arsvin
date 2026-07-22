namespace ARSVIN.Subscriber.Models;

public sealed class PhasorVector
{
    public string Channel { get; init; } = string.Empty;
    public string Kind { get; init; } = string.Empty;
    public double Rms { get; init; }
    public double Peak { get; init; }
    public double AngleDegrees { get; init; }
    public bool IsValid { get; init; } = true;
    public string InvalidReason { get; init; } = string.Empty;
    public string Unit => Kind.Equals("Voltage", StringComparison.OrdinalIgnoreCase) ? "V" :
        Kind.Equals("Current", StringComparison.OrdinalIgnoreCase) ? "A" : string.Empty;
    public string RmsText => IsValid ? $"{Rms:0.###} {Unit}".TrimEnd() : "invalid";
    public string PeakText => IsValid ? $"{Peak:0.###} {Unit}".TrimEnd() : "invalid";
    public string AngleText => IsValid ? $"{AngleDegrees:0.0}°" : "—";
}
