using AR.Iec61850.Capture;

namespace ARSVIN.Subscriber.Models;

internal sealed class PcapFrame
{
    public DateTimeOffset Timestamp { get; init; }
    public ReadOnlyMemory<byte> Frame { get; init; }
}

/// <summary>
/// Compatibility adapter. File parsing is owned by ARIEC61850 so classic PCAP, PCAPNG,
/// and future offline capture formats feed the same Subscriber protocol pipeline.
/// </summary>
internal static class PcapFrames
{
    public static IEnumerable<PcapFrame> Read(string path)
        => ProcessBusCaptureFileReader.Read(path)
            .Select(packet => new PcapFrame
            {
                Timestamp = packet.Timestamp,
                Frame = packet.Frame
            });
}
