using HKW.HKWMapper;

namespace HKW.MapperTest;

[TestClass]
public sealed class MapPropertyAttributeTests
{
    [TestMethod]
    public void CopiesInBothDirections()
    {
        var source = new ConstructSource { Value = new ConstructValue(7) };
        var target = new ConstructTarget { Value = new ConstructValue(1) };

        source.MapTo(target);

        Assert.AreEqual(7, target.Value.Value);
        Assert.AreNotSame(source.Value, target.Value);

        target.Value = new ConstructValue(12);
        source.MapFrom(target);

        Assert.AreEqual(12, source.Value.Value);
        Assert.AreNotSame(source.Value, target.Value);
    }

    [TestMethod]
    public void PropertyNameMapsToDifferentTargetProperty1()
    {
        var source = new PropertyNameSource { Original = 3 };
        var target = new PropertyNameTarget();

        source.MapTo(target);

        Assert.AreEqual(3, target.Mapped);
        target.Mapped = 8;
        source.MapFrom(target);
        Assert.AreEqual(8, source.Original);
    }

    [TestMethod]
    public void PropertyNameMapsToDifferentTargetProperty2()
    {
        var source = new PropertyTargetNameSource { Original = 3 };
        var target = new PropertyTargetNameTarget();

        source.MapTo(target);

        Assert.AreEqual(3, target.Mapped);
        target.Mapped = 8;
        source.MapFrom(target);
        Assert.AreEqual(8, source.Original);
    }

    [TestMethod]
    public void ConverterTypeConvertsBothDirections()
    {
        var source = new ConverterTypeSource { Number = 4 };
        var target = new ConverterTypeTarget();

        source.MapTo(target);

        Assert.AreEqual("number:4", target.Text);
        target.Text = "number:9";
        source.MapFrom(target);
        Assert.AreEqual(9, source.Number);
    }

    [TestMethod]
    public void IgnoreDoesNotMapProperty()
    {
        var source = new PropertyIgnoreSource { Value = 7 };
        var target = new PropertyIgnoreTarget { Value = 1 };

        source.MapTo(target);

        Assert.AreEqual(1, target.Value);
        target.Value = 9;
        source.MapFrom(target);
        Assert.AreEqual(7, source.Value);
    }

    [TestMethod]
    public void CloneableProperty()
    {
        var source = new CloneablePropertySource { Value = new(123) };
        var target = new CloneablePropertyTarget();

        source.MapTo(target);

        Assert.AreEqual(123, target.Value.Value);
        Assert.AreNotSame(source.Value, target.Value);
        target.Value.Value = 8;
        source.MapFrom(target);
        Assert.AreEqual(8, source.Value.Value);
        Assert.AreNotSame(source.Value, target.Value);
    }
}

public sealed class ConstructValue
{
    public ConstructValue(int value) => Value = value;

    public ConstructValue(ConstructValue value) => Value = value.Value;

    public int Value { get; }
}

[MapTarget(typeof(ConstructTarget))]
public sealed class ConstructSource
{
    [MapProperty(
        typeof(ConstructTarget),
        nameof(Value),
        MapType = MapPropertyType.ConstructFromSelf
    )]
    public ConstructValue Value { get; set; } = new(0);
}

public sealed class ConstructTarget
{
    public ConstructValue Value { get; set; } = new(0);
}

[MapTarget(typeof(PropertyNameTarget))]
public sealed class PropertyNameSource
{
    [MapProperty(typeof(PropertyNameTarget), "Mapped")]
    public int Original { get; set; }
}

public sealed class PropertyNameTarget
{
    public int Mapped { get; set; }
}

[MapTarget(typeof(PropertyTargetNameTarget))]
public sealed class PropertyTargetNameSource
{
    [MapProperty(nameof(PropertyTargetNameTarget), "Mapped")]
    public int Original { get; set; }
}

public sealed class PropertyTargetNameTarget
{
    public int Mapped { get; set; }
}

//[MapTarget(typeof(PropertyConverterTypeTarget))]
[MapTarget(typeof(PropertyConverterTypeSource))]
public sealed class PropertyConverterTypeSource
{
    [MapProperty(typeof(PropertyConverterTypeTarget), typeof(NumberToDoubleNumber))]
    public double Value { get; set; }
}

public sealed class PropertyConverterTypeTarget
{
    public double Value { get; set; }
}

[MapTarget(typeof(CloneablePropertyTarget))]
public sealed class CloneablePropertySource
{
    [MapProperty(typeof(CloneablePropertyTarget))]
    public CloneableValue Value { get; set; } = default!;
}

public sealed class CloneablePropertyTarget
{
    public CloneableValue Value { get; set; } = default!;
}

[MapTarget(typeof(ConverterTypeTarget))]
public sealed class ConverterTypeSource
{
    [MapProperty(typeof(ConverterTypeTarget), "Text", typeof(NumberTextConverter))]
    public int Number { get; set; }
}

public sealed class ConverterTypeTarget
{
    public string Text { get; set; } = string.Empty;
}

public sealed class NumberTextConverter : IMapConverter<int, string>
{
    public string Convert(object source, int value) => $"number:{value}";

    public int ConvertBack(object target, string value) => int.Parse(value[7..]);
}

[MapTarget(typeof(PropertyIgnoreTarget))]
public sealed class PropertyIgnoreSource
{
    [MapProperty(typeof(PropertyIgnoreTarget), nameof(Value), Ignore = true)]
    public int Value { get; set; }
}

public sealed class PropertyIgnoreTarget
{
    public int Value { get; set; }
}

public class NumberToDoubleNumber : IMapConverter<double, double>
{
    public double Convert(object source, double value)
    {
        return value * 2;
    }

    public double ConvertBack(object source, double value)
    {
        return value / 2;
    }
}

public class CloneableValue : ICloneable
{
    public CloneableValue(int value)
    {
        Value = value;
    }

    public int Value { get; set; }

    public object Clone()
    {
        return new CloneableValue(Value);
    }
}
