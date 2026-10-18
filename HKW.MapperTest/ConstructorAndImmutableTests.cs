using System.Collections.Frozen;
using System.Collections.Immutable;
using HKW.Mapper;

namespace HKW.MapperTest;

[TestClass]
public sealed class ConstructorAndImmutableTests
{
    [TestMethod]
    public void NullTargetUsesAccessibleParameterlessConstructor()
    {
        var source = new ConstructorSource { Id = 7, Name = "mapped" };

        var target = source.MapTo((ConstructorTarget?)null);

        Assert.AreEqual(7, target.Id);
        Assert.AreEqual("mapped", target.Name);
    }

    [TestMethod]
    public void NonNullTargetPreservesWriteInBehavior()
    {
        var source = new ConstructorSource { Id = 8, Name = "updated" };
        var target = new ConstructorTarget { Id = 1, Name = "old" };

        var result = source.MapTo(target);

        Assert.AreSame(target, result);
        Assert.AreEqual(8, target.Id);
        Assert.AreEqual("updated", target.Name);
    }

    [TestMethod]
    public void SelectedConstructorReceivesCallerArgumentsAndInitializesRequiredMembers()
    {
        var source = new SelectedConstructorSource { Name = "Ada" };

        var target = source.MapTo((SelectedConstructorTarget?)null, 42);

        Assert.AreEqual(42, target.Seed);
        Assert.AreEqual("Ada", target.Name);
        Assert.ThrowsExactly<ArgumentException>(() =>
            source.MapTo(new SelectedConstructorTarget(1) { Name = "old" }, 2)
        );
    }

    [TestMethod]
    public void RequiredSettableMemberIsInitializedDuringCreation()
    {
        var target = new RequiredSettableSource { Name = "required" }.MapTo(
            (RequiredSettableTarget?)null
        );

        Assert.AreEqual("required", target.Name);
    }

    [TestMethod]
    public void TargetNameSelectsConstructorOverload()
    {
        var target = new NamedConstructorSource { Value = 5 }.MapToAlias(
            (NamedConstructorTarget?)null,
            "tag"
        );

        Assert.AreEqual("tag", target.Tag);
        Assert.AreEqual(5, target.Value);
    }

    [TestMethod]
    public async Task ConstructorOverloadPreservesAsyncConfigActions()
    {
        var target = await new AsyncConstructorSource { Value = 6 }.MapToAsync(
            (AsyncConstructorTarget?)null,
            11
        );

        Assert.AreEqual(11, target.Seed);
        Assert.AreEqual(6, target.Value);
        Assert.IsTrue(target.Started);
    }

    [TestMethod]
    public void RecordAndStructSourcesGenerateMappings()
    {
        var recordSource = new RecordMappingSource(12);
        var recordTarget = recordSource.MapTo((RecordMappingTarget?)null);
        Assert.AreEqual(12, recordTarget.Value);

        var structSource = new StructMappingSource { Value = 3 };
        structSource.MapFrom(new StructMappingTarget { Value = 9 });
        Assert.AreEqual(9, structSource.Value);

        var recordStructTarget = new RecordStructSource { Value = 14 }.MapTo(
            default(RecordStructTarget)
        );
        Assert.AreEqual(14, recordStructTarget.Value);
    }

    [TestMethod]
    public void ImmutableAndFrozenCollectionsAreCreatedAndMappedBack()
    {
        var source = new ImmutableCollectionSource
        {
            ArrayValues = [1, 2],
            ListValues = [1, 2],
            SetValues = [3, 4],
            FrozenSetValues = [3, 4],
            Pairs = new Dictionary<string, int> { ["one"] = 1 },
            FrozenPairs = new Dictionary<string, int> { ["one"] = 1 },
        };

        var target = source.MapTo((ImmutableCollectionTarget?)null);

        CollectionAssert.AreEqual(new[] { 1, 2 }, target.Array.ToArray());
        CollectionAssert.AreEqual(new[] { 1, 2 }, target.List.ToArray());
        CollectionAssert.AreEquivalent(new[] { 3, 4 }, target.Set.ToArray());
        Assert.AreEqual(1, target.Dictionary["one"]);
        CollectionAssert.AreEquivalent(new[] { 3, 4 }, target.FrozenSet.ToArray());
        Assert.AreEqual(1, target.FrozenDictionary["one"]);

        target.Array = [8, 9];
        target.Set = ImmutableHashSet.Create(10);
        target.Dictionary = ImmutableDictionary<string, int>.Empty.Add("two", 2);
        source.MapFrom(target);

        CollectionAssert.AreEqual(new[] { 8, 9 }, source.ArrayValues);
        CollectionAssert.AreEquivalent(new[] { 10 }, source.SetValues.ToArray());
        Assert.AreEqual(2, source.Pairs["two"]);
    }

    [TestMethod]
    public void NullableImmutableCollectionsPreserveNull()
    {
        var target = new NullableImmutableSource().MapTo((NullableImmutableTarget?)null);
        Assert.IsNull(target.Items);

        var source = new NullableImmutableSource { Items = [1] };
        source.MapFrom(new NullableImmutableTarget { Items = null });
        Assert.IsNull(source.Items);
    }

    [TestMethod]
    public void ImmutableCollectionCanInitializeInitOnlyTargetMember()
    {
        var target = new InitImmutableSource { Items = [4, 5] }.MapTo((InitImmutableTarget?)null);

        CollectionAssert.AreEqual(new[] { 4, 5 }, target.Items.ToArray());
    }
}

[MapTarget(typeof(ConstructorTarget))]
public sealed class ConstructorSource
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public sealed class ConstructorTarget
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

[MapTarget(typeof(SelectedConstructorTarget), Direction = MapDirections.To)]
[MapConstructor(typeof(SelectedConstructorTarget), typeof(int))]
public sealed class SelectedConstructorSource
{
    public string Name { get; set; } = string.Empty;
}

public sealed class SelectedConstructorTarget(int seed)
{
    public int Seed { get; } = seed;
    public required string Name { get; init; }
}

[MapTarget(typeof(RequiredSettableTarget), Direction = MapDirections.To)]
public sealed class RequiredSettableSource
{
    public string Name { get; set; } = string.Empty;
}

public sealed class RequiredSettableTarget
{
    public required string Name { get; set; }
}

[MapTarget(typeof(NamedConstructorTarget), TargetName = "Alias", Direction = MapDirections.To)]
[MapConstructor("Alias", typeof(string))]
public sealed class NamedConstructorSource
{
    public int Value { get; set; }
}

public sealed class NamedConstructorTarget(string tag)
{
    public string Tag { get; } = tag;
    public int Value { get; set; }
}

[MapTarget(
    typeof(AsyncConstructorTarget),
    typeof(AsyncConstructorConfig),
    Direction = MapDirections.To
)]
[MapConstructor(typeof(AsyncConstructorTarget), typeof(int))]
public sealed class AsyncConstructorSource
{
    public int Value { get; set; }
}

public sealed class AsyncConstructorTarget(int seed)
{
    public int Seed { get; } = seed;
    public int Value { get; set; }
    public bool Started { get; set; }
}

public sealed class AsyncConstructorConfig
    : MapperConfigBase<AsyncConstructorSource, AsyncConstructorTarget>
{
    public AsyncConstructorConfig(AsyncConstructorSource source, AsyncConstructorTarget target)
        : base(source, target) { }

    [MapToConfigAction(MapConfigActionMode.Start)]
    public async Task StartAsync()
    {
        await Task.Yield();
        Target.Started = true;
    }
}

[MapTarget(typeof(RecordMappingTarget), Direction = MapDirections.To)]
public sealed record RecordMappingSource(int Value);

public sealed record RecordMappingTarget
{
    public int Value { get; init; }
}

[MapTarget(typeof(StructMappingTarget))]
public struct StructMappingSource
{
    public int Value { get; set; }
}

public struct StructMappingTarget
{
    public int Value { get; set; }
}

[MapTarget(typeof(RecordStructTarget), Direction = MapDirections.To)]
public sealed class RecordStructSource
{
    public int Value { get; set; }
}

public readonly record struct RecordStructTarget
{
    public int Value { get; init; }
}

[MapTarget(typeof(ImmutableCollectionTarget))]
public sealed class ImmutableCollectionSource
{
    [MapProperty(typeof(ImmutableCollectionTarget), nameof(ImmutableCollectionTarget.Array))]
    public List<int> ArrayValues { get; set; } = [];

    [MapProperty(typeof(ImmutableCollectionTarget), nameof(ImmutableCollectionTarget.List))]
    public List<int> ListValues { get; set; } = [];

    [MapProperty(typeof(ImmutableCollectionTarget), nameof(ImmutableCollectionTarget.Set))]
    public HashSet<int> SetValues { get; set; } = [];

    [MapProperty(typeof(ImmutableCollectionTarget), nameof(ImmutableCollectionTarget.FrozenSet))]
    public HashSet<int> FrozenSetValues { get; set; } = [];

    [MapProperty(typeof(ImmutableCollectionTarget), nameof(ImmutableCollectionTarget.Dictionary))]
    public Dictionary<string, int> Pairs { get; set; } = [];

    [MapProperty(
        typeof(ImmutableCollectionTarget),
        nameof(ImmutableCollectionTarget.FrozenDictionary)
    )]
    public Dictionary<string, int> FrozenPairs { get; set; } = [];
}

[MapTarget(typeof(InitImmutableTarget), Direction = MapDirections.To)]
public sealed class InitImmutableSource
{
    public List<int> Items { get; set; } = [];
}

public sealed class InitImmutableTarget
{
    public ImmutableArray<int> Items { get; init; }
}

[MapTarget(typeof(NullableImmutableTarget))]
public sealed class NullableImmutableSource
{
    public List<int>? Items { get; set; }
}

public sealed class NullableImmutableTarget
{
    public ImmutableList<int>? Items { get; set; }
}

public sealed class ImmutableCollectionTarget
{
    public ImmutableArray<int> Array { get; set; }
    public ImmutableList<int> List { get; set; } = [];
    public ImmutableHashSet<int> Set { get; set; } = [];
    public ImmutableDictionary<string, int> Dictionary { get; set; } =
        ImmutableDictionary<string, int>.Empty;
    public FrozenSet<int> FrozenSet { get; set; } = System.Array.Empty<int>().ToFrozenSet();
    public FrozenDictionary<string, int> FrozenDictionary { get; set; } =
        new Dictionary<string, int>().ToFrozenDictionary();
}
