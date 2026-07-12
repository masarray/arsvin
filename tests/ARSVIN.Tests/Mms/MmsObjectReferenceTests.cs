using AR.Iec61850.Mms;
using Xunit;

namespace ARSVIN.Tests.Mms;

public sealed class MmsObjectReferenceTests
{
    [Fact]
    public void FromIec61850ReferenceInsertsFunctionalConstraint()
    {
        var reference = MmsObjectReference.FromIec61850Reference("LD0/MMXU1.TotW.mag.f", "MX");

        Assert.Equal("LD0", reference.Domain);
        Assert.Equal("MMXU1$MX$TotW$mag$f", reference.Item);
        Assert.Equal("MX", reference.FunctionalConstraint);
        Assert.Equal("LD0/MMXU1.MX.TotW.mag.f [MX]", reference.ToString());
    }

    [Fact]
    public void ExistingFunctionalConstraintIsNotDuplicated()
    {
        var reference = MmsObjectReference.FromIec61850Reference("LD0/MMXU1$MX$TotW$mag$f", "MX");

        Assert.Equal("MMXU1$MX$TotW$mag$f", reference.Item);
    }

    [Fact]
    public void WithoutFunctionalConstraintRemovesOnlyConstraintSegment()
    {
        var reference = new MmsObjectReference("LD0", "MMXU1$MX$TotW$mag$f", "MX");

        var withoutFc = reference.WithoutFunctionalConstraint();

        Assert.Equal("LD0", withoutFc.Domain);
        Assert.Equal("MMXU1$TotW$mag$f", withoutFc.Item);
        Assert.Equal(string.Empty, withoutFc.FunctionalConstraint);
    }

    [Fact]
    public void ReferenceWithoutDomainRemainsUsable()
    {
        var reference = MmsObjectReference.Parse("MMXU1.TotW.mag.f", "MX");

        Assert.Equal(string.Empty, reference.Domain);
        Assert.Equal("MMXU1$TotW$mag$f", reference.Item);
        Assert.Equal("MMXU1$TotW$mag$f", reference.ToString());
    }
}
