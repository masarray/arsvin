namespace ARSVIN.Subscriber.Models;

public sealed class WaveformPoint
{
    public int Index { get; init; }
    public ushort? SampleCount { get; init; }
    public double? Ia { get; set; }
    public double? Ib { get; set; }
    public double? Ic { get; set; }
    public double? In { get; set; }
    public double? Va { get; set; }
    public double? Vb { get; set; }
    public double? Vc { get; set; }
    public double? Vn { get; set; }
}
