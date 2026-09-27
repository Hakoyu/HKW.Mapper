using HKW.HKWMapper;

namespace HKW.MapperTest;

[TestClass]
public sealed class MapIgnorePropertyAttributeTests
{
    [TestMethod]
    public void DoesNotMapPropertyInEitherDirection()
    {
        var source = new IgnoreSource { Mapped = 7, Ignored = 8 };
        var target = new IgnoreTarget { Mapped = 1, Ignored = 2 };

        source.MapToIgnoreTarget(target);

        Assert.AreEqual(7, target.Mapped);
        Assert.AreEqual(2, target.Ignored);

        target.Mapped = 9;
        target.Ignored = 10;
        source.MapFromIgnoreTarget(target);

        Assert.AreEqual(9, source.Mapped);
        Assert.AreEqual(8, source.Ignored);
    }
}

[MapTarget(typeof(IgnoreTarget))]
public sealed class IgnoreSource
{
    public int Mapped { get; set; }

    [MapIgnoreProperty]
    public int Ignored { get; set; }
}

public sealed class IgnoreTarget
{
    public int Mapped { get; set; }
    public int Ignored { get; set; }
}
