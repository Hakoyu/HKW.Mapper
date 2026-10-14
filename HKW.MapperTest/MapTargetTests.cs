using HKW.Mapper;

namespace HKW.MapperTest;

[TestClass]
public sealed class MapTargetTests
{
    [TestMethod]
    public void TargetTypeDefaultsToBothDirectionsAndUsesProvidedInstances()
    {
        var source = new MapTargetFixtureDefaultSource { Id = 7, Name = "source" };
        var target = new MapTargetFixtureDefaultTarget { Id = 1, Name = "target" };

        var returnedTarget = source.MapTo(target);

        Assert.AreSame(target, returnedTarget);
        Assert.AreEqual(7, target.Id);
        Assert.AreEqual("source", target.Name);

        target.Id = 12;
        target.Name = "updated";
        var returnedSource = source.MapFrom(target);

        Assert.AreSame(source, returnedSource);
        Assert.AreEqual(12, source.Id);
        Assert.AreEqual("updated", source.Name);
    }

    [TestMethod]
    public void TargetNameSelectsTheGeneratedMethodNames()
    {
        var source = new MapTargetFixtureNamedSource { Value = 4 };
        var target = new MapTargetFixtureNamedTarget();

        var returnedTarget = source.MapToAlias(target);

        Assert.AreSame(target, returnedTarget);
        Assert.AreEqual(4, target.Value);

        target.Value = 9;
        var returnedSource = source.MapFromAlias(target);

        Assert.AreSame(source, returnedSource);
        Assert.AreEqual(9, source.Value);
    }

    [TestMethod]
    public void DirectionToGeneratesForwardMapping()
    {
        var source = new MapTargetFixtureToSource { Value = 3 };
        var target = new MapTargetFixtureToTarget();

        var returnedTarget = source.MapTo(target);

        Assert.AreSame(target, returnedTarget);
        Assert.AreEqual(3, target.Value);
    }

    [TestMethod]
    public void DirectionFromGeneratesReverseMapping()
    {
        var source = new MapTargetFixtureFromSource { Value = 1 };
        var target = new MapTargetFixtureFromTarget { Value = 8 };

        var returnedSource = source.MapFrom(target);

        Assert.AreSame(source, returnedSource);
        Assert.AreEqual(8, source.Value);
    }

    [TestMethod]
    public void DirectionBothGeneratesForwardAndReverseMapping()
    {
        var source = new MapTargetFixtureBothSource { Value = 5 };
        var target = new MapTargetFixtureBothTarget();

        source.MapTo(target);
        Assert.AreEqual(5, target.Value);

        target.Value = 6;
        source.MapFrom(target);
        Assert.AreEqual(6, source.Value);
    }

    [TestMethod]
    public void MapParentPropertiesIncludesTheSourceInheritanceChain()
    {
        var source = new MapTargetFixtureParentSource
        {
            Own = 1,
            Parent = 2,
            Grandparent = 3,
        };
        var target = new MapTargetFixtureParentTarget();

        source.MapTo(target);

        Assert.AreEqual(1, target.Own);
        Assert.AreEqual(2, target.Parent);
        Assert.AreEqual(3, target.Grandparent);

        target.Own = 4;
        target.Parent = 5;
        target.Grandparent = 6;
        source.MapFrom(target);

        Assert.AreEqual(4, source.Own);
        Assert.AreEqual(5, source.Parent);
        Assert.AreEqual(6, source.Grandparent);
    }

    [TestMethod]
    public void DerivedPropertyTakesPrecedenceOverHiddenBaseProperty()
    {
        var source = new MapTargetFixtureHiddenDerivedSource { Value = 7 };
        ((MapTargetFixtureHiddenBaseSource)source).Value = 2;
        var target = new MapTargetFixtureHiddenTarget();

        source.MapTo(target);

        Assert.AreEqual(7, target.Value);

        target.Value = 9;
        source.MapFrom(target);

        Assert.AreEqual(9, source.Value);
        Assert.AreEqual(2, ((MapTargetFixtureHiddenBaseSource)source).Value);
    }

    [TestMethod]
    public void ParentPropertiesAreExcludedByDefault()
    {
        var source = new MapTargetFixtureNoParentSource { Own = 1, Parent = 2 };
        var target = new MapTargetFixtureNoParentTarget { Own = 10, Parent = 20 };

        source.MapTo(target);

        Assert.AreEqual(1, target.Own);
        Assert.AreEqual(20, target.Parent);

        target.Own = 30;
        target.Parent = 40;
        source.MapFrom(target);

        Assert.AreEqual(30, source.Own);
        Assert.AreEqual(2, source.Parent);
    }

    [TestMethod]
    public void IgnorePropertiesExcludesPropertiesInBothDirections()
    {
        var source = new MapTargetFixtureIgnoredSource { Included = 1, Ignored = 2 };
        var target = new MapTargetFixtureIgnoredTarget { Included = 10, Ignored = 20 };

        source.MapTo(target);

        Assert.AreEqual(1, target.Included);
        Assert.AreEqual(20, target.Ignored);

        target.Included = 30;
        target.Ignored = 40;
        source.MapFrom(target);

        Assert.AreEqual(30, source.Included);
        Assert.AreEqual(2, source.Ignored);
    }

    [TestMethod]
    public void MapsPropertiesInheritedByTheTarget()
    {
        var source = new MapTargetFixtureInheritedTargetSource { Name = "Apple" };
        var target = new MapTargetFixtureInheritedTarget();

        source.MapTo(target);
        Assert.AreEqual("Apple", target.Name);

        target.Name = "Bread";
        source.MapFrom(target);
        Assert.AreEqual("Bread", source.Name);
    }

    [TestMethod]
    public void MapsCollectionsInBothDirectionsAndReturnsProvidedInstances()
    {
        var source = new MapTargetFixtureCollectionSource
        {
            Array = [1, 2],
            List = [3, 4],
            Set = [5, 6],
            Dictionary = new Dictionary<string, int> { ["one"] = 1 },
            Items = [new MapTargetFixtureCollectionItemSource { Value = 7 }],
        };
        var target = new MapTargetFixtureCollectionTarget
        {
            Array = [99],
            List = [99],
            Set = [99],
            Dictionary = new Dictionary<string, int> { ["old"] = 99 },
            Items = [new MapTargetFixtureCollectionItemTarget { Value = 99 }],
        };

        var returnedTarget = source.MapTo(target);

        Assert.AreSame(target, returnedTarget);
        CollectionAssert.AreEqual(new[] { 1, 2 }, target.Array);
        CollectionAssert.AreEqual(new[] { 3, 4 }, target.List);
        CollectionAssert.AreEquivalent(new[] { 5, 6 }, target.Set.ToArray());
        Assert.AreEqual(1, target.Dictionary.Count);
        Assert.AreEqual(1, target.Dictionary["one"]);
        Assert.AreEqual(7, target.Items.Single().Value);
        Assert.AreNotSame(source.Array, target.Array);
        Assert.AreNotSame((object)source.Items.Single(), target.Items.Single());

        target.Array = [8, 9];
        target.List = [10];
        target.Set = [11];
        target.Dictionary = new Dictionary<string, int> { ["two"] = 2 };
        target.Items = [new MapTargetFixtureCollectionItemTarget { Value = 12 }];
        var returnedSource = source.MapFrom(target);

        Assert.AreSame(source, returnedSource);
        CollectionAssert.AreEqual(new[] { 8, 9 }, source.Array);
        CollectionAssert.AreEqual(new[] { 10 }, source.List);
        CollectionAssert.AreEquivalent(new[] { 11 }, source.Set.ToArray());
        Assert.AreEqual(2, source.Dictionary["two"]);
        Assert.AreEqual(12, source.Items.Single().Value);
        Assert.AreNotSame((object)source.Items.Single(), target.Items.Single());
    }

    [TestMethod]
    public void MapsNullableValuesAndPreservesNullReferences()
    {
        var source = new MapTargetFixtureNullableSource { Number = null, Text = null };
        var target = new MapTargetFixtureNullableTarget { Number = 12, Text = "old" };

        var returnedTarget = source.MapTo(target);

        Assert.AreSame(target, returnedTarget);
        Assert.AreEqual(0, target.Number);
        Assert.IsNull(target.Text);

        target.Number = 7;
        target.Text = "new";
        var returnedSource = source.MapFrom(target);

        Assert.AreSame(source, returnedSource);
        Assert.AreEqual(7, source.Number);
        Assert.AreEqual("new", source.Text);
    }
}

[MapTarget(typeof(MapTargetFixtureDefaultTarget))]
public sealed class MapTargetFixtureDefaultSource
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public sealed class MapTargetFixtureDefaultTarget
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

[MapTarget(typeof(MapTargetFixtureNamedTarget), TargetName = "Alias")]
public sealed class MapTargetFixtureNamedSource
{
    public int Value { get; set; }
}

public sealed class MapTargetFixtureNamedTarget
{
    public int Value { get; set; }
}

[MapTarget(typeof(MapTargetFixtureToTarget), Direction = MapDirections.To)]
public sealed class MapTargetFixtureToSource
{
    public int Value { get; set; }
}

public sealed class MapTargetFixtureToTarget
{
    public int Value { get; set; }
}

[MapTarget(typeof(MapTargetFixtureFromTarget), Direction = MapDirections.From)]
public sealed class MapTargetFixtureFromSource
{
    public int Value { get; set; }
}

public sealed class MapTargetFixtureFromTarget
{
    public int Value { get; set; }
}

[MapTarget(typeof(MapTargetFixtureBothTarget), Direction = MapDirections.Both)]
public sealed class MapTargetFixtureBothSource
{
    public int Value { get; set; }
}

public sealed class MapTargetFixtureBothTarget
{
    public int Value { get; set; }
}

public class MapTargetFixtureGrandparentSource
{
    public int Grandparent { get; set; }
}

public class MapTargetFixtureParentBaseSource : MapTargetFixtureGrandparentSource
{
    public int Parent { get; set; }
}

[MapTarget(typeof(MapTargetFixtureParentTarget), MapParentProperties = true)]
public sealed class MapTargetFixtureParentSource : MapTargetFixtureParentBaseSource
{
    public int Own { get; set; }
}

public sealed class MapTargetFixtureParentTarget
{
    public int Own { get; set; }
    public int Parent { get; set; }
    public int Grandparent { get; set; }
}

public class MapTargetFixtureHiddenBaseSource
{
    public int Value { get; set; }
}

[MapTarget(typeof(MapTargetFixtureHiddenTarget), MapParentProperties = true)]
public sealed class MapTargetFixtureHiddenDerivedSource : MapTargetFixtureHiddenBaseSource
{
    public new int Value { get; set; }
}

public sealed class MapTargetFixtureHiddenTarget
{
    public int Value { get; set; }
}

public class MapTargetFixtureNoParentBaseSource
{
    public int Parent { get; set; }
}

[MapTarget(typeof(MapTargetFixtureNoParentTarget))]
public sealed class MapTargetFixtureNoParentSource : MapTargetFixtureNoParentBaseSource
{
    public int Own { get; set; }
}

public sealed class MapTargetFixtureNoParentTarget
{
    public int Own { get; set; }
    public int Parent { get; set; }
}

[MapTarget(
    typeof(MapTargetFixtureIgnoredTarget),
    IgnoreProperties = new[] { nameof(MapTargetFixtureIgnoredSource.Ignored) }
)]
public sealed class MapTargetFixtureIgnoredSource
{
    public int Included { get; set; }
    public int Ignored { get; set; }
}

public sealed class MapTargetFixtureIgnoredTarget
{
    public int Included { get; set; }
    public int Ignored { get; set; }
}

[MapTarget(typeof(MapTargetFixtureInheritedTarget))]
public sealed class MapTargetFixtureInheritedTargetSource
{
    public string Name { get; set; } = string.Empty;
}

public class MapTargetFixtureInheritedTargetBase
{
    public string Name { get; set; } = string.Empty;
}

public sealed class MapTargetFixtureInheritedTarget : MapTargetFixtureInheritedTargetBase;

[MapTarget(typeof(MapTargetFixtureCollectionTarget))]
public sealed class MapTargetFixtureCollectionSource
{
    public int[] Array { get; set; } = [];
    public List<int> List { get; set; } = [];
    public HashSet<int> Set { get; set; } = [];
    public Dictionary<string, int> Dictionary { get; set; } = [];
    public List<MapTargetFixtureCollectionItemSource> Items { get; set; } = [];
}

public sealed class MapTargetFixtureCollectionTarget
{
    public int[] Array { get; set; } = [];
    public List<int> List { get; set; } = [];
    public HashSet<int> Set { get; set; } = [];
    public Dictionary<string, int> Dictionary { get; set; } = [];
    public List<MapTargetFixtureCollectionItemTarget> Items { get; set; } = [];
}

[MapTarget(typeof(MapTargetFixtureCollectionItemTarget))]
public sealed class MapTargetFixtureCollectionItemSource
{
    public int Value { get; set; }
}

public sealed class MapTargetFixtureCollectionItemTarget
{
    public int Value { get; set; }
}

[MapTarget(typeof(MapTargetFixtureNullableTarget))]
public sealed class MapTargetFixtureNullableSource
{
    public int? Number { get; set; }
    public string? Text { get; set; }
}

public sealed class MapTargetFixtureNullableTarget
{
    public int Number { get; set; }
    public string? Text { get; set; }
}
