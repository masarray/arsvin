using AR.Iec61850.Mms;
using Xunit;

namespace ARSVIN.Tests.Mms;

public sealed class MmsDataCodecTests
{
    [Fact]
    public void StructureRoundTripPreservesCommonMmsDataKinds()
    {
        var source = MmsDataValue.Structure(new MmsDataValue[]
        {
            MmsDataValue.Boolean(true),
            MmsDataValue.Integer(-42),
            MmsDataValue.Unsigned(42),
            MmsDataValue.FloatingPoint(12.5f),
            MmsDataValue.VisibleString("MU01"),
            MmsDataValue.OctetString(new byte[] { 0xAA, 0x55 }),
            MmsDataValue.BitString(3, new byte[] { 0xA0 })
        });

        var decoded = Assert.Single(MmsDataCodec.DecodeAllData(MmsDataCodec.Encode(source)));

        Assert.Equal(MmsDataKind.Structure, decoded.Kind);
        Assert.Equal(7, decoded.Children.Count);
        Assert.True((bool)decoded.Children[0].Value!);
        Assert.Equal(-42L, decoded.Children[1].Value);
        Assert.Equal(42UL, decoded.Children[2].Value);
        Assert.Equal(12.5f, Assert.IsType<float>(decoded.Children[3].Value));
        Assert.Equal("MU01", decoded.Children[4].Value);
        Assert.Equal(new byte[] { 0xAA, 0x55 }, decoded.Children[5].RawValue);
        Assert.Equal(new byte[] { 0x03, 0xA0 }, decoded.Children[6].RawValue);
    }

    [Fact]
    public void UnknownContextSpecificTagRoundTripsWithoutDataLoss()
    {
        var source = MmsDataValue.Unknown(22, new byte[] { 0xDE, 0xAD, 0xBE, 0xEF });

        var decoded = Assert.Single(MmsDataCodec.DecodeAllData(MmsDataCodec.Encode(source)));

        Assert.Equal(MmsDataKind.Unknown, decoded.Kind);
        Assert.Equal(22, decoded.UnknownTagNumber);
        Assert.Equal(source.RawValue, decoded.RawValue);
    }

    [Fact]
    public void DecodeAllDataReturnsEmptyForEmptyInput()
    {
        Assert.Empty(MmsDataCodec.DecodeAllData(ReadOnlyMemory<byte>.Empty));
    }

    [Fact]
    public void DisplayStringUsesInvariantEngineeringText()
    {
        Assert.Equal("true", MmsDataCodec.ToDisplayString(MmsDataValue.Boolean(true)));
        Assert.Equal("-17", MmsDataCodec.ToDisplayString(MmsDataValue.Integer(-17)));
        Assert.Equal("12.5", MmsDataCodec.ToDisplayString(MmsDataValue.FloatingPoint(12.5f)));
        Assert.Equal("SV01", MmsDataCodec.ToDisplayString(MmsDataValue.VisibleString("SV01")));
    }
}
