namespace ARSVIN.Subscriber.Models;

internal sealed class PcapFrame
{
    public DateTimeOffset Timestamp { get; init; }
    public ReadOnlyMemory<byte> Frame { get; init; }
}

internal static class PcapFrames
{
    public static IEnumerable<PcapFrame> Read(string path)
    {
        using var stream = File.OpenRead(path);
        var header = new byte[24];
        if (stream.Read(header) != header.Length)
            throw new InvalidDataException("PCAP global header is incomplete.");

        var magic = ReadUInt32Little(header, 0);
        var littleEndian = magic switch
        {
            0xA1B2C3D4 => true,
            0xD4C3B2A1 => false,
            0xA1B23C4D => true,
            0x4D3CB2A1 => false,
            _ => throw new InvalidDataException("Only classic PCAP files are supported. PCAPNG is not supported yet.")
        };
        var nano = magic is 0xA1B23C4D or 0x4D3CB2A1;

        var packetHeader = new byte[16];
        while (stream.Read(packetHeader) == packetHeader.Length)
        {
            var seconds = ReadUInt32(packetHeader, 0, littleEndian);
            var fraction = ReadUInt32(packetHeader, 4, littleEndian);
            var includedLength = ReadUInt32(packetHeader, 8, littleEndian);
            _ = ReadUInt32(packetHeader, 12, littleEndian);
            if (includedLength == 0 || includedLength > 262_144)
                throw new InvalidDataException($"Invalid PCAP packet length: {includedLength}.");

            var data = new byte[includedLength];
            var read = stream.Read(data);
            if (read != data.Length)
                throw new InvalidDataException("PCAP packet data is truncated.");

            var ticks = nano ? fraction / 100 : fraction * 10;
            var timestamp = DateTimeOffset.FromUnixTimeSeconds(seconds).AddTicks(ticks);
            yield return new PcapFrame { Timestamp = timestamp, Frame = data };
        }
    }

    private static uint ReadUInt32(byte[] source, int offset, bool littleEndian)
        => littleEndian ? ReadUInt32Little(source, offset) : ReadUInt32Big(source, offset);

    private static uint ReadUInt32Little(byte[] source, int offset)
        => (uint)(source[offset] | (source[offset + 1] << 8) | (source[offset + 2] << 16) | (source[offset + 3] << 24));

    private static uint ReadUInt32Big(byte[] source, int offset)
        => (uint)((source[offset] << 24) | (source[offset + 1] << 16) | (source[offset + 2] << 8) | source[offset + 3]);
}
