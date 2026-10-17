using HKW.Mapper;

namespace HKW.MapperTest;

[TestClass]
public sealed class MapFieldTests
{
    [TestMethod]
    public void ExplicitFieldMappingSupportsFieldsPropertiesIgnoresAndInheritance()
    {
        var source = new FieldSource
        {
            FieldToField = 1,
            FieldToProperty = 2,
            PropertyToField = 3,
            InternalField = 4,
            Ignored = 5,
            ParentField = 6,
        };
        var target = new FieldTarget { Ignored = 50 };

        source.MapTo(target);

        Assert.AreEqual(1, target.FieldToField);
        Assert.AreEqual(2, target.FieldToProperty);
        Assert.AreEqual(3, target.PropertyToField);
        Assert.AreEqual(4, target.InternalField);
        Assert.AreEqual(50, target.Ignored);
        Assert.AreEqual(6, target.ParentField);

        target.FieldToField = 11;
        target.FieldToProperty = 12;
        target.PropertyToField = 13;
        target.InternalField = 14;
        target.ParentField = 16;
        source.MapFrom(target);

        Assert.AreEqual(11, source.FieldToField);
        Assert.AreEqual(12, source.FieldToProperty);
        Assert.AreEqual(13, source.PropertyToField);
        Assert.AreEqual(14, source.InternalField);
        Assert.AreEqual(5, source.Ignored);
        Assert.AreEqual(16, source.ParentField);
    }

    [TestMethod]
    public void NestedPathsMixTargetPropertiesAndFieldsBothDirections()
    {
        var source = new NestedFieldPathSource { ThroughProperty = 17, ThroughField = 19 };
        var target = new NestedFieldPathTarget();

        source.MapTo(target);

        Assert.AreEqual(17, target.PropertyNode.ValueField);
        Assert.AreEqual(19, target.FieldNode.ValueProperty);

        target.PropertyNode.ValueField = 23;
        target.FieldNode.ValueProperty = 29;
        source.MapFrom(target);

        Assert.AreEqual(23, source.ThroughProperty);
        Assert.AreEqual(29, source.ThroughField);
    }

    [TestMethod]
    public void FieldAttributesSupportFullSourcePathsBothDirections()
    {
        var source = new FieldSourcePathSource
        {
            DirectNode = new() { Value = 39 },
            NumberNode = new() { Value = 41 },
            RangeNode = new() { Value = new(43, 47) },
        };
        var target = new FieldSourcePathTarget();

        source.MapTo(target);

        Assert.AreEqual(39, target.Direct);
        Assert.AreEqual("41", target.Text);
        Assert.AreEqual(43, target.Low);
        Assert.AreEqual(47, target.High);
        Assert.AreEqual("NumberNode.Value", FieldSourcePathNumberConverter.LastSourcePath);
        Assert.AreEqual("RangeNode.Value", FieldSourcePathRangeConverter.LastSourcePath);

        target.Direct = 51;
        target.Text = "53";
        target.Low = 59;
        target.High = 61;
        source.MapFrom(target);

        Assert.AreEqual(51, source.DirectNode.Value);
        Assert.AreEqual(53, source.NumberNode.Value);
        Assert.AreEqual(new FieldRange(59, 61), source.RangeNode.Value);
        Assert.AreEqual("NumberNode.Value", FieldSourcePathNumberConverter.LastSourcePath);
        Assert.AreEqual("RangeNode.Value", FieldSourcePathRangeConverter.LastSourcePath);
    }

    [TestMethod]
    public void FieldAttributesUseRegularAndCompositeConverters()
    {
        var source = new FieldConverterSource { Number = 7, Range = new(2, 5) };
        var target = new FieldConverterTarget();

        source.MapTo(target);

        Assert.AreEqual("7", target.Text);
        Assert.AreEqual(2, target.Low);
        Assert.AreEqual(5, target.High);

        target.Text = "11";
        target.Low = 13;
        target.High = 17;
        source.MapFrom(target);

        Assert.AreEqual(11, source.Number);
        Assert.AreEqual(new FieldRange(13, 17), source.Range);
    }

    [TestMethod]
    public void ConfigFieldConvertersMapRegularAndCompositeFields()
    {
        var source = new ConfigFieldSource { Number = 3, Range = new(5, 7) };
        var target = new ConfigFieldTarget();

        source.MapTo(target);

        Assert.AreEqual("value:3", target.Text);
        Assert.AreEqual(5, target.Low);
        Assert.AreEqual(7, target.High);

        target.Text = "value:19";
        target.Low = 23;
        target.High = 29;
        source.MapFrom(target);

        Assert.AreEqual(19, source.Number);
        Assert.AreEqual(new FieldRange(23, 29), source.Range);
    }

    [TestMethod]
    public void ReadonlyFieldsMapInTheirReadableDirections()
    {
        var toSource = new ReadonlyToSource(31);
        var toTarget = new ReadonlyToTarget();
        toSource.MapTo(toTarget);
        Assert.AreEqual(31, toTarget.Value);

        var fromSource = new ReadonlyFromSource();
        var fromTarget = new ReadonlyFromTarget(37);
        fromSource.MapFrom(fromTarget);
        Assert.AreEqual(37, fromSource.Value);
    }

    [TestMethod]
    public void FieldsRemainDisabledByDefault()
    {
        var source = new DisabledFieldSource { Value = 41 };
        var target = new DisabledFieldTarget { Value = 43 };

        source.MapTo(target);

        Assert.AreEqual(43, target.Value);
    }

    [TestMethod]
    public void MemberDirectionsLimitRegularAndCompositeFieldMappings()
    {
        var source = new DirectionFieldSource
        {
            ToOnly = 3,
            FromOnly = 5,
            CompositeTo = new(7, 11),
            CompositeFrom = new(13, 17),
        };
        var target = new DirectionFieldTarget
        {
            ToOnly = 19,
            FromOnly = 23,
            ToLow = 29,
            ToHigh = 31,
            FromLow = 37,
            FromHigh = 41,
        };

        source.MapTo(target);

        Assert.AreEqual(3, target.ToOnly);
        Assert.AreEqual(23, target.FromOnly);
        Assert.AreEqual(7, target.ToLow);
        Assert.AreEqual(11, target.ToHigh);
        Assert.AreEqual(37, target.FromLow);
        Assert.AreEqual(41, target.FromHigh);

        target.ToOnly = 43;
        target.FromOnly = 47;
        target.ToLow = 53;
        target.ToHigh = 59;
        target.FromLow = 61;
        target.FromHigh = 67;
        source.MapFrom(target);

        Assert.AreEqual(3, source.ToOnly);
        Assert.AreEqual(47, source.FromOnly);
        Assert.AreEqual(new FieldRange(7, 11), source.CompositeTo);
        Assert.AreEqual(new FieldRange(61, 67), source.CompositeFrom);
    }
}

[MapTarget(typeof(NestedFieldPathTarget), MapFields = true)]
public sealed class NestedFieldPathSource
{
    [MapField(typeof(NestedFieldPathTarget), "PropertyNode.ValueField")]
    public int ThroughProperty;

    [MapField(typeof(NestedFieldPathTarget), "FieldNode.ValueProperty")]
    public int ThroughField;
}

public sealed class NestedFieldPathTarget
{
    public NestedPropertyNode PropertyNode { get; set; } = new();
    public NestedFieldNode FieldNode = new();
}

public sealed class NestedPropertyNode
{
    public int ValueField;
}

public sealed class NestedFieldNode
{
    public int ValueProperty { get; set; }
}

public class FieldSourceBase
{
    public int ParentField;
}

[MapTarget(
    typeof(FieldTarget),
    MapFields = true,
    MapParentFields = true,
    IgnoreFields = new[] { nameof(FieldSource.Ignored) }
)]
public sealed class FieldSource : FieldSourceBase
{
    public int FieldToField;
    public int FieldToProperty;
    public int PropertyToField { get; set; }
    internal int InternalField;
    public int Ignored;
}

public sealed class FieldTarget
{
    public int FieldToField;
    public int FieldToProperty { get; set; }
    public int PropertyToField;
    internal int InternalField;
    public int Ignored;
    public int ParentField;
}

public readonly record struct FieldRange(int Low, int High);

[MapTarget(typeof(FieldSourcePathTarget), MapFields = true)]
public sealed class FieldSourcePathSource
{
    [MapField(
        typeof(FieldSourcePathTarget),
        nameof(FieldSourcePathTarget.Direct),
        SourcePath = "DirectNode.Value"
    )]
    public FieldSourcePathNumberNode DirectNode = new();

    [MapField(
        typeof(FieldSourcePathTarget),
        nameof(FieldSourcePathTarget.Text),
        typeof(FieldSourcePathNumberConverter),
        SourcePath = "NumberNode.Value"
    )]
    public FieldSourcePathNumberNode NumberNode = new();

    [MapCompositeField(
        typeof(FieldSourcePathTarget),
        typeof(FieldSourcePathRangeConverter),
        nameof(FieldSourcePathTarget.Low),
        nameof(FieldSourcePathTarget.High),
        SourcePath = "RangeNode.Value"
    )]
    public FieldSourcePathRangeNode RangeNode = new();
}

public sealed class FieldSourcePathNumberNode
{
    public int Value;
}

public sealed class FieldSourcePathRangeNode
{
    public FieldRange Value;
}

public sealed class FieldSourcePathTarget
{
    public int Direct;
    public string Text { get; set; } = string.Empty;
    public int Low;
    public int High { get; set; }
}

public sealed class FieldSourcePathNumberConverter : IMapConverter<int, string>
{
    public static string LastSourcePath { get; private set; } = string.Empty;

    public string Convert(int value, MapConvertArgs args)
    {
        LastSourcePath = args.SourcePropertyName;
        return value.ToString();
    }

    public int ConvertBack(string value, MapConvertArgs args)
    {
        LastSourcePath = args.SourcePropertyName;
        return int.Parse(value);
    }
}

public sealed class FieldSourcePathRangeConverter
    : IMapCompositeConverter<FieldRange, (int Low, int High)>
{
    public static string LastSourcePath { get; private set; } = string.Empty;

    public (int Low, int High) Convert(FieldRange value, MapCompositeConvertArgs args)
    {
        LastSourcePath = args.SourcePropertyName;
        return (value.Low, value.High);
    }

    public FieldRange ConvertBack((int Low, int High) values, MapCompositeConvertArgs args)
    {
        LastSourcePath = args.SourcePropertyName;
        return new(values.Low, values.High);
    }
}

[MapTarget(typeof(FieldConverterTarget), MapFields = true)]
public sealed class FieldConverterSource
{
    [MapField(
        typeof(FieldConverterTarget),
        nameof(FieldConverterTarget.Text),
        typeof(FieldNumberConverter)
    )]
    public int Number;

    [MapCompositeField(
        typeof(FieldConverterTarget),
        typeof(FieldRangeConverter),
        nameof(FieldConverterTarget.Low),
        nameof(FieldConverterTarget.High)
    )]
    public FieldRange Range;
}

public sealed class FieldConverterTarget
{
    public string Text { get; set; } = string.Empty;
    public int Low;
    public int High { get; set; }
}

public sealed class FieldNumberConverter : IMapConverter<int, string>
{
    public string Convert(int value, MapConvertArgs args) => value.ToString();

    public int ConvertBack(string value, MapConvertArgs args) => int.Parse(value);
}

public sealed class FieldRangeConverter : IMapCompositeConverter<FieldRange, (int Low, int High)>
{
    public (int Low, int High) Convert(FieldRange value, MapCompositeConvertArgs args) =>
        (value.Low, value.High);

    public FieldRange ConvertBack((int Low, int High) values, MapCompositeConvertArgs args) =>
        new(values.Low, values.High);
}

[MapTarget(typeof(ConfigFieldTarget), typeof(ConfigFieldMap), MapFields = true)]
public sealed class ConfigFieldSource
{
    public int Number;
    public FieldRange Range;
}

public sealed class ConfigFieldTarget
{
    public string Text = string.Empty;
    public int Low;
    public int High;
}

public sealed class ConfigFieldMap : MapperConfig<ConfigFieldSource, ConfigFieldTarget>
{
    public ConfigFieldMap(ConfigFieldSource source, ConfigFieldTarget target)
        : base(source, target) { }

    public MapConfigFieldConverter<int, string> NumberConverter { get; } =
        new(
            nameof(ConfigFieldSource.Number),
            nameof(ConfigFieldTarget.Text),
            (value, _) => $"value:{value}",
            (value, _) => int.Parse(value[6..])
        );

    public MapConfigCompositeFieldConverter<
        FieldRange,
        (int Low, int High)
    > RangeConverter { get; } =
        new(
            nameof(ConfigFieldSource.Range),
            [nameof(ConfigFieldTarget.Low), nameof(ConfigFieldTarget.High)],
            (value, _) => (value.Low, value.High),
            (value, _) => new(value.Low, value.High)
        );
}

[MapTarget(typeof(ReadonlyToTarget), Direction = MapDirections.To, MapFields = true)]
public sealed class ReadonlyToSource(int value)
{
    public readonly int Value = value;
}

public sealed class ReadonlyToTarget
{
    public int Value;
}

[MapTarget(typeof(ReadonlyFromTarget), Direction = MapDirections.From, MapFields = true)]
public sealed class ReadonlyFromSource
{
    public int Value;
}

public sealed class ReadonlyFromTarget(int value)
{
    public readonly int Value = value;
}

[MapTarget(typeof(DisabledFieldTarget))]
public sealed class DisabledFieldSource
{
    public int Value;
}

public sealed class DisabledFieldTarget
{
    public int Value;
}

[MapTarget(typeof(DirectionFieldTarget), MapFields = true)]
public sealed class DirectionFieldSource
{
    [MapField(typeof(DirectionFieldTarget), Direction = MapDirections.To)]
    public int ToOnly;

    [MapField(typeof(DirectionFieldTarget), Direction = MapDirections.From)]
    public int FromOnly;

    [MapCompositeField(
        typeof(DirectionFieldTarget),
        typeof(FieldRangeConverter),
        nameof(DirectionFieldTarget.ToLow),
        nameof(DirectionFieldTarget.ToHigh),
        Direction = MapDirections.To
    )]
    public FieldRange CompositeTo;

    [MapCompositeField(
        typeof(DirectionFieldTarget),
        typeof(FieldRangeConverter),
        nameof(DirectionFieldTarget.FromLow),
        nameof(DirectionFieldTarget.FromHigh),
        Direction = MapDirections.From
    )]
    public FieldRange CompositeFrom;
}

public sealed class DirectionFieldTarget
{
    public int ToOnly;
    public int FromOnly;
    public int ToLow;
    public int ToHigh;
    public int FromLow;
    public int FromHigh;
}
