using HKW.Mapper;

namespace HKW.MapperTest;

[TestClass]
public sealed class InheritedPropertyMappingTests
{
    [TestMethod]
    public void MapsPropertiesAcrossTheInheritanceChain()
    {
        var source = new InheritedSource { Own = 1, Parent = 2, Grandparent = 3 };
        var target = new InheritedTarget();

        source.MapTo(target);

        Assert.AreEqual(1, target.Own);
        Assert.AreEqual(2, target.Parent);
        Assert.AreEqual(3, target.Grandparent);

        target.Own = 4;
        target.Parent = 5;
        target.Grandparent = 6;
        source.MapFrom(target);

        Assert.AreEqual(4, source.Own);
        Assert.AreEqual(5, source.Parent);
        Assert.AreEqual(6, source.Grandparent);
    }

    [TestMethod]
    public void PrefersAPropertyDeclaredByTheDerivedSource()
    {
        var source = new HiddenPropertySource { Value = 7 };
        ((HiddenPropertyBase)source).Value = 2;
        var target = new HiddenPropertyTarget();

        source.MapTo(target);

        Assert.AreEqual(7, target.Value);
        target.Value = 9;
        source.MapFrom(target);
        Assert.AreEqual(9, source.Value);
        Assert.AreEqual(2, ((HiddenPropertyBase)source).Value);
    }

    [TestMethod]
    public void DoesNotMapParentPropertiesByDefault()
    {
        var source = new DefaultParentMappingSource { Own = 1, Parent = 2 };
        var target = new DefaultParentMappingTarget { Own = 10, Parent = 20 };

        source.MapTo(target);

        Assert.AreEqual(1, target.Own);
        Assert.AreEqual(20, target.Parent);
    }

    [TestMethod]
    public void CombinesAttributeAndConfigIgnoreProperties()
    {
        var source = new IgnoredInheritedSource
        {
            Included = 1,
            IgnoredByAttribute = 2,
            IgnoredByConfig = 3,
        };
        var target = new IgnoredInheritedTarget
        {
            Included = 10,
            IgnoredByAttribute = 20,
            IgnoredByConfig = 30,
        };

        source.MapTo(target);

        Assert.AreEqual(1, target.Included);
        Assert.AreEqual(20, target.IgnoredByAttribute);
        Assert.AreEqual(30, target.IgnoredByConfig);
    }

    [TestMethod]
    public void UsesConfigConverterForAnInheritedProperty()
    {
        var source = new ConvertedInheritedSource { Number = 12 };
        var target = new ConvertedInheritedTarget();

        source.MapTo(target);
        Assert.AreEqual("12", target.Number);

        target.Number = "34";
        source.MapFrom(target);
        Assert.AreEqual(34, source.Number);
    }
}

public class InheritedGrandparentSource
{
    public int Grandparent { get; set; }
}

public class InheritedParentSource : InheritedGrandparentSource
{
    public int Parent { get; set; }
}

[MapTarget(typeof(InheritedTarget), MapParentProperties = true)]
public sealed class InheritedSource : InheritedParentSource
{
    public int Own { get; set; }
}

public sealed class InheritedTarget
{
    public int Own { get; set; }
    public int Parent { get; set; }
    public int Grandparent { get; set; }
}

public class HiddenPropertyBase
{
    public int Value { get; set; }
}

[MapTarget(typeof(HiddenPropertyTarget), MapParentProperties = true)]
public sealed class HiddenPropertySource : HiddenPropertyBase
{
    public new int Value { get; set; }
}

public sealed class HiddenPropertyTarget
{
    public int Value { get; set; }
}

public class DefaultParentMappingBase
{
    public int Parent { get; set; }
}

[MapTarget(typeof(DefaultParentMappingTarget))]
public sealed class DefaultParentMappingSource : DefaultParentMappingBase
{
    public int Own { get; set; }
}

public sealed class DefaultParentMappingTarget
{
    public int Own { get; set; }
    public int Parent { get; set; }
}

public class IgnoredInheritedBase
{
    public int Included { get; set; }
    public int IgnoredByAttribute { get; set; }
    public int IgnoredByConfig { get; set; }
}

[MapTarget(
    typeof(IgnoredInheritedTarget),
    typeof(IgnoredInheritedConfig),
    MapParentProperties = true,
    IgnoreProperties = new[] { nameof(IgnoredByAttribute) }
)]
public sealed class IgnoredInheritedSource : IgnoredInheritedBase;

public sealed class IgnoredInheritedTarget
{
    public int Included { get; set; }
    public int IgnoredByAttribute { get; set; }
    public int IgnoredByConfig { get; set; }
}

public sealed class IgnoredInheritedConfig
    : MapperConfig<IgnoredInheritedSource, IgnoredInheritedTarget>
{
    public IgnoredInheritedConfig(IgnoredInheritedSource source, IgnoredInheritedTarget target)
        : base(source, target) { }

    public string[] IgnoreProperties { get; } = [nameof(IgnoredInheritedSource.IgnoredByConfig)];
}

public class ConvertedInheritedBase
{
    public int Number { get; set; }
}

[MapTarget(
    typeof(ConvertedInheritedTarget),
    typeof(ConvertedInheritedConfig),
    MapParentProperties = true
)]
public sealed class ConvertedInheritedSource : ConvertedInheritedBase;

public sealed class ConvertedInheritedTarget
{
    public string Number { get; set; } = string.Empty;
}

public sealed class ConvertedInheritedConfig
    : MapperConfig<ConvertedInheritedSource, ConvertedInheritedTarget>
{
    public ConvertedInheritedConfig(ConvertedInheritedSource source, ConvertedInheritedTarget target)
        : base(source, target) { }

    public MapConfigPropertyConverter<int, string> NumberConverter { get; } =
        new(
            nameof(ConvertedInheritedSource.Number),
            (_, value) => value.ToString(),
            (_, value) => int.Parse(value)
        );
}
