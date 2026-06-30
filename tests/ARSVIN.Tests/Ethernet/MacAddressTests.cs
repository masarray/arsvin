using Xunit;
using AR.Iec61850.Ethernet;

namespace ARSVIN.Tests.Ethernet;

public sealed class MacAddressTests
{
    [Fact]
    public void ParseAcceptsColonSeparatedHex()
    {
        var address = MacAddress.Parse("01:0C:CD:04:00:01");

        Assert.Equal("01:0C:CD:04:00:01", address.ToString());
    }

    [Fact]
    public void ParseAcceptsDashSeparatedHex()
    {
        var address = MacAddress.Parse("01-0c-cd-04-00-01");

        Assert.Equal("01:0C:CD:04:00:01", address.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("01:0C:CD:04:00")]
    [InlineData("01:0C:CD:04:00:GG")]
    [InlineData("01:0C:CD:04:00:01:02")]
    public void TryParseRejectsInvalidInput(string text)
    {
        Assert.False(MacAddress.TryParse(text, out _));
    }
}
