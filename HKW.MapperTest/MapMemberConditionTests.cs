using HKW.Mapper;

namespace HKW.MapperTest;

[TestClass]
public sealed class MapMemberConditionTests
{
    [TestMethod]
    public void RegularPropertiesEvaluateAllConditionsAgainstOriginalRoles()
    {
        var source = new ConditionPropertySource
        {
            ToSourceDefault = 7,
            ToSourceNotDefault = 0,
            ToTargetDefault = "target-default",
            ToTargetNotDefault = "target-not-default",
        };
        var target = new ConditionPropertyTarget
        {
            ToSourceDefault = 11,
            ToSourceNotDefault = 13,
            ToTargetDefault = null,
            ToTargetNotDefault = "existing",
        };

        source.MapTo(target);

        Assert.AreEqual(11, target.ToSourceDefault);
        Assert.AreEqual(13, target.ToSourceNotDefault);
        Assert.AreEqual("target-default", target.ToTargetDefault);
        Assert.AreEqual("target-not-default", target.ToTargetNotDefault);

        source = new ConditionPropertySource
        {
            FromSourceDefault = null,
            FromSourceNotDefault = 17,
            FromTargetDefault = 19,
            FromTargetNotDefault = "unchanged",
        };
        target = new ConditionPropertyTarget
        {
            FromSourceDefault = "mapped",
            FromSourceNotDefault = 23,
            FromTargetDefault = null,
            FromTargetNotDefault = "returned",
        };

        source.MapFrom(target);

        Assert.AreEqual("mapped", source.FromSourceDefault);
        Assert.AreEqual(23, source.FromSourceNotDefault);
        Assert.IsNull(source.FromTargetDefault);
        Assert.AreEqual("returned", source.FromTargetNotDefault);
    }

    [TestMethod]
    public void NullableValueTypesUseDefaultSemantics()
    {
        var source = new ConditionNullableSource { Value = null };
        var target = new ConditionNullableTarget { Value = 31 };

        source.MapTo(target);
        Assert.IsNull(target.Value);

        source.Value = 37;
        target.Value = 41;
        source.MapTo(target);
        Assert.AreEqual(41, target.Value);
    }

    [TestMethod]
    public void FieldsConvertersAndCollectionsAreSkippedAsCompleteMappingUnits()
    {
        ConditionCountingConverter.Reset();
        var source = new ConditionMixedSource
        {
            FieldValue = 43,
            Converted = 0,
            Items = null,
        };
        var target = new ConditionMixedTarget
        {
            FieldValue = 47,
            Converted = 53,
            Items = [59],
        };

        source.MapTo(target);

        Assert.AreEqual(47, target.FieldValue);
        Assert.AreEqual(53, target.Converted);
        CollectionAssert.AreEqual(new[] { 59 }, target.Items);
        Assert.AreEqual(0, ConditionCountingConverter.ConvertCalls);

        source.FieldValue = 0;
        source.Converted = 61;
        source.Items = [67, 71];
        target.FieldValue = 73;
        source.MapTo(target);

        Assert.AreEqual(0, target.FieldValue);
        Assert.AreEqual(61, target.Converted);
        CollectionAssert.AreEqual(new[] { 67, 71 }, target.Items);
        Assert.AreEqual(1, ConditionCountingConverter.ConvertCalls);

        target.Converted = 0;
        source.MapFrom(target);
        Assert.AreEqual(61, source.Converted);
        Assert.AreEqual(0, ConditionCountingConverter.ConvertBackCalls);
    }

    [TestMethod]
    public void CompositeConditionsRequireAllTargetsDefaultAndNegateTheWholeGroup()
    {
        ConditionCompositeConverter.Reset();
        var source = new ConditionCompositeSource
        {
            PropertyRange = new(3, 5),
            FieldRange = new(7, 11),
        };
        var target = new ConditionCompositeTarget
        {
            PropertyLow = 0,
            PropertyHigh = 13,
            FieldLow = 0,
            FieldHigh = 17,
        };

        source.MapTo(target);

        Assert.AreEqual(0, target.PropertyLow);
        Assert.AreEqual(13, target.PropertyHigh);
        Assert.AreEqual(7, target.FieldLow);
        Assert.AreEqual(11, target.FieldHigh);
        Assert.AreEqual(1, ConditionCompositeConverter.ConvertCalls);

        target.PropertyLow = 0;
        target.PropertyHigh = 0;
        target.FieldLow = 0;
        target.FieldHigh = 0;
        source.MapTo(target);

        Assert.AreEqual(3, target.PropertyLow);
        Assert.AreEqual(5, target.PropertyHigh);
        Assert.AreEqual(0, target.FieldLow);
        Assert.AreEqual(0, target.FieldHigh);
        Assert.AreEqual(2, ConditionCompositeConverter.ConvertCalls);

        source.PropertyRange = default;
        target.PropertyLow = 19;
        target.PropertyHigh = 23;
        source.MapFrom(target);
        Assert.AreEqual(new ConditionRange(19, 23), source.PropertyRange);

        source.PropertyRange = new(29, 31);
        target.PropertyLow = 37;
        target.PropertyHigh = 41;
        source.MapFrom(target);
        Assert.AreEqual(new ConditionRange(29, 31), source.PropertyRange);
    }
}

[MapTarget(typeof(ConditionPropertyTarget))]
public sealed class ConditionPropertySource
{
    [MapProperty(
        typeof(ConditionPropertyTarget),
        MapToCondition = MapMemberCondition.SourceIsDefault
    )]
    public int ToSourceDefault { get; set; }

    [MapProperty(
        typeof(ConditionPropertyTarget),
        MapToCondition = MapMemberCondition.SourceIsNotDefault
    )]
    public int ToSourceNotDefault { get; set; }

    [MapProperty(
        typeof(ConditionPropertyTarget),
        MapToCondition = MapMemberCondition.TargetIsDefault
    )]
    public string? ToTargetDefault { get; set; }

    [MapProperty(
        typeof(ConditionPropertyTarget),
        MapToCondition = MapMemberCondition.TargetIsNotDefault
    )]
    public string? ToTargetNotDefault { get; set; }

    [MapProperty(
        typeof(ConditionPropertyTarget),
        MapFromCondition = MapMemberCondition.SourceIsDefault
    )]
    public string? FromSourceDefault { get; set; }

    [MapProperty(
        typeof(ConditionPropertyTarget),
        MapFromCondition = MapMemberCondition.SourceIsNotDefault
    )]
    public int FromSourceNotDefault { get; set; }

    [MapProperty(
        typeof(ConditionPropertyTarget),
        MapFromCondition = MapMemberCondition.TargetIsDefault
    )]
    public int? FromTargetDefault { get; set; }

    [MapProperty(
        typeof(ConditionPropertyTarget),
        MapFromCondition = MapMemberCondition.TargetIsNotDefault
    )]
    public string? FromTargetNotDefault { get; set; }
}

public sealed class ConditionPropertyTarget
{
    public int ToSourceDefault { get; set; }
    public int ToSourceNotDefault { get; set; }
    public string? ToTargetDefault { get; set; }
    public string? ToTargetNotDefault { get; set; }
    public string? FromSourceDefault { get; set; }
    public int FromSourceNotDefault { get; set; }
    public int? FromTargetDefault { get; set; }
    public string? FromTargetNotDefault { get; set; }
}

[MapTarget(typeof(ConditionNullableTarget))]
public sealed class ConditionNullableSource
{
    [MapProperty(
        typeof(ConditionNullableTarget),
        MapToCondition = MapMemberCondition.SourceIsDefault
    )]
    public int? Value { get; set; }
}

public sealed class ConditionNullableTarget
{
    public int? Value { get; set; }
}

[MapTarget(typeof(ConditionMixedTarget), MapFields = true)]
public sealed class ConditionMixedSource
{
    [MapField(typeof(ConditionMixedTarget), MapToCondition = MapMemberCondition.SourceIsDefault)]
    public int FieldValue;

    [MapProperty(
        typeof(ConditionMixedTarget),
        typeof(ConditionCountingConverter),
        MapToCondition = MapMemberCondition.SourceIsNotDefault,
        MapFromCondition = MapMemberCondition.TargetIsNotDefault
    )]
    public int Converted { get; set; }

    [MapProperty(
        typeof(ConditionMixedTarget),
        Direction = MapDirections.To,
        MapToCondition = MapMemberCondition.SourceIsNotDefault
    )]
    public List<int>? Items { get; set; }
}

public sealed class ConditionMixedTarget
{
    public int FieldValue;
    public int Converted { get; set; }
    public List<int>? Items { get; set; }
}

public sealed class ConditionCountingConverter : IMapConverter<int, int>
{
    public static int ConvertCalls { get; private set; }
    public static int ConvertBackCalls { get; private set; }

    public static void Reset()
    {
        ConvertCalls = 0;
        ConvertBackCalls = 0;
    }

    public int Convert(int value, MapConvertArgs args)
    {
        ConvertCalls++;
        return value;
    }

    public int ConvertBack(int value, MapConvertArgs args)
    {
        ConvertBackCalls++;
        return value;
    }
}

public readonly record struct ConditionRange(int Low, int High);

[MapTarget(typeof(ConditionCompositeTarget), MapFields = true)]
public sealed class ConditionCompositeSource
{
    [MapCompositeProperty(
        typeof(ConditionCompositeTarget),
        typeof(ConditionCompositeConverter),
        nameof(ConditionCompositeTarget.PropertyLow),
        nameof(ConditionCompositeTarget.PropertyHigh),
        MapToCondition = MapMemberCondition.TargetIsDefault,
        MapFromCondition = MapMemberCondition.SourceIsDefault
    )]
    public ConditionRange PropertyRange { get; set; }

    [MapCompositeField(
        typeof(ConditionCompositeTarget),
        typeof(ConditionCompositeConverter),
        nameof(ConditionCompositeTarget.FieldLow),
        nameof(ConditionCompositeTarget.FieldHigh),
        MapToCondition = MapMemberCondition.TargetIsNotDefault
    )]
    public ConditionRange FieldRange;
}

public sealed class ConditionCompositeTarget
{
    public int PropertyLow { get; set; }
    public int PropertyHigh { get; set; }
    public int FieldLow;
    public int FieldHigh;
}

public sealed class ConditionCompositeConverter
    : IMapCompositeConverter<ConditionRange, (int Low, int High)>
{
    public static int ConvertCalls { get; private set; }

    public static void Reset() => ConvertCalls = 0;

    public (int Low, int High) Convert(ConditionRange value, MapCompositeConvertArgs args)
    {
        ConvertCalls++;
        return (value.Low, value.High);
    }

    public ConditionRange ConvertBack((int Low, int High) values, MapCompositeConvertArgs args) =>
        new(values.Low, values.High);
}
