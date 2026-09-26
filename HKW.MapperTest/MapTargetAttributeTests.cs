using HKW.HKWMapper;

namespace HKW.MapperTest;

[TestClass]
public sealed class MapTargetAttributeTests
{
    [TestMethod]
    public void MapsBothDirectionsAndUsesExistingTarget()
    {
        var source = new BasicSource { Id = 7, Name = 2 };
        var target = new BasicTarget { Id = 1, Name = 1 };

        source.MapToBasicTarget(target);

        Assert.AreEqual(7, target.Id);
        Assert.AreEqual(2, target.Name);

        target.Id = 12;
        target.Name = 3;
        source.MapFromBasicTarget(target);
        Assert.AreEqual(12, source.Id);
        Assert.AreEqual(3, source.Name);
    }

    [TestMethod]
    public void UsesExplicitTargetName()
    {
        var source = new RenamedSource { Number = 4 };
        var target = new RenamedTarget();

        source.MapToRenamedTarget(target);
        Assert.AreEqual(4, target.Number);

        target.Number = 9;
        source.MapFromRenamedTarget(target);
        Assert.AreEqual(9, source.Number);
    }
}

[MapTarget(typeof(BasicTarget))]
public sealed class BasicSource
{
    public int Id { get; set; }
    public int Name { get; set; }
}

public sealed class BasicTarget
{
    public int Id { get; set; }
    public int Name { get; set; }
}

[MapTarget(typeof(RenamedTarget), TargetName = "RenamedTarget")]
public sealed class RenamedSource
{
    public int Number { get; set; }
}

public sealed class RenamedTarget
{
    public int Number { get; set; }
}
