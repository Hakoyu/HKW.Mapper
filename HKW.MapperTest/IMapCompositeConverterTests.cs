using HKW.Mapper;

namespace HKW.MapperTest;

[TestClass]
public sealed class IMapCompositeConverterTests
{
    [TestMethod]
    public void CompositeConverterMapsMoreThanTwoValuesInBothDirectionsAndReceivesArguments()
    {
        var source = new IMapCompositeTripleSource { Value = new(1, "two", true) };
        var target = new IMapCompositeTripleTarget();

        source.MapTo(target);

        Assert.AreEqual(1, target.Number);
        Assert.AreEqual("two", target.Text);
        Assert.IsTrue(target.Enabled);

        target.Number = 3;
        target.Text = "four";
        target.Enabled = false;
        source.MapFrom(target);

        Assert.AreEqual(new IMapCompositeTripleValue(3, "four", false), source.Value);
    }

    [TestMethod]
    public void CompositeConverterMapsFromReadOnlyTargetWhenDirectionIsFromOnly()
    {
        var source = new IMapCompositeReadOnlySource();

        source.MapFrom(new IMapCompositeReadOnlyTarget(5, 9));

        Assert.AreEqual(new IMapCompositeRangeValue(5, 9), source.Range);
    }

    [TestMethod]
    public void CompositeConverterSupportsInterfaceConverterTypesAndConcreteProperties()
    {
        var source = new IMapCompositeCompatibleSource { Values = [1, 2] };
        var target = new IMapCompositeCompatibleTarget();

        source.MapTo(target);

        CollectionAssert.AreEqual(new[] { "1", "2" }, target.TextValues);
        CollectionAssert.AreEqual(new[] { 2, 4 }, target.DoubledValues);

        target.TextValues = ["7", "8"];
        target.DoubledValues = [14, 16];
        source.MapFrom(target);

        CollectionAssert.AreEqual(new[] { 7, 8 }, source.Values);
    }

    [TestMethod]
    public void ConfigCompositeConverterSupportsInterfaceGenericTypesAndConcreteProperties()
    {
        var source = new IMapCompositeConfigCompatibleSource { Values = [5, 6] };
        var target = new IMapCompositeConfigCompatibleTarget();

        source.MapTo(target);

        CollectionAssert.AreEqual(new[] { "5", "6" }, target.TextValues);
        CollectionAssert.AreEqual(new[] { 10, 12 }, target.DoubledValues);

        target.TextValues = ["7", "8"];
        target.DoubledValues = [14, 16];
        source.MapFrom(target);

        CollectionAssert.AreEqual(new[] { 7, 8 }, source.Values);
    }

    [TestMethod]
    public void MapConfigCompositeConverterDelegatesBothDirectionsAndPreservesArguments()
    {
        MapCompositeConvertArgs? convertArgs = null;
        MapCompositeConvertArgs? convertBackArgs = null;
        var converter = new MapConfigCompositeMemberConverter<
            IMapCompositeRangeValue,
            (int Min, int Max)
        >(
            "UniqueRange",
            ["UniqueMin", "UniqueMax"],
            (value, args) =>
            {
                convertArgs = args;
                return (value.Min, value.Max);
            },
            (values, args) =>
            {
                convertBackArgs = args;
                return new(values.Min, values.Max);
            }
        );
        var source = new object();
        var target = new object();
        var targetPropertyNames = new[] { "UniqueMin", "UniqueMax" };
        var args = new MapCompositeConvertArgs(source, target, "UniqueRange", targetPropertyNames);

        Assert.AreEqual((2, 8), converter.Convert(new(2, 8), args));
        Assert.AreEqual(new IMapCompositeRangeValue(3, 9), converter.ConvertBack((3, 9), args));

        Assert.IsTrue(convertArgs.HasValue);
        Assert.IsTrue(convertBackArgs.HasValue);
        AssertCompositeArgs(convertArgs.Value, source, target, targetPropertyNames);
        AssertCompositeArgs(convertBackArgs.Value, source, target, targetPropertyNames);
    }

    [TestMethod]
    public void MapConfigCompositeConverterRejectsInvalidSourcePropertyNames()
    {
        foreach (var sourceProperty in new string?[] { null, string.Empty, "   " })
        {
            Assert.Throws<ArgumentException>(() =>
                CreateConfigConverter(sourceProperty!, ["A", "B"])
            );
        }
    }

    [TestMethod]
    public void MapConfigCompositeConverterRejectsInvalidTargetPropertyNames()
    {
        Assert.Throws<ArgumentNullException>(() => CreateConfigConverter("Value", null!));

        foreach (
            var targetProperties in new[]
            {
                Array.Empty<string>(),
                new[] { "A" },
                new[] { null!, "B" },
                new[] { string.Empty, "B" },
                new[] { "   ", "B" },
                new[] { "A", "A" },
            }
        )
        {
            Assert.Throws<ArgumentException>(() =>
                CreateConfigConverter("Value", targetProperties)
            );
        }
    }

    [TestMethod]
    public void MapConfigCompositeConverterRejectsNullDelegates()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new MapConfigCompositeMemberConverter<IMapCompositeRangeValue, (int, int)>(
                "Value",
                ["A", "B"],
                null!,
                (values, _) => new(values.Item1, values.Item2)
            )
        );
        Assert.Throws<ArgumentNullException>(() =>
            new MapConfigCompositeMemberConverter<IMapCompositeRangeValue, (int, int)>(
                "Value",
                ["A", "B"],
                (value, _) => (value.Min, value.Max),
                null!
            )
        );
    }

    private static MapConfigCompositeMemberConverter<
        IMapCompositeRangeValue,
        (int, int)
    > CreateConfigConverter(string sourceProperty, string[] targetProperties) =>
        new(
            sourceProperty,
            targetProperties,
            (value, _) => (value.Min, value.Max),
            (values, _) => new(values.Item1, values.Item2)
        );

    private static void AssertCompositeArgs(
        MapCompositeConvertArgs args,
        object source,
        object target,
        string[] targetPropertyNames
    )
    {
        Assert.AreSame(source, args.Source);
        Assert.AreSame(target, args.Target);
        Assert.AreEqual("UniqueRange", args.SourcePropertyName);
        Assert.AreSame(targetPropertyNames, args.TargetPropertyNames);
    }
}

public readonly record struct IMapCompositeTripleValue(int Number, string Text, bool Enabled);

[MapTarget(typeof(IMapCompositeTripleTarget))]
public sealed class IMapCompositeTripleSource
{
    [MapCompositeProperty(
        typeof(IMapCompositeTripleTarget),
        typeof(IMapCompositeTripleConverter),
        nameof(IMapCompositeTripleTarget.Number),
        nameof(IMapCompositeTripleTarget.Text),
        nameof(IMapCompositeTripleTarget.Enabled)
    )]
    public IMapCompositeTripleValue Value { get; set; }
}

public sealed class IMapCompositeTripleTarget
{
    public int Number { get; set; }
    public string Text { get; set; } = string.Empty;
    public bool Enabled { get; set; }
}

public sealed class IMapCompositeTripleConverter
    : IMapCompositeConverter<IMapCompositeTripleValue, (int Number, string Text, bool Enabled)>
{
    public (int Number, string Text, bool Enabled) Convert(
        IMapCompositeTripleValue value,
        MapCompositeConvertArgs args
    )
    {
        AssertArguments(args);
        return (value.Number, value.Text, value.Enabled);
    }

    public IMapCompositeTripleValue ConvertBack(
        (int Number, string Text, bool Enabled) values,
        MapCompositeConvertArgs args
    )
    {
        AssertArguments(args);
        return new(values.Number, values.Text, values.Enabled);
    }

    private static void AssertArguments(MapCompositeConvertArgs args)
    {
        Assert.IsInstanceOfType<IMapCompositeTripleSource>(args.Source);
        Assert.IsInstanceOfType<IMapCompositeTripleTarget>(args.Target);
        Assert.AreEqual(nameof(IMapCompositeTripleSource.Value), args.SourcePropertyName);
        CollectionAssert.AreEqual(
            new[]
            {
                nameof(IMapCompositeTripleTarget.Number),
                nameof(IMapCompositeTripleTarget.Text),
                nameof(IMapCompositeTripleTarget.Enabled),
            },
            args.TargetPropertyNames
        );
    }
}

public readonly record struct IMapCompositeRangeValue(int Min, int Max);

[MapTarget(typeof(IMapCompositeReadOnlyTarget), Direction = MapDirections.From)]
public sealed class IMapCompositeReadOnlySource
{
    [MapCompositeProperty(
        typeof(IMapCompositeReadOnlyTarget),
        typeof(IMapCompositeReadOnlyConverter),
        nameof(IMapCompositeReadOnlyTarget.Min),
        nameof(IMapCompositeReadOnlyTarget.Max)
    )]
    public IMapCompositeRangeValue Range { get; set; }
}

public sealed class IMapCompositeReadOnlyTarget(int min, int max)
{
    public int Min { get; } = min;
    public int Max { get; } = max;
}

public sealed class IMapCompositeReadOnlyConverter
    : IMapCompositeConverter<IMapCompositeRangeValue, (int Min, int Max)>
{
    public (int Min, int Max) Convert(
        IMapCompositeRangeValue value,
        MapCompositeConvertArgs args
    ) => (value.Min, value.Max);

    public IMapCompositeRangeValue ConvertBack(
        (int Min, int Max) values,
        MapCompositeConvertArgs args
    ) => new(values.Min, values.Max);
}

[MapTarget(typeof(IMapCompositeCompatibleTarget))]
public sealed class IMapCompositeCompatibleSource
{
    [MapCompositeProperty(
        typeof(IMapCompositeCompatibleTarget),
        typeof(IMapCompositeCompatibleConverter),
        nameof(IMapCompositeCompatibleTarget.TextValues),
        nameof(IMapCompositeCompatibleTarget.DoubledValues)
    )]
    public List<int> Values { get; set; } = [];
}

public sealed class IMapCompositeCompatibleTarget
{
    public List<string> TextValues { get; set; } = [];
    public List<int> DoubledValues { get; set; } = [];
}

public sealed class IMapCompositeCompatibleConverter
    : IMapCompositeConverter<
        IList<int>,
        (IList<string> TextValues, IReadOnlyList<int> DoubledValues)
    >
{
    public (IList<string> TextValues, IReadOnlyList<int> DoubledValues) Convert(
        IList<int> value,
        MapCompositeConvertArgs args
    ) => (value.Select(x => x.ToString()).ToList(), value.Select(x => x * 2).ToList());

    public IList<int> ConvertBack(
        (IList<string> TextValues, IReadOnlyList<int> DoubledValues) values,
        MapCompositeConvertArgs args
    ) => values.TextValues.Select(int.Parse).ToList();
}

[MapTarget(typeof(IMapCompositeConfigCompatibleTarget), typeof(IMapCompositeCompatibleConfig))]
public sealed class IMapCompositeConfigCompatibleSource
{
    public List<int> Values { get; set; } = [];
}

public sealed class IMapCompositeConfigCompatibleTarget
{
    public List<string> TextValues { get; set; } = [];
    public List<int> DoubledValues { get; set; } = [];
}

public sealed class IMapCompositeCompatibleConfig
    : MapperConfigBase<IMapCompositeConfigCompatibleSource, IMapCompositeConfigCompatibleTarget>
{
    public IMapCompositeCompatibleConfig(
        IMapCompositeConfigCompatibleSource source,
        IMapCompositeConfigCompatibleTarget target
    )
        : base(source, target) { }

    public MapConfigCompositeMemberConverter<
        IList<int>,
        (IList<string> TextValues, IReadOnlyList<int> DoubledValues)
    > ValuesConverter { get; } =
        new(
            nameof(IMapCompositeConfigCompatibleSource.Values),
            [
                nameof(IMapCompositeConfigCompatibleTarget.TextValues),
                nameof(IMapCompositeConfigCompatibleTarget.DoubledValues),
            ],
            (values, _) =>
                (values.Select(x => x.ToString()).ToList(), values.Select(x => x * 2).ToList()),
            (values, _) => values.TextValues.Select(int.Parse).ToList()
        );
}
