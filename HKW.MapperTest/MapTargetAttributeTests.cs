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

        source.MapTo(target);

        Assert.AreEqual(7, target.Id);
        Assert.AreEqual(2, target.Name);

        target.Id = 12;
        target.Name = 3;
        source.MapFrom(target);
        Assert.AreEqual(12, source.Id);
        Assert.AreEqual(3, source.Name);
    }

    [TestMethod]
    public void UsesExplicitTargetName()
    {
        var source = new RenamedSource { Number = 4 };
        var target = new RenamedTarget();

        source.MapToRenamed(target);
        Assert.AreEqual(4, target.Number);

        target.Number = 9;
        source.MapFromRenamed(target);
        Assert.AreEqual(9, source.Number);
    }

    [TestMethod]
    public void MapsPropertyInheritedByTarget()
    {
        var source = new FoodSource { Name = "Apple" };
        var target = new Food();

        source.MapTo(target);
        Assert.AreEqual("Apple", target.Name);

        target.Name = "Bread";
        source.MapFrom(target);
        Assert.AreEqual("Bread", source.Name);
    }

    [TestMethod]
    public void MapsPropertyInherited()
    {
        var source = new Food { Name = "Apple" };
        var target = new Food();

        source.MapTo(target);
        Assert.AreEqual("Apple", target.Name);

        target.Name = "Bread";
        source.MapFrom(target);
        Assert.AreEqual("Bread", source.Name);
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

[MapTarget(typeof(RenamedTarget), TargetName = "Renamed")]
public sealed class RenamedSource
{
    public int Number { get; set; }
}

public sealed class RenamedTarget
{
    public int Number { get; set; }
}

[MapTarget(typeof(Food))]
public sealed class FoodSource
{
    public string Name { get; set; } = string.Empty;
}

public class FoodBase
{
    public string Name { get; set; } = string.Empty;
}

[MapTarget(typeof(Food))]
public sealed class Food : FoodBase;
