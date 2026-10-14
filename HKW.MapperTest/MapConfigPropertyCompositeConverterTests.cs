using HKW.Mapper;

namespace HKW.MapperTest;

[TestClass]
public sealed class MapConfigPropertyCompositeConverterTests
{
    [TestMethod]
    public void ConvertsBothDirectionsAndValidatesArguments()
    {
        var converter = new MapConfigPropertyCompositeConverter<NumericRange, (int Min, int Max)>(
            "Range",
            ["Min", "Max"],
            (value, _) => (value.Min, value.Max),
            (values, _) => new(values.Min, values.Max)
        );

        var source = new object();
        var target = new object();
        var targetPropertyNames = new[] { "Min", "Max" };
        var args = new MapCompositeConvertArgs(source, target, "Range", targetPropertyNames);

        Assert.AreSame(source, args.Source);
        Assert.AreSame(target, args.Target);
        Assert.AreEqual("Range", args.SourcePropertyName);
        CollectionAssert.AreEqual(targetPropertyNames, args.TargetPropertyNames);
        Assert.AreEqual((2, 8), converter.Convert(new(2, 8), args));
        Assert.AreEqual(new NumericRange(3, 9), converter.ConvertBack((3, 9), args));
        Assert.Throws<ArgumentException>(() =>
            new MapConfigPropertyCompositeConverter<NumericRange, (int, int)>(
                "",
                ["Min", "Max"],
                (value, _) => (value.Min, value.Max),
                (values, _) => new(values.Item1, values.Item2)
            )
        );
        Assert.Throws<ArgumentException>(() =>
            new MapConfigPropertyCompositeConverter<NumericRange, (int, int)>(
                "Range",
                ["Min", "Min"],
                (value, _) => (value.Min, value.Max),
                (values, _) => new(values.Item1, values.Item2)
            )
        );
        Assert.Throws<ArgumentNullException>(() =>
            new MapConfigPropertyCompositeConverter<NumericRange, (int, int)>(
                "Range",
                ["Min", "Max"],
                null!,
                (values, _) => new(values.Item1, values.Item2)
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

    public MapConfigPropertyCompositeConverter<
        NumericRange,
        (int Min, int Max)
    > RangeConverter { get; } =
        new(
            nameof(ConfigCompositeSource.Range),
            [nameof(ConfigCompositeTarget.Min), nameof(ConfigCompositeTarget.Max)],
            (value, args) =>
            {
                Assert.IsInstanceOfType<ConfigCompositeSource>(args.Source);
                Assert.IsInstanceOfType<ConfigCompositeTarget>(args.Target);
                Assert.AreEqual(nameof(ConfigCompositeSource.Range), args.SourcePropertyName);
                CollectionAssert.AreEqual(
                    new[] { nameof(ConfigCompositeTarget.Min), nameof(ConfigCompositeTarget.Max) },
                    args.TargetPropertyNames
                );
                return (value.Min, value.Max);
            },
            (values, args) =>
            {
                Assert.IsInstanceOfType<ConfigCompositeSource>(args.Source);
                Assert.IsInstanceOfType<ConfigCompositeTarget>(args.Target);
                Assert.AreEqual(nameof(ConfigCompositeSource.Range), args.SourcePropertyName);
                CollectionAssert.AreEqual(
                    new[] { nameof(ConfigCompositeTarget.Min), nameof(ConfigCompositeTarget.Max) },
                    args.TargetPropertyNames
                );
                return new(values.Min, values.Max);
            }
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

    public MapConfigPropertyCompositeConverter<
        NumericRange,
        (int Min, int Max)
    > RangeConverter { get; } =
        new(
            nameof(PriorityCompositeSource.Range),
            [nameof(PriorityCompositeTarget.ConfigMin), nameof(PriorityCompositeTarget.ConfigMax)],
            (value, _) => (value.Min + 100, value.Max + 200),
            (values, _) => new(values.Min - 100, values.Max - 200)
        );
}
