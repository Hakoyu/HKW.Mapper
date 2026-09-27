using HKW.HKWMapper;

namespace HKW.MapperTest;

[TestClass]
public sealed class ArrayAndNullableMappingTests
{
    [TestMethod]
    public void MapsArraysInBothDirections()
    {
        var source = new ArraySource { Values = [1, 2, 3] };
        var target = new ArrayTarget { Values = [9] };

        var returnedTarget = source.MapTo(target);

        Assert.AreSame(target, returnedTarget);
        Assert.AreSequenceEqual(new[] { 1, 2, 3 }, target.Values);
        Assert.AreNotSame(source.Values, target.Values);

        target.Values = [4, 5];
        var returnedSource = source.MapFrom(target);

        Assert.AreSame(source, returnedSource);
        Assert.AreSequenceEqual(new[] { 4, 5 }, source.Values);
        Assert.AreNotSame(source.Values, target.Values);
    }

    [TestMethod]
    public void MapsListsAndReplacesExistingItems()
    {
        var source = new ListCollectionSource { Items = [1, 2, 3] };
        var target = new ListCollectionTarget { Items = [99] };

        source.MapTo(target);

        Assert.AreSequenceEqual(new[] { 1, 2, 3 }, target.Items);

        target.Items = [7, 8];
        source.MapFrom(target);

        Assert.AreSequenceEqual(new[] { 7, 8 }, source.Items);
    }

    [TestMethod]
    public void MapsHashSetsAndReplacesExistingItems()
    {
        var source = new HashSetCollectionSource { Items = [1, 2, 3] };
        var target = new HashSetCollectionTarget { Items = [99] };

        source.MapTo(target);

        Assert.AreSequenceEqual(new[] { 1, 2, 3 }, target.Items);

        target.Items = [7, 8];
        source.MapFrom(target);

        Assert.AreSequenceEqual(new[] { 7, 8 }, source.Items);
    }

    [TestMethod]
    public void MapsDictionariesInBothDirections()
    {
        var source = new DictionaryCollectionSource
        {
            Values = new Dictionary<string, int> { ["one"] = 1, ["two"] = 2 },
        };
        var target = new DictionaryCollectionTarget
        {
            Values = new Dictionary<string, int> { ["old"] = 0 },
        };

        source.MapTo(target);

        Assert.AreEqual(2, target.Values.Count);
        Assert.AreEqual(1, target.Values["one"]);
        Assert.AreEqual(2, target.Values["two"]);
        Assert.IsFalse(target.Values.ContainsKey("old"));

        target.Values["three"] = 3;
        source.MapFrom(target);

        Assert.AreEqual(3, source.Values.Count);
        Assert.AreEqual(3, source.Values["three"]);
    }

    [TestMethod]
    public void MapsNestedObjectsInsideCollections()
    {
        var source = new NestedCollectionSource
        {
            Items = [new NestedSource { Value = 4 }, new NestedSource { Value = 8 }],
        };
        var target = source.MapTo(new NestedCollectionTarget());

        Assert.IsNotNull(target.Items);
        Assert.AreEqual(2, target.Items.Count);
        Assert.AreEqual(4, target.Items[0].Value);
        Assert.AreEqual(8, target.Items[1].Value);
        Assert.IsFalse(ReferenceEquals(source.Items[0], target.Items[0]));

        target.Items[0].Value = 10;
        source.MapFrom(target);

        Assert.AreEqual(10, source.Items[0].Value);
    }

    [TestMethod]
    public void MapsNullableValueTypesAndPreservesNull()
    {
        var source = new NullableSource { Number = null, Text = null };
        var target = new NullableTarget { Number = 12, Text = "old" };

        source.MapTo(target);

        Assert.AreEqual(0, target.Number);
        Assert.IsNull(target.Text);

        target.Number = 7;
        target.Text = "new";
        source.MapFrom(target);

        Assert.AreEqual(7, source.Number);
        Assert.AreEqual("new", source.Text);
    }

    [TestMethod]
    public void MappingReturnsTheProvidedTarget()
    {
        var source = new NullableSource { Number = 5, Text = "value" };

        var target = new NullableTarget();

        var returnedTarget = source.MapTo(target);

        Assert.AreSame(target, returnedTarget);
        Assert.AreEqual(5, target.Number);
        Assert.AreEqual("value", target.Text);
    }
}

[MapTarget(typeof(ArrayTarget))]
public sealed class ArraySource
{
    public int[] Values { get; set; } = [];
}

public sealed class ArrayTarget
{
    public int[] Values { get; set; } = [];
}

[MapTarget(typeof(ListCollectionTarget))]
public sealed class ListCollectionSource
{
    public List<int> Items { get; set; } = [];
}

public sealed class ListCollectionTarget
{
    public List<int> Items { get; set; } = [];
}

[MapTarget(typeof(DictionaryCollectionTarget))]
public sealed class DictionaryCollectionSource
{
    public Dictionary<string, int> Values { get; set; } = [];
}

public sealed class DictionaryCollectionTarget
{
    public Dictionary<string, int> Values { get; set; } = [];
}

[MapTarget(typeof(HashSetCollectionTarget))]
public sealed class HashSetCollectionSource
{
    public HashSet<int> Items { get; set; } = [];
}

public sealed class HashSetCollectionTarget
{
    public HashSet<int> Items { get; set; } = [];
}

[MapTarget(typeof(NestedCollectionTarget))]
public sealed class NestedCollectionSource
{
    public List<NestedSource> Items { get; set; } = [];
}

public sealed class NestedCollectionTarget
{
    public List<NestedTarget> Items { get; set; } = [];
}

[MapTarget(typeof(NestedTarget))]
public sealed class NestedSource
{
    public int Value { get; set; }
}

public sealed class NestedTarget
{
    public int Value { get; set; }
}

[MapTarget(typeof(NullableTarget))]
public sealed class NullableSource
{
    public int? Number { get; set; }
    public string? Text { get; set; }
}

public sealed class NullableTarget
{
    public int Number { get; set; }
    public string? Text { get; set; }
}
