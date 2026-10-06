using HKW.HKWMapper;

namespace HKW.MapperTest;

[TestClass]
public sealed class MapConfigCompositePropertyConverterTests
{
    [TestMethod]
    public void ConvertsBothDirectionsAndValidatesArguments()
    {
        var converter = new MapConfigCompositePropertyConverter<NumericRange, (int Min, int Max)>(
            "Range",
            ["Min", "Max"],
            (_, value) => (value.Min, value.Max),
            (_, values) => new(values.Min, values.Max)
        );

        Assert.AreEqual((2, 8), converter.Convert(new object(), new(2, 8)));
        Assert.AreEqual(new NumericRange(3, 9), converter.ConvertBack(new object(), (3, 9)));
        Assert.Throws<ArgumentException>(() =>
            new MapConfigCompositePropertyConverter<NumericRange, (int, int)>(
                "",
                ["Min", "Max"],
                (_, value) => (value.Min, value.Max),
                (_, values) => new(values.Item1, values.Item2)
            )
        );
        Assert.Throws<ArgumentException>(() =>
            new MapConfigCompositePropertyConverter<NumericRange, (int, int)>(
                "Range",
                ["Min", "Min"],
                (_, value) => (value.Min, value.Max),
                (_, values) => new(values.Item1, values.Item2)
            )
        );
        Assert.Throws<ArgumentNullException>(() =>
            new MapConfigCompositePropertyConverter<NumericRange, (int, int)>(
                "Range",
                ["Min", "Max"],
                null!,
                (_, values) => new(values.Item1, values.Item2)
            )
        );
    }

    [TestMethod]
    public void ConfiguresCompositeMappingWithoutAttribute()
    {
        var source = new ConfigCompositeSource { Range = new(4, 12) };
        var target = new ConfigCompositeTarget();

        source.MapTo(target);

        Assert.AreEqual(4, target.Min);
        Assert.AreEqual(12, target.Max);

        target.Min = 6;
        target.Max = 16;
        source.MapFrom(target);

        Assert.AreEqual(new NumericRange(6, 16), source.Range);
    }

    [TestMethod]
    public void ConfigTakesPriorityOverCompositeAttribute()
    {
        var source = new PriorityCompositeSource { Range = new(1, 2) };
        var target = new PriorityCompositeTarget();

        source.MapTo(target);

        Assert.AreEqual(0, target.AttributeMin);
        Assert.AreEqual(0, target.AttributeMax);
        Assert.AreEqual(101, target.ConfigMin);
        Assert.AreEqual(202, target.ConfigMax);

        target.ConfigMin = 110;
        target.ConfigMax = 220;
        source.MapFrom(target);

        Assert.AreEqual(new NumericRange(10, 20), source.Range);
    }
}

[MapTarget(typeof(ConfigCompositeTarget), typeof(ConfigCompositeMapperConfig))]
public sealed class ConfigCompositeSource
{
    public NumericRange Range { get; set; }
}

public sealed class ConfigCompositeTarget
{
    public int Min { get; set; }
    public int Max { get; set; }
}

public sealed class ConfigCompositeMapperConfig
    : MapperConfig<ConfigCompositeSource, ConfigCompositeTarget>
{
    public ConfigCompositeMapperConfig(ConfigCompositeSource source, ConfigCompositeTarget target)
        : base(source, target) { }

    public MapConfigCompositePropertyConverter<NumericRange, (int Min, int Max)> RangeConverter { get; } =
        new(
            nameof(ConfigCompositeSource.Range),
            [nameof(ConfigCompositeTarget.Min), nameof(ConfigCompositeTarget.Max)],
            (_, value) => (value.Min, value.Max),
            (_, values) => new(values.Min, values.Max)
        );
}

[MapTarget(typeof(PriorityCompositeTarget), typeof(PriorityCompositeMapperConfig))]
public sealed class PriorityCompositeSource
{
    [MapCompositeProperty(
        typeof(PriorityCompositeTarget),
        typeof(NumericRangeConverter),
        nameof(PriorityCompositeTarget.AttributeMin),
        nameof(PriorityCompositeTarget.AttributeMax)
    )]
    public NumericRange Range { get; set; }
}

public sealed class PriorityCompositeTarget
{
    public int AttributeMin { get; set; }
    public int AttributeMax { get; set; }
    public int ConfigMin { get; set; }
    public int ConfigMax { get; set; }
}

public sealed class PriorityCompositeMapperConfig
    : MapperConfig<PriorityCompositeSource, PriorityCompositeTarget>
{
    public PriorityCompositeMapperConfig(
        PriorityCompositeSource source,
        PriorityCompositeTarget target
    )
        : base(source, target) { }

    public MapConfigCompositePropertyConverter<NumericRange, (int Min, int Max)> RangeConverter { get; } =
        new(
            nameof(PriorityCompositeSource.Range),
            [
                nameof(PriorityCompositeTarget.ConfigMin),
                nameof(PriorityCompositeTarget.ConfigMax),
            ],
            (_, value) => (value.Min + 100, value.Max + 200),
            (_, values) => new(values.Min - 100, values.Max - 200)
        );
}
