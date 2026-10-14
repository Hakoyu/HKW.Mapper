using HKW.Mapper;

namespace HKW.MapperTest;

[TestClass]
public sealed class MapPropertyTests
{
    [TestMethod]
    public void TargetTypeAndTargetNameOverloadsMapBothDirections()
    {
        var typeSource = new MptFixtureTargetTypeSource { Value = 3 };
        var typeTarget = new MptFixtureTargetTypeTarget();

        typeSource.MapTo(typeTarget);
        Assert.AreEqual(3, typeTarget.Value);

        typeTarget.Value = 7;
        typeSource.MapFrom(typeTarget);
        Assert.AreEqual(7, typeSource.Value);

        var nameSource = new MptFixtureTargetNameSource { Value = 5 };
        var nameTarget = new MptFixtureTargetNameTarget();

        nameSource.MapToMptNamedTarget(nameTarget);
        Assert.AreEqual(5, nameTarget.Value);

        nameTarget.Value = 11;
        nameSource.MapFromMptNamedTarget(nameTarget);
        Assert.AreEqual(11, nameSource.Value);
    }

    [TestMethod]
    public void PropertyNameOverloadsMapRenamedPropertiesBothDirections()
    {
        var typeSource = new MptFixtureTypePropertyNameSource { Original = 13 };
        var typeTarget = new MptFixtureTypePropertyNameTarget();

        typeSource.MapTo(typeTarget);
        Assert.AreEqual(13, typeTarget.Renamed);

        typeTarget.Renamed = 17;
        typeSource.MapFrom(typeTarget);
        Assert.AreEqual(17, typeSource.Original);

        var nameSource = new MptFixtureNamePropertyNameSource { Original = 19 };
        var nameTarget = new MptFixtureNamePropertyNameTarget();

        nameSource.MapToMptRenamedTarget(nameTarget);
        Assert.AreEqual(19, nameTarget.Renamed);

        nameTarget.Renamed = 23;
        nameSource.MapFromMptRenamedTarget(nameTarget);
        Assert.AreEqual(23, nameSource.Original);
    }

    [TestMethod]
    public void IgnoreOptionsLeaveTargetAndSourceValuesUnchanged()
    {
        var source = new MptFixtureIgnoreSource
        {
            Mapped = 2,
            IgnoredByMapProperty = 3,
            IgnoredByAttribute = 5,
        };
        var target = new MptFixtureIgnoreTarget
        {
            Mapped = 7,
            IgnoredByMapProperty = 11,
            IgnoredByAttribute = 13,
        };

        source.MapTo(target);

        Assert.AreEqual(2, target.Mapped);
        Assert.AreEqual(11, target.IgnoredByMapProperty);
        Assert.AreEqual(13, target.IgnoredByAttribute);

        target.Mapped = 17;
        target.IgnoredByMapProperty = 19;
        target.IgnoredByAttribute = 23;
        source.MapFrom(target);

        Assert.AreEqual(17, source.Mapped);
        Assert.AreEqual(3, source.IgnoredByMapProperty);
        Assert.AreEqual(5, source.IgnoredByAttribute);
    }

    [TestMethod]
    public void MapTypesControlCloneReferenceAndConstructionBothDirections()
    {
        var source = new MptFixtureMapTypeSource
        {
            Cloned = new MptFixtureCloneValue(2),
            Referenced = new MptFixtureReferenceValue(3),
            Constructed = new MptFixtureConstructValue(5),
        };
        var target = new MptFixtureMapTypeTarget();

        source.MapTo(target);

        Assert.AreEqual(2, target.Cloned.Value);
        Assert.AreNotSame(source.Cloned, target.Cloned);
        Assert.AreSame(source.Referenced, target.Referenced);
        Assert.AreEqual(5, target.Constructed.Value);
        Assert.AreNotSame(source.Constructed, target.Constructed);

        target.Cloned = new MptFixtureCloneValue(7);
        target.Referenced = new MptFixtureReferenceValue(11);
        target.Constructed = new MptFixtureConstructValue(13);
        var targetClone = target.Cloned;
        var targetReference = target.Referenced;
        var targetConstructed = target.Constructed;

        source.MapFrom(target);

        Assert.AreEqual(7, source.Cloned.Value);
        Assert.AreNotSame(targetClone, source.Cloned);
        Assert.AreSame(targetReference, source.Referenced);
        Assert.AreEqual(13, source.Constructed.Value);
        Assert.AreNotSame(targetConstructed, source.Constructed);
    }
}

[MapTarget(typeof(MptFixtureTargetTypeTarget))]
public sealed class MptFixtureTargetTypeSource
{
    [MapProperty(typeof(MptFixtureTargetTypeTarget))]
    public int Value { get; set; }
}

public sealed class MptFixtureTargetTypeTarget
{
    public int Value { get; set; }
}

[MapTarget(typeof(MptFixtureTargetNameTarget), TargetName = "MptNamedTarget")]
public sealed class MptFixtureTargetNameSource
{
    [MapProperty("MptNamedTarget")]
    public int Value { get; set; }
}

public sealed class MptFixtureTargetNameTarget
{
    public int Value { get; set; }
}

[MapTarget(typeof(MptFixtureTypePropertyNameTarget))]
public sealed class MptFixtureTypePropertyNameSource
{
    [MapProperty(typeof(MptFixtureTypePropertyNameTarget), nameof(MptFixtureTypePropertyNameTarget.Renamed))]
    public int Original { get; set; }
}

public sealed class MptFixtureTypePropertyNameTarget
{
    public int Renamed { get; set; }
}

[MapTarget(typeof(MptFixtureNamePropertyNameTarget), TargetName = "MptRenamedTarget")]
public sealed class MptFixtureNamePropertyNameSource
{
    [MapProperty("MptRenamedTarget", nameof(MptFixtureNamePropertyNameTarget.Renamed))]
    public int Original { get; set; }
}

public sealed class MptFixtureNamePropertyNameTarget
{
    public int Renamed { get; set; }
}

[MapTarget(typeof(MptFixtureIgnoreTarget))]
public sealed class MptFixtureIgnoreSource
{
    public int Mapped { get; set; }

    [MapProperty(typeof(MptFixtureIgnoreTarget), Ignore = true)]
    public int IgnoredByMapProperty { get; set; }

    [MapIgnoreProperty]
    public int IgnoredByAttribute { get; set; }
}

public sealed class MptFixtureIgnoreTarget
{
    public int Mapped { get; set; }
    public int IgnoredByMapProperty { get; set; }
    public int IgnoredByAttribute { get; set; }
}

[MapTarget(typeof(MptFixtureMapTypeTarget))]
public sealed class MptFixtureMapTypeSource
{
    [MapProperty(typeof(MptFixtureMapTypeTarget), MapType = MapPropertyType.None)]
    public MptFixtureCloneValue Cloned { get; set; } = new(0);

    [MapProperty(typeof(MptFixtureMapTypeTarget), MapType = MapPropertyType.Reference)]
    public MptFixtureReferenceValue Referenced { get; set; } = new(0);

    [MapProperty(typeof(MptFixtureMapTypeTarget), MapType = MapPropertyType.ConstructFromSelf)]
    public MptFixtureConstructValue Constructed { get; set; } = new(0);
}

public sealed class MptFixtureMapTypeTarget
{
    public MptFixtureCloneValue Cloned { get; set; } = new(0);
    public MptFixtureReferenceValue Referenced { get; set; } = new(0);
    public MptFixtureConstructValue Constructed { get; set; } = new(0);
}

public sealed class MptFixtureCloneValue(int value) : ICloneable
{
    public int Value { get; } = value;

    public object Clone() => new MptFixtureCloneValue(Value);
}

public sealed class MptFixtureReferenceValue(int value)
{
    public int Value { get; } = value;
}

public sealed class MptFixtureConstructValue
{
    public MptFixtureConstructValue(int value) => Value = value;

    public MptFixtureConstructValue(MptFixtureConstructValue value) => Value = value.Value;

    public int Value { get; }
}
