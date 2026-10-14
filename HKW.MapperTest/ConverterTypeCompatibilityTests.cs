using HKW.Mapper;

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

        Assert.AreEqual("1,2", target.Values);

        target.Values = "3,4";
        source.MapFrom(target);

        CollectionAssert.AreEqual(new[] { 3, 4 }, source.Values);
    }

    [TestMethod]
    public void ConverterSupportsConcreteSubtypeForInterfaceProperties()
    {
        var source = new CompatibleSubtypeSource
        {
            Values = new List<int> { 9, 10 },
        };
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
    public void ConverterSupportsBoxingToEnumBaseTypeInBothDirections()
    {
        var source = new EnumWrapperSource
        {
            Mode = new EnumWrapper<CompatibleMode>(CompatibleMode.First),
        };
        var target = new EnumWrapperTarget();

        source.MapTo(target);

        Assert.AreEqual(CompatibleMode.First, target.Mode);

        target.Mode = CompatibleMode.Second;
        source.MapFrom(target);

        Assert.AreEqual(CompatibleMode.Second, source.Mode.Value);
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

public sealed class CompatibleListConverter : IMapConverter<IList<int>, string>
{
    public string Convert(IList<int> value, MapConvertArgs args) => string.Join(',', value);

    public IList<int> ConvertBack(string value, MapConvertArgs args) =>
        value.Split(',').Select(int.Parse).ToList();
}

public sealed class CompatibleConcreteListConverter : IMapConverter<List<int>, List<string>>
{
    public List<string> Convert(List<int> value, MapConvertArgs args) =>
        value.Select(x => x.ToString()).ToList();

    public List<int> ConvertBack(List<string> value, MapConvertArgs args) =>
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

public interface IEnumWrapper
{
    Enum Value { get; }
}

public sealed class EnumWrapper<TEnum>(TEnum value) : IEnumWrapper
    where TEnum : struct, Enum
{
    public TEnum Value { get; } = value;

    Enum IEnumWrapper.Value => Value;
}

public enum CompatibleMode
{
    First,
    Second,
}

public sealed class CompatibleEnumConverter : IMapConverter<IEnumWrapper, Enum>
{
    public Enum Convert(IEnumWrapper value, MapConvertArgs args) => value.Value;

    public IEnumWrapper ConvertBack(Enum value, MapConvertArgs args) =>
        new EnumWrapper<CompatibleMode>((CompatibleMode)value);
}

[MapTarget(typeof(EnumWrapperTarget))]
public sealed class EnumWrapperSource
{
    [MapProperty(typeof(EnumWrapperTarget), typeof(CompatibleEnumConverter))]
    public EnumWrapper<CompatibleMode> Mode { get; set; } = new(CompatibleMode.First);
}

public sealed class EnumWrapperTarget
{
    public CompatibleMode Mode { get; set; }
}

[MapTarget(typeof(CompatibleAttributeTarget))]
public sealed class CompatibleAttributeSource
{
    [MapProperty(typeof(CompatibleAttributeTarget), typeof(CompatibleListConverter))]
    public List<int> Values { get; set; } = [];
}

public sealed class CompatibleAttributeTarget
{
    public string Values { get; set; } = string.Empty;
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
            (values, _) => values.Select(x => x.ToString()).ToList(),
            (values, _) => values.Select(int.Parse).ToList()
        );
}

public sealed class CompatibleCompositeConverter
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

    public MapConfigPropertyCompositeConverter<
        IList<int>,
        (IList<string> TextValues, IReadOnlyList<int> DoubledValues)
    > ValuesConverter { get; } =
        new(
            nameof(CompatibleCompositeConfigSource.Values),
            [
                nameof(CompatibleCompositeConfigTarget.TextValues),
                nameof(CompatibleCompositeConfigTarget.DoubledValues),
            ],
            (values, _) =>
                (values.Select(x => x.ToString()).ToList(), values.Select(x => x * 2).ToList()),
            (values, _) => values.TextValues.Select(int.Parse).ToList()
        );
}
