namespace ARSVIN.Subscriber.Models;

public sealed class AdapterChoice
{
    public int Index { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string MacAddress { get; init; } = string.Empty;
    public string DisplayName => $"{Index}. {Description}";
    public string Selector => Index.ToString();
}
