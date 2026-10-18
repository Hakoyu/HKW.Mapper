using HKW.Mapper;

namespace HKW.MapperTest;

[TestClass]
public sealed class MapSourceTests
{
    [TestMethod]
    public void GeneratesExtensionsInAnnotatedContainer()
    {
        var source = new ExternalMapSource { Value = 7 };
        var target = new ExternalMapTarget();

        var returned = ExternalMappings.MapTo(source, target);

        Assert.AreSame(target, returned);
        Assert.AreEqual(7, target.Value);

        var created = source.MapTo((ExternalMapTarget?)null);
        Assert.AreEqual(7, created.Value);

        target.Value = 11;
        source.MapFrom(target);
        Assert.AreEqual(11, source.Value);
    }

    [TestMethod]
    public void SupportsMapTargetOptionsAndSourceMemberMetadata()
    {
        var source = new ConfiguredExternalSource { Value = 3, Ignored = 4, Field = 5 };
        var target = new ConfiguredExternalTarget { Ignored = 40 };

        source.MapToConfigured(target);

        Assert.AreEqual(3, target.Renamed);
        Assert.AreEqual(40, target.Ignored);
        Assert.AreEqual(5, target.Field);
    }

    [TestMethod]
    public void SupportsSourceTypesFromReferencedAssemblies()
    {
        var source = new KeyValuePair<string, int>("answer", 42);
        var target = new ExternalKeyValueTarget();

        source.MapTo(target);

        Assert.AreEqual("answer", target.Key);
        Assert.AreEqual(42, target.Value);
    }

    [TestMethod]
    public void SupportsMapperConfig()
    {
        var source = new ConfigExternalSource { Value = 17 };
        var target = new ConfigExternalTarget();

        source.MapTo(target);

        Assert.AreEqual(17, target.Value);
    }

    [TestMethod]
    public void UsesMapSourceMappingsForNestedCollections()
    {
        var source = new ExternalCollectionSource
        {
            Items = [new ExternalCollectionItemSource { Value = 13 }],
        };
        var target = new ExternalCollectionTarget();

        source.MapTo(target);

        Assert.AreEqual(13, target.Items.Single().Value);
    }
}

[MapSource(typeof(ExternalMapSource), typeof(ExternalMapTarget))]
[MapSource(
    typeof(ConfiguredExternalSource),
    typeof(ConfiguredExternalTarget),
    TargetName = "Configured",
    Direction = MapDirections.To,
    MapFields = true,
    IgnoreProperties = [nameof(ConfiguredExternalSource.Ignored)]
)]
[MapSource(typeof(KeyValuePair<string, int>), typeof(ExternalKeyValueTarget), Direction = MapDirections.To)]
[MapSource(typeof(ConfigExternalSource), typeof(ConfigExternalTarget), typeof(ConfigExternalMapperConfig))]
[MapSource(typeof(ExternalCollectionItemSource), typeof(ExternalCollectionItemTarget))]
[MapSource(typeof(ExternalCollectionSource), typeof(ExternalCollectionTarget), Direction = MapDirections.To)]
public static partial class ExternalMappings;

public sealed class ExternalMapSource
{
    public int Value { get; set; }
}

public sealed class ExternalMapTarget
{
    public int Value { get; set; }
}

public sealed class ConfiguredExternalSource
{
    [MapProperty("Configured", nameof(ConfiguredExternalTarget.Renamed))]
    public int Value { get; set; }

    public int Ignored { get; set; }

    public int Field;
}

public sealed class ConfiguredExternalTarget
{
    public int Renamed { get; set; }

    public int Ignored { get; set; }

    public int Field;
}

public sealed class ExternalKeyValueTarget
{
    public string Key { get; set; } = string.Empty;

    public int Value { get; set; }
}

public sealed class ConfigExternalSource
{
    public int Value { get; set; }
}

public sealed class ConfigExternalTarget
{
    public int Value { get; set; }
}

public sealed class ConfigExternalMapperConfig
    : MapperConfig<ConfigExternalSource, ConfigExternalTarget>
{
    public ConfigExternalMapperConfig(ConfigExternalSource source, ConfigExternalTarget target)
        : base(source, target) { }
}

public sealed class ExternalCollectionSource
{
    public List<ExternalCollectionItemSource> Items { get; set; } = [];
}

public sealed class ExternalCollectionTarget
{
    public List<ExternalCollectionItemTarget> Items { get; set; } = [];
}

public sealed class ExternalCollectionItemSource
{
    public int Value { get; set; }
}

public sealed class ExternalCollectionItemTarget
{
    public int Value { get; set; }
}
