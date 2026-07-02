namespace ARSVIN.Subscriber.Models;

public sealed class PhasorVector
{
    public string Channel { get; init; } = string.Empty;
    public string Kind { get; init; } = string.Empty;
    public double Rms { get; init; }
    public double Peak { get; init; }
    public double AngleDegrees { get; init; }
    public string RmsText => $"{Rms:0.###}";
    public string PeakText => $"{Peak:0.###}";
    public string AngleText => $"{AngleDegrees:0.0}°";
}
