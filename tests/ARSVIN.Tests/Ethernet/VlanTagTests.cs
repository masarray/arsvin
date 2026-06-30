using Xunit;
using AR.Iec61850.Ethernet;

namespace ARSVIN.Tests.Ethernet;

public sealed class VlanTagTests
{
    [Fact]
    public void ToTagControlInformationEncodesPriorityDropEligibleAndVlanId()
    {
        var tag = new VlanTag(PriorityCodePoint: 4, DropEligible: true, VlanId: 100);

        var tci = tag.ToTagControlInformation();

        Assert.Equal(0x9064, tci);
        Assert.Equal(tag, VlanTag.FromTagControlInformation(tci));
    }

    [Fact]
    public void ToTagControlInformationRejectsInvalidPriority()
    {
        var tag = new VlanTag(priorityCodePoint: 8, vlanId: 100);

        Assert.Throws<ArgumentOutOfRangeException>(() => tag.ToTagControlInformation());
    }

    [Fact]
    public void ToTagControlInformationRejectsInvalidVlanId()
    {
        var tag = new VlanTag(priorityCodePoint: 4, vlanId: 4095);

        Assert.Throws<ArgumentOutOfRangeException>(() => tag.ToTagControlInformation());
    }
}
