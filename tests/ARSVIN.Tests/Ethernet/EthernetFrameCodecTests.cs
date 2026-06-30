using Xunit;
using AR.Iec61850.Ethernet;

namespace ARSVIN.Tests.Ethernet;

public sealed class EthernetFrameCodecTests
{
    [Fact]
    public void EncodeDecodeRoundTripWithoutVlan()
    {
        var payload = new byte[] { 0x60, 0x81, 0x02, 0x01 };
        var frame = new EthernetFrame(
            MacAddress.Parse("01:0C:CD:04:00:01"),
            MacAddress.Parse("02:00:00:00:20:01"),
            EthernetConstants.SampledValuesEtherType,
            null,
            payload);

        var encoded = EthernetFrameCodec.Encode(frame);

        Assert.Equal(14 + payload.Length, encoded.Length);
        Assert.True(EthernetFrameCodec.TryDecode(encoded, out var decoded));
        Assert.Equal(frame.Destination, decoded.Destination);
        Assert.Equal(frame.Source, decoded.Source);
        Assert.Equal(frame.EtherType, decoded.EtherType);
        Assert.Null(decoded.Vlan);
        Assert.Equal(payload, decoded.Payload.ToArray());
    }

    [Fact]
    public void EncodeDecodeRoundTripWithVlan()
    {
        var payload = new byte[] { 0x60, 0x81, 0x02, 0x01 };
        var vlan = new VlanTag(priorityCodePoint: 4, vlanId: 100);
        var frame = new EthernetFrame(
            MacAddress.Parse("01:0C:CD:04:00:02"),
            MacAddress.Parse("02:00:00:00:20:02"),
            EthernetConstants.SampledValuesEtherType,
            vlan,
            payload);

        var encoded = EthernetFrameCodec.Encode(frame);

        Assert.Equal(18 + payload.Length, encoded.Length);
        Assert.True(EthernetFrameCodec.TryDecode(encoded, out var decoded));
        Assert.Equal(frame.Destination, decoded.Destination);
        Assert.Equal(frame.Source, decoded.Source);
        Assert.Equal(frame.EtherType, decoded.EtherType);
        Assert.Equal(vlan, decoded.Vlan);
        Assert.Equal(payload, decoded.Payload.ToArray());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public void TryDecodeRejectsTooShortFrames(int length)
    {
        var bytes = new byte[length];

        Assert.False(EthernetFrameCodec.TryDecode(bytes, out _));
    }

    [Fact]
    public void TryDecodeRejectsTruncatedVlanFrame()
    {
        var bytes = new byte[17];
        bytes[12] = 0x81;
        bytes[13] = 0x00;

        Assert.False(EthernetFrameCodec.TryDecode(bytes, out _));
    }
}
