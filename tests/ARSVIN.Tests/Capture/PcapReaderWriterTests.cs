using System.Buffers.Binary;
using AR.Iec61850.Capture;
using Xunit;

namespace ARSVIN.Tests.Capture;

public sealed class PcapReaderWriterTests
{
    [Fact]
    public void RoundTripPreservesTimestampAndEthernetFrame()
    {
        var timestamp = DateTimeOffset.UnixEpoch.AddSeconds(123).AddTicks(12_340);
        var frame = new byte[] { 0x01, 0x0C, 0xCD, 0x04, 0x00, 0x01, 0x88, 0xBA };
        using var stream = new MemoryStream();

        using (var writer = new PcapWriter(stream, leaveOpen: true))
            writer.WritePacket(timestamp, frame);

        stream.Position = 0;
        var packets = PcapReader.ReadAll(stream);

        var packet = Assert.Single(packets);
        Assert.Equal(timestamp, packet.Timestamp);
        Assert.Equal(frame, packet.Frame);
    }

    [Fact]
    public void ReaderRejectsUnsupportedLinkType()
    {
        using var stream = new MemoryStream();
        using (var writer = new PcapWriter(stream, leaveOpen: true))
        {
        }

        var bytes = stream.ToArray();
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(20, 4), 101);

        var ex = Assert.Throws<PcapFormatException>(() => PcapReader.ReadAll(new MemoryStream(bytes)));

        Assert.Contains("link type", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ReaderRejectsTruncatedPacketPayload()
    {
        using var stream = new MemoryStream();
        using (var writer = new PcapWriter(stream, leaveOpen: true))
            writer.WritePacket(DateTimeOffset.UnixEpoch, new byte[] { 1, 2, 3, 4 });

        var truncated = stream.ToArray()[..^1];

        var ex = Assert.Throws<PcapFormatException>(() => PcapReader.ReadAll(new MemoryStream(truncated)));

        Assert.Contains("payload", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void WriterRejectsWritesAfterDispose()
    {
        var stream = new MemoryStream();
        var writer = new PcapWriter(stream, leaveOpen: true);
        writer.Dispose();

        Assert.Throws<ObjectDisposedException>(() => writer.WritePacket(DateTimeOffset.UnixEpoch, new byte[] { 1 }));
    }
}
