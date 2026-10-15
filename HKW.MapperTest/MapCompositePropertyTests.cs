using HKW.Mapper;

namespace HKW.MapperTest;

[TestClass]
public sealed class MapCompositePropertyTests
{
    [TestMethod]
    public void SelectsCompositeAttributeByTargetTypeAndTargetName()
    {
        var source = new CompositePropertySelectionSource
        {
            Value = new CompositePropertySelectionValue(3, 7),
        };
        var typeTarget = new CompositePropertyTypeSelectionTarget();
        var nameTarget = new CompositePropertyNameSelectionTarget();

        source.MapTo(typeTarget);
        source.MapToCompositePropertyNamed(nameTarget);

        Assert.AreEqual(3, typeTarget.Left);
        Assert.AreEqual(7, typeTarget.Right);
        Assert.AreEqual(3, nameTarget.Start);
        Assert.AreEqual(7, nameTarget.End);

        typeTarget.Left = 11;
        typeTarget.Right = 13;
        source.MapFrom(typeTarget);
        Assert.AreEqual(new CompositePropertySelectionValue(11, 13), source.Value);

        nameTarget.Start = 17;
        nameTarget.End = 19;
        source.MapFromCompositePropertyNamed(nameTarget);
        Assert.AreEqual(new CompositePropertySelectionValue(17, 19), source.Value);
    }

    [TestMethod]
    public void CompositePropertySupportsFullSourcePathBothDirections()
    {
        var source = new CompositePropertySourcePathSource
        {
            Node = new() { Value = new(23, 29) },
        };
        var target = new CompositePropertySourcePathTarget();

        source.MapTo(target);

        Assert.AreEqual(23, target.Low);
        Assert.AreEqual(29, target.High);
        Assert.AreEqual(
            "Node.Value",
            CompositePropertySourcePathConverter.LastSourcePropertyName
        );

        target.Low = 31;
        target.High = 37;
        source.MapFrom(target);

        Assert.AreEqual(new CompositePropertySourcePathValue(31, 37), source.Node.Value);
        Assert.AreEqual(
            "Node.Value",
            CompositePropertySourcePathConverter.LastSourcePropertyName
        );
    }

    [TestMethod]
    public void PreservesTargetPropertyNamesOrderAndWiresGeneratedParameters()
    {
        var source = new CompositePropertyWiringSource
        {
            Value = new CompositePropertyWiringValue(23, "mapped", true),
        };
        var target = new CompositePropertyWiringTarget();

        source.MapTo(target);

        Assert.AreEqual("mapped", target.TextSlot);
        Assert.IsTrue(target.EnabledSlot);
        Assert.AreEqual(23, target.NumberSlot);

        target.TextSlot = "returned";
        target.EnabledSlot = false;
        target.NumberSlot = 29;
        source.MapFrom(target);

        Assert.AreEqual(new CompositePropertyWiringValue(29, "returned", false), source.Value);
    }
}

public readonly record struct CompositePropertySelectionValue(int First, int Second);

[MapTarget(typeof(CompositePropertyTypeSelectionTarget))]
[MapTarget(
    typeof(CompositePropertyNameSelectionTarget),
    TargetName = "CompositePropertyNamed"
)]
public sealed class CompositePropertySelectionSource
{
    [MapCompositeProperty(
        typeof(CompositePropertyTypeSelectionTarget),
        typeof(CompositePropertySelectionConverter),
        nameof(CompositePropertyTypeSelectionTarget.Left),
        nameof(CompositePropertyTypeSelectionTarget.Right)
    )]
    [MapCompositeProperty(
        "CompositePropertyNamed",
        typeof(CompositePropertySelectionConverter),
        nameof(CompositePropertyNameSelectionTarget.Start),
        nameof(CompositePropertyNameSelectionTarget.End)
    )]
    public CompositePropertySelectionValue Value { get; set; }
}

public sealed class CompositePropertyTypeSelectionTarget
{
    public int Left { get; set; }
    public int Right { get; set; }
}

public sealed class CompositePropertyNameSelectionTarget
{
    public int Start { get; set; }
    public int End { get; set; }
}

public sealed class CompositePropertySelectionConverter
    : IMapCompositeConverter<CompositePropertySelectionValue, (int First, int Second)>
{
    public (int First, int Second) Convert(
        CompositePropertySelectionValue value,
        MapCompositeConvertArgs args
    ) => (value.First, value.Second);

    public CompositePropertySelectionValue ConvertBack(
        (int First, int Second) values,
        MapCompositeConvertArgs args
    ) => new(values.First, values.Second);
}

public readonly record struct CompositePropertySourcePathValue(int Low, int High);

[MapTarget(typeof(CompositePropertySourcePathTarget))]
public sealed class CompositePropertySourcePathSource
{
    [MapCompositeProperty(
        typeof(CompositePropertySourcePathTarget),
        typeof(CompositePropertySourcePathConverter),
        nameof(CompositePropertySourcePathTarget.Low),
        nameof(CompositePropertySourcePathTarget.High),
        SourcePath = "Node.Value"
    )]
    public CompositePropertySourcePathNode Node { get; set; } = new();
}

public sealed class CompositePropertySourcePathNode
{
    public CompositePropertySourcePathValue Value { get; set; }
}

public sealed class CompositePropertySourcePathTarget
{
    public int Low { get; set; }
    public int High { get; set; }
}

public sealed class CompositePropertySourcePathConverter
    : IMapCompositeConverter<CompositePropertySourcePathValue, (int Low, int High)>
{
    public static string LastSourcePropertyName { get; private set; } = string.Empty;

    public (int Low, int High) Convert(
        CompositePropertySourcePathValue value,
        MapCompositeConvertArgs args
    )
    {
        LastSourcePropertyName = args.SourcePropertyName;
        return (value.Low, value.High);
    }

    public CompositePropertySourcePathValue ConvertBack(
        (int Low, int High) values,
        MapCompositeConvertArgs args
    )
    {
        LastSourcePropertyName = args.SourcePropertyName;
        return new(values.Low, values.High);
    }
}

public readonly record struct CompositePropertyWiringValue(
    int Number,
    string Text,
    bool Enabled
);

[MapTarget(typeof(CompositePropertyWiringTarget))]
public sealed class CompositePropertyWiringSource
{
    [MapCompositeProperty(
        typeof(CompositePropertyWiringTarget),
        typeof(CompositePropertyWiringConverter),
        nameof(CompositePropertyWiringTarget.TextSlot),
        nameof(CompositePropertyWiringTarget.EnabledSlot),
        nameof(CompositePropertyWiringTarget.NumberSlot)
    )]
    public CompositePropertyWiringValue Value { get; set; }
}

public sealed class CompositePropertyWiringTarget
{
    public int NumberSlot { get; set; }
    public string TextSlot { get; set; } = string.Empty;
    public bool EnabledSlot { get; set; }
}

public sealed class CompositePropertyWiringConverter
    : IMapCompositeConverter<CompositePropertyWiringValue, (string Text, bool Enabled, int Number)>
{
    public (string Text, bool Enabled, int Number) Convert(
        CompositePropertyWiringValue value,
        MapCompositeConvertArgs args
    )
    {
        AssertGeneratedArguments(args);
        return (value.Text, value.Enabled, value.Number);
    }

    public CompositePropertyWiringValue ConvertBack(
        (string Text, bool Enabled, int Number) values,
        MapCompositeConvertArgs args
    )
    {
        AssertGeneratedArguments(args);
        return new(values.Number, values.Text, values.Enabled);
    }

    private static void AssertGeneratedArguments(MapCompositeConvertArgs args)
    {
        Assert.IsInstanceOfType<CompositePropertyWiringSource>(args.Source);
        Assert.IsInstanceOfType<CompositePropertyWiringTarget>(args.Target);
        Assert.AreEqual(nameof(CompositePropertyWiringSource.Value), args.SourcePropertyName);
        CollectionAssert.AreEqual(
            new[]
            {
                nameof(CompositePropertyWiringTarget.TextSlot),
                nameof(CompositePropertyWiringTarget.EnabledSlot),
                nameof(CompositePropertyWiringTarget.NumberSlot),
            },
            args.TargetPropertyNames
        );
    }
}
