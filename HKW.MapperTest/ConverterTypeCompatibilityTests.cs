using HKW.HKWMapper;

namespace HKW.MapperTest;

[TestClass]
public sealed class ConverterTypeCompatibilityTests
{
    [TestMethod]
    public void AttributeConverterSupportsInterfaceTypesInBothDirections()
    {
        var source = new CompatibleAttributeSource { Values = [1, 2] };
        var target = new CompatibleAttributeTarget();

        source.MapTo(target);

        CollectionAssert.AreEqual(new[] { "1", "2" }, target.Values);

        target.Values = ["3", "4"];
        source.MapFrom(target);

        CollectionAssert.AreEqual(new[] { 3, 4 }, source.Values);
    }

    [TestMethod]
    public void ConverterSupportsConcreteSubtypeForInterfaceProperties()
    {
        var source = new CompatibleSubtypeSource { Values = new List<int> { 9, 10 } };
        var target = new CompatibleSubtypeTarget();

        source.MapTo(target);

        CollectionAssert.AreEqual(new[] { "9", "10" }, target.Values.ToList());

        target.Values = new List<string> { "11", "12" };
        source.MapFrom(target);

        CollectionAssert.AreEqual(new[] { 11, 12 }, source.Values.ToList());
    }

    [TestMethod]
    public void ExplicitSubtypeConversionThrowsForIncompatibleRuntimeType()
    {
        var source = new CompatibleSubtypeSource { Values = new[] { 1, 2 } };

        Assert.Throws<InvalidCastException>(() => source.MapTo(new CompatibleSubtypeTarget()));
    }

    [TestMethod]
    public void ConfigConverterSupportsInterfaceTypesInBothDirections()
    {
        var source = new CompatibleConfigSource { Values = [5, 6] };
        var target = new CompatibleConfigTarget();

        source.MapTo(target);

        CollectionAssert.AreEqual(new[] { "5", "6" }, target.Values);

        target.Values = ["7", "8"];
        source.MapFrom(target);

        CollectionAssert.AreEqual(new[] { 7, 8 }, source.Values);
    }

    [TestMethod]
    public void AttributeCompositeConverterSupportsInterfaceTypesInBothDirections()
    {
        var source = new CompatibleCompositeAttributeSource { Values = [1, 2] };
        var target = new CompatibleCompositeAttributeTarget();

        source.MapTo(target);

        CollectionAssert.AreEqual(new[] { "1", "2" }, target.TextValues);
        CollectionAssert.AreEqual(new[] { 2, 4 }, target.DoubledValues);

        target.TextValues = ["3", "4"];
        target.DoubledValues = [6, 8];
        source.MapFrom(target);

        CollectionAssert.AreEqual(new[] { 3, 4 }, source.Values);
    }

    [TestMethod]
    public void ConfigCompositeConverterSupportsInterfaceTypesInBothDirections()
    {
        var source = new CompatibleCompositeConfigSource { Values = [5, 6] };
        var target = new CompatibleCompositeConfigTarget();

        source.MapTo(target);

        CollectionAssert.AreEqual(new[] { "5", "6" }, target.TextValues);
        CollectionAssert.AreEqual(new[] { 10, 12 }, target.DoubledValues);

        target.TextValues = ["7", "8"];
        target.DoubledValues = [14, 16];
        source.MapFrom(target);

        CollectionAssert.AreEqual(new[] { 7, 8 }, source.Values);
    }
}

public sealed class CompatibleListConverter : IMapConverter<IList<int>, IList<string>>
{
    public IList<string> Convert(object source, IList<int> value) =>
        value.Select(x => x.ToString()).ToList();

    public IList<int> ConvertBack(object target, IList<string> value) =>
        value.Select(int.Parse).ToList();
}

public sealed class CompatibleConcreteListConverter
    : IMapConverter<List<int>, List<string>>
{
    public List<string> Convert(object source, List<int> value) =>
        value.Select(x => x.ToString()).ToList();

    public List<int> ConvertBack(object target, List<string> value) =>
        value.Select(int.Parse).ToList();
}

[MapTarget(typeof(CompatibleSubtypeTarget))]
public sealed class CompatibleSubtypeSource
{
    [MapProperty(typeof(CompatibleSubtypeTarget), typeof(CompatibleConcreteListConverter))]
    public IList<int> Values { get; set; } = new List<int>();
}

public sealed class CompatibleSubtypeTarget
{
    public IList<string> Values { get; set; } = new List<string>();
}

[MapTarget(typeof(CompatibleAttributeTarget))]
public sealed class CompatibleAttributeSource
{
    [MapProperty(typeof(CompatibleAttributeTarget), typeof(CompatibleListConverter))]
    public List<int> Values { get; set; } = [];
}

public sealed class CompatibleAttributeTarget
{
    public List<string> Values { get; set; } = [];
}

[MapTarget(typeof(CompatibleConfigTarget), typeof(CompatibleMapperConfig))]
public sealed class CompatibleConfigSource
{
    public List<int> Values { get; set; } = [];
}

public sealed class CompatibleConfigTarget
{
    public List<string> Values { get; set; } = [];
}

public sealed class CompatibleMapperConfig
    : MapperConfig<CompatibleConfigSource, CompatibleConfigTarget>
{
    public CompatibleMapperConfig(CompatibleConfigSource source, CompatibleConfigTarget target)
        : base(source, target) { }

    public MapConfigPropertyConverter<IList<int>, IList<string>> ValuesConverter { get; } =
        new(
            nameof(CompatibleConfigSource.Values),
            (_, values) => values.Select(x => x.ToString()).ToList(),
            (_, values) => values.Select(int.Parse).ToList()
        );
}

public sealed class CompatibleCompositeConverter
    : ICompositeMapConverter<IList<int>, (IList<string> TextValues, IReadOnlyList<int> DoubledValues)>
{
    public (IList<string> TextValues, IReadOnlyList<int> DoubledValues) Convert(
        object source,
        IList<int> value
    ) => (value.Select(x => x.ToString()).ToList(), value.Select(x => x * 2).ToList());

    public IList<int> ConvertBack(
        object target,
        (IList<string> TextValues, IReadOnlyList<int> DoubledValues) values
    ) => values.TextValues.Select(int.Parse).ToList();
}

[MapTarget(typeof(CompatibleCompositeAttributeTarget))]
public sealed class CompatibleCompositeAttributeSource
{
    [MapCompositeProperty(
        typeof(CompatibleCompositeAttributeTarget),
        typeof(CompatibleCompositeConverter),
        nameof(CompatibleCompositeAttributeTarget.TextValues),
        nameof(CompatibleCompositeAttributeTarget.DoubledValues)
    )]
    public List<int> Values { get; set; } = [];
}

public sealed class CompatibleCompositeAttributeTarget
{
    public List<string> TextValues { get; set; } = [];
    public List<int> DoubledValues { get; set; } = [];
}

[MapTarget(typeof(CompatibleCompositeConfigTarget), typeof(CompatibleCompositeMapperConfig))]
public sealed class CompatibleCompositeConfigSource
{
    public List<int> Values { get; set; } = [];
}

public sealed class CompatibleCompositeConfigTarget
{
    public List<string> TextValues { get; set; } = [];
    public List<int> DoubledValues { get; set; } = [];
}

public sealed class CompatibleCompositeMapperConfig
    : MapperConfig<CompatibleCompositeConfigSource, CompatibleCompositeConfigTarget>
{
    public CompatibleCompositeMapperConfig(
        CompatibleCompositeConfigSource source,
        CompatibleCompositeConfigTarget target
    )
        : base(source, target) { }

    public MapConfigCompositePropertyConverter<
        IList<int>,
        (IList<string> TextValues, IReadOnlyList<int> DoubledValues)
    > ValuesConverter
    { get; } =
        new(
            nameof(CompatibleCompositeConfigSource.Values),
            [
                nameof(CompatibleCompositeConfigTarget.TextValues),
                nameof(CompatibleCompositeConfigTarget.DoubledValues),
            ],
            (_, values) =>
                (
                    values.Select(x => x.ToString()).ToList(),
                    values.Select(x => x * 2).ToList()
                ),
            (_, values) => values.TextValues.Select(int.Parse).ToList()
        );
}
