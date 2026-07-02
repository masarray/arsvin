namespace ARSVIN.Subscriber.Models;

public sealed class DecodedValueRow
{
    public int Index { get; init; }
    public string Signal { get; init; } = string.Empty;
    public string Kind { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
    public string Raw { get; init; } = string.Empty;
    public double? NumericValue { get; init; }
}

