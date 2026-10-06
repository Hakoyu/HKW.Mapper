using HKW.HKWMapper;

namespace HKW.MapperTest;

[TestClass]
public sealed class CompositeMapConverterTests
{
    [TestMethod]
    public void MapsOnePropertyToMultiplePropertiesInBothDirections()
    {
        var source = new CompositeRangeSource { Range = new(2, 8) };
        var target = new CompositeRangeTarget();

        source.MapTo(target);

        Assert.AreEqual(2, target.Min);
        Assert.AreEqual(8, target.Max);

        target.Min = 3;
        target.Max = 13;
        source.MapFrom(target);

        Assert.AreEqual(new NumericRange(3, 13), source.Range);
    }

    [TestMethod]
    public void MapsFromReadOnlyTargetsForFromOnlyMapping()
    {
        var source = new CompositeFromOnlySource();

        source.MapFrom(new CompositeFromOnlyTarget(5, 9));

        Assert.AreEqual(new NumericRange(5, 9), source.Range);
    }

    [TestMethod]
    public void SupportsMoreThanTwoTargetProperties()
    {
        var source = new CompositeTripleSource { Value = new(1, "two", true) };
        var target = new CompositeTripleTarget();

        source.MapTo(target);

        Assert.AreEqual(1, target.Number);
        Assert.AreEqual("two", target.Text);
        Assert.IsTrue(target.Enabled);

        target.Number = 4;
        target.Text = "five";
        target.Enabled = false;
        source.MapFrom(target);

        Assert.AreEqual(new TripleValue(4, "five", false), source.Value);
    }
}

public readonly record struct NumericRange(int Min, int Max);

[MapTarget(typeof(CompositeRangeTarget))]
public sealed class CompositeRangeSource
{
    [MapCompositeProperty(
        typeof(CompositeRangeTarget),
        typeof(NumericRangeConverter),
        nameof(CompositeRangeTarget.Min),
        nameof(CompositeRangeTarget.Max)
    )]
    public NumericRange Range { get; set; }
}

public sealed class CompositeRangeTarget
{
    public int Min { get; set; }
    public int Max { get; set; }
}

public sealed class NumericRangeConverter
    : ICompositeMapConverter<NumericRange, (int Min, int Max)>
{
    public (int Min, int Max) Convert(object source, NumericRange value) =>
        (value.Min, value.Max);

    public NumericRange ConvertBack(object target, (int Min, int Max) values) =>
        new(values.Min, values.Max);
}

[MapTarget(typeof(CompositeFromOnlyTarget), Direction = MapDirections.From)]
public sealed class CompositeFromOnlySource
{
    [MapCompositeProperty(
        typeof(CompositeFromOnlyTarget),
        typeof(NumericRangeConverter),
        nameof(CompositeFromOnlyTarget.Min),
        nameof(CompositeFromOnlyTarget.Max)
    )]
    public NumericRange Range { get; set; }
}

public sealed class CompositeFromOnlyTarget(int min, int max)
{
    public int Min { get; } = min;
    public int Max { get; } = max;
}

public readonly record struct TripleValue(int Number, string Text, bool Enabled);

[MapTarget(typeof(CompositeTripleTarget))]
public sealed class CompositeTripleSource
{
    [MapCompositeProperty(
        typeof(CompositeTripleTarget),
        typeof(TripleValueConverter),
        nameof(CompositeTripleTarget.Number),
        nameof(CompositeTripleTarget.Text),
        nameof(CompositeTripleTarget.Enabled)
    )]
    public TripleValue Value { get; set; }
}

public sealed class CompositeTripleTarget
{
    public int Number { get; set; }
    public string Text { get; set; } = string.Empty;
    public bool Enabled { get; set; }
}

public sealed class TripleValueConverter
    : ICompositeMapConverter<TripleValue, (int Number, string Text, bool Enabled)>
{
    public (int Number, string Text, bool Enabled) Convert(object source, TripleValue value) =>
        (value.Number, value.Text, value.Enabled);

    public TripleValue ConvertBack(
        object target,
        (int Number, string Text, bool Enabled) values
    ) => new(values.Number, values.Text, values.Enabled);
}
