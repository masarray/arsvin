using AR.Iec61850.SampledValues.Analysis;
using Xunit;

namespace ARSVIN.Tests.SampledValues.Analysis;

public sealed class SvGenericPayloadInspectorTests
{
    [Fact]
    public void InspectExposesEveryWordWithoutInventingChannelSemantics()
    {
        var payload = new byte[64];
        payload[2] = 0x03;
        payload[3] = 0xE8;
        payload[8] = 0xFF;
        payload[9] = 0xFF;
        payload[10] = 0xFC;
        payload[11] = 0x18;

        var inspection = SvGenericPayloadInspector.Inspect(payload);

        Assert.Equal(64, inspection.PayloadLength);
        Assert.Equal(16, inspection.CompleteWordCount);
        Assert.True(inspection.IsFourByteAligned);
        Assert.True(inspection.HasEightByteGroupShape);
        Assert.Equal(1000, inspection.Words[0].SignedInt32);
        Assert.Equal(-1000, inspection.Words[2].SignedInt32);
        Assert.All(inspection.Words, word => Assert.StartsWith("Word ", word.GenericLabel));
        Assert.DoesNotContain(inspection.Words, word =>
            word.GenericLabel.Contains("Ia", StringComparison.OrdinalIgnoreCase) ||
            word.GenericLabel.Contains("Voltage", StringComparison.OrdinalIgnoreCase) ||
            word.GenericLabel.Contains("Quality", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void InspectUsesBigEndianSignedAndUnsignedViewsOfTheSameBytes()
    {
        var inspection = SvGenericPayloadInspector.Inspect(
            new byte[] { 0xFF, 0xFF, 0xFF, 0xFE });

        var word = Assert.Single(inspection.Words);
        Assert.Equal(-2, word.SignedInt32);
        Assert.Equal(4_294_967_294u, word.UnsignedInt32);
        Assert.Equal("FFFFFFFE", word.Hex);
        Assert.Equal("+0x00", word.OffsetLabel);
        Assert.Equal(SvGenericPayloadWordRole.StandaloneWord, word.StructuralRole);
    }

    [Fact]
    public void InspectMarksEightByteGroupingAsStructuralOnly()
    {
        var inspection = SvGenericPayloadInspector.Inspect(new byte[16]);

        Assert.Equal(SvGenericPayloadWordRole.FirstWordInEightByteGroup, inspection.Words[0].StructuralRole);
        Assert.Equal(SvGenericPayloadWordRole.SecondWordInEightByteGroup, inspection.Words[1].StructuralRole);
        Assert.Contains(inspection.Diagnostics, diagnostic =>
            diagnostic.Contains("structural evidence only", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void InspectPreservesTrailingBytesInsteadOfDroppingPayload()
    {
        var inspection = SvGenericPayloadInspector.Inspect(
            new byte[] { 0x00, 0x00, 0x00, 0x01, 0xAA, 0xBB });

        Assert.False(inspection.IsFourByteAligned);
        Assert.Equal(1, inspection.CompleteWordCount);
        Assert.Equal(2, inspection.TrailingByteCount);
        Assert.Equal(new byte[] { 0xAA, 0xBB }, inspection.TrailingBytes);
        Assert.Contains("2 trailing byte(s)", inspection.Summary);
    }

    [Fact]
    public void InspectHandlesEmptyPayloadExplicitly()
    {
        var inspection = SvGenericPayloadInspector.Inspect(ReadOnlyMemory<byte>.Empty);

        Assert.Empty(inspection.Words);
        Assert.Equal("Empty seqOfData payload", inspection.Summary);
        Assert.Contains("empty", Assert.Single(inspection.Diagnostics), StringComparison.OrdinalIgnoreCase);
    }
}
