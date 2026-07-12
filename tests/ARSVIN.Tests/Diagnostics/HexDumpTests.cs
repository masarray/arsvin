using AR.Iec61850.Diagnostics;
using Xunit;

namespace ARSVIN.Tests.Diagnostics;

public sealed class HexDumpTests
{
    [Fact]
    public void ParseAcceptsMixedWhitespaceAndHexCase()
    {
        var bytes = HexDump.Parse("01 0a\nFF\t7c");

        Assert.Equal(new byte[] { 0x01, 0x0A, 0xFF, 0x7C }, bytes);
    }

    [Fact]
    public void ParseReturnsEmptyForWhitespace()
    {
        Assert.Empty(HexDump.Parse("  \r\n\t "));
    }

    [Fact]
    public void ToCompactStringTruncatesAndReportsRemainingBytes()
    {
        var text = HexDump.ToCompactString(new byte[] { 0x01, 0x02, 0x03, 0x04 }, maxBytes: 2);

        Assert.Equal("01 02 ... (+2 byte)", text);
    }

    [Fact]
    public void ContainsFindsOverlappingPatternAndTreatsEmptyPatternAsMatch()
    {
        var data = new byte[] { 0x10, 0x20, 0x20, 0x30 };

        Assert.True(HexDump.Contains(data, new byte[] { 0x20, 0x20 }));
        Assert.True(HexDump.Contains(data, ReadOnlySpan<byte>.Empty));
        Assert.False(HexDump.Contains(data, new byte[] { 0x30, 0x40 }));
    }
}
