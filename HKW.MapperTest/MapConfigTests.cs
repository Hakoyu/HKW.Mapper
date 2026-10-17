using HKW.Mapper;

namespace HKW.MapperTest;

[TestClass]
public sealed class MapConfigTests
{
    [TestMethod]
    public void MapTargetConfigArgumentWiresSynchronousActionsAndProtectedEndpoints()
    {
        const string expectedTrace = "start;before;replace;after;end;";
        var source = new MapConfigSyncSource { Number = 7 };
        var target = new MapConfigSyncTarget();

        source.MapTo(target);

        Assert.AreEqual(107, target.Number);
        Assert.AreEqual(expectedTrace, source.Trace);
        Assert.AreEqual(expectedTrace, target.Trace);

        source.Trace = string.Empty;
        target.Trace = string.Empty;
        target.Number = 142;
        source.MapFrom(target);

        Assert.AreEqual(42, source.Number);
        Assert.AreEqual(expectedTrace, source.Trace);
        Assert.AreEqual(expectedTrace, target.Trace);
    }

    [TestMethod]
    public async Task MapTargetConfigArgumentWiresAsynchronousActionsAndProtectedEndpoints()
    {
        const string expectedTrace = "start;before;replace;after;end;";
        var source = new MapConfigAsyncSource { Number = 8 };
        var target = new MapConfigAsyncTarget();

        await source.MapToAsync(target);

        Assert.AreEqual(208, target.Number);
        Assert.AreEqual(expectedTrace, source.Trace);
        Assert.AreEqual(expectedTrace, target.Trace);

        source.Trace = string.Empty;
        target.Trace = string.Empty;
        target.Number = 233;
        await source.MapFromAsync(target);

        Assert.AreEqual(33, source.Number);
        Assert.AreEqual(expectedTrace, source.Trace);
        Assert.AreEqual(expectedTrace, target.Trace);
    }

    [TestMethod]
    public void ConfigConverterReceivesEndpointsAndRedirectsTheTargetProperty()
    {
        var source = new MapConfigRedirectSource { Number = 4 };
        var target = new MapConfigRedirectTarget { Number = -1 };

        source.MapTo(target);

        Assert.AreEqual(-1, target.Number);
        Assert.AreEqual("configured:4", target.Text);

        target.Text = "configured:9";
        source.MapFrom(target);

        Assert.AreEqual(9, source.Number);
    }

    [TestMethod]
    public void ConfigConverterTakesPriorityOverPropertyAttribute()
    {
        var source = new MapConfigPrioritySource { Value = 3 };
        var target = new MapConfigPriorityTarget();

        source.MapTo(target);

        Assert.AreEqual(13, target.Value);
        target.Value = 25;
        source.MapFrom(target);
        Assert.AreEqual(15, source.Value);
    }

    [TestMethod]
    public void ConfigAppliesConvertersAndCombinesIgnoresForInheritedSourceProperties()
    {
        var source = new MapConfigInheritedSource
        {
            Number = 12,
            Included = 1,
            IgnoredByAttribute = 2,
            IgnoredByConfig = 3,
        };
        var target = new MapConfigInheritedTarget
        {
            Included = 10,
            IgnoredByAttribute = 20,
            IgnoredByConfig = 30,
        };

        source.MapTo(target);

        Assert.AreEqual("12", target.Number);
        Assert.AreEqual(1, target.Included);
        Assert.AreEqual(20, target.IgnoredByAttribute);
        Assert.AreEqual(30, target.IgnoredByConfig);

        target.Number = "34";
        target.Included = 40;
        target.IgnoredByAttribute = 50;
        target.IgnoredByConfig = 60;
        source.MapFrom(target);

        Assert.AreEqual(34, source.Number);
        Assert.AreEqual(40, source.Included);
        Assert.AreEqual(2, source.IgnoredByAttribute);
        Assert.AreEqual(3, source.IgnoredByConfig);
    }

    [TestMethod]
    public void ConfigCompositeConvertersMapEndpointsAndOverrideCompositeAttributes()
    {
        var source = new MapConfigCompositeSource { Plain = new(2, 5), Priority = new(7, 11) };
        var target = new MapConfigCompositeTarget();

        source.MapTo(target);

        Assert.AreEqual(2, target.PlainLow);
        Assert.AreEqual(5, target.PlainHigh);
        Assert.AreEqual(107, target.ConfigLow);
        Assert.AreEqual(211, target.ConfigHigh);
        Assert.AreEqual(0, target.AttributeLow);
        Assert.AreEqual(0, target.AttributeHigh);

        target.PlainLow = 3;
        target.PlainHigh = 6;
        target.ConfigLow = 109;
        target.ConfigHigh = 213;
        source.MapFrom(target);

        Assert.AreEqual(new MapConfigRange(3, 6), source.Plain);
        Assert.AreEqual(new MapConfigRange(9, 13), source.Priority);
    }
}

[MapTarget(typeof(MapConfigSyncTarget), typeof(MapConfigSyncConfiguration))]
public sealed class MapConfigSyncSource
{
    public int Number { get; set; }

    [MapIgnoreProperty]
    public string Trace { get; set; } = string.Empty;
}

public sealed class MapConfigSyncTarget
{
    public int Number { get; set; }
    public string Trace { get; set; } = string.Empty;
}

public sealed class MapConfigSyncConfiguration
    : MapperConfig<MapConfigSyncSource, MapConfigSyncTarget>
{
    public MapConfigSyncConfiguration(MapConfigSyncSource source, MapConfigSyncTarget target)
        : base(source, target) { }

    [MapToConfigAction(MapConfigActionMode.Start)]
    public void ToStart() => Record("start;");

    [MapToConfigAction(MapConfigActionMode.BeforeProperty, nameof(MapConfigSyncSource.Number))]
    public void ToBefore() => Record("before;");

    [MapToConfigAction(MapConfigActionMode.ReplaceProperty, nameof(MapConfigSyncSource.Number))]
    public void ToReplace()
    {
        Record("replace;");
        Target.Number = Source.Number + 100;
    }

    [MapToConfigAction(MapConfigActionMode.AfterProperty, nameof(MapConfigSyncSource.Number))]
    public void ToAfter() => Record("after;");

    [MapToConfigAction(MapConfigActionMode.End)]
    public void ToEnd() => Record("end;");

    [MapFromConfigAction(MapConfigActionMode.Start)]
    public void FromStart() => Record("start;");

    [MapFromConfigAction(MapConfigActionMode.BeforeProperty, nameof(MapConfigSyncSource.Number))]
    public void FromBefore() => Record("before;");

    [MapFromConfigAction(MapConfigActionMode.ReplaceProperty, nameof(MapConfigSyncSource.Number))]
    public void FromReplace()
    {
        Record("replace;");
        Source.Number = Target.Number - 100;
    }

    [MapFromConfigAction(MapConfigActionMode.AfterProperty, nameof(MapConfigSyncSource.Number))]
    public void FromAfter() => Record("after;");

    [MapFromConfigAction(MapConfigActionMode.End)]
    public void FromEnd() => Record("end;");

    private void Record(string value)
    {
        Source.Trace += value;
        Target.Trace += value;
    }
}

[MapTarget(typeof(MapConfigAsyncTarget), typeof(MapConfigAsyncConfiguration))]
public sealed class MapConfigAsyncSource
{
    public int Number { get; set; }

    [MapIgnoreProperty]
    public string Trace { get; set; } = string.Empty;
}

public sealed class MapConfigAsyncTarget
{
    public int Number { get; set; }
    public string Trace { get; set; } = string.Empty;
}

public sealed class MapConfigAsyncConfiguration
    : MapperConfig<MapConfigAsyncSource, MapConfigAsyncTarget>
{
    public MapConfigAsyncConfiguration(MapConfigAsyncSource source, MapConfigAsyncTarget target)
        : base(source, target) { }

    [MapToConfigAction(MapConfigActionMode.Start)]
    public Task ToStart() => RecordAsync("start;");

    [MapToConfigAction(MapConfigActionMode.BeforeProperty, nameof(MapConfigAsyncSource.Number))]
    public Task ToBefore() => RecordAsync("before;");

    [MapToConfigAction(MapConfigActionMode.ReplaceProperty, nameof(MapConfigAsyncSource.Number))]
    public async Task ToReplace()
    {
        await RecordAsync("replace;");
        Target.Number = Source.Number + 200;
    }

    [MapToConfigAction(MapConfigActionMode.AfterProperty, nameof(MapConfigAsyncSource.Number))]
    public Task ToAfter() => RecordAsync("after;");

    [MapToConfigAction(MapConfigActionMode.End)]
    public Task ToEnd() => RecordAsync("end;");

    [MapFromConfigAction(MapConfigActionMode.Start)]
    public Task FromStart() => RecordAsync("start;");

    [MapFromConfigAction(MapConfigActionMode.BeforeProperty, nameof(MapConfigAsyncSource.Number))]
    public Task FromBefore() => RecordAsync("before;");

    [MapFromConfigAction(MapConfigActionMode.ReplaceProperty, nameof(MapConfigAsyncSource.Number))]
    public async Task FromReplace()
    {
        await RecordAsync("replace;");
        Source.Number = Target.Number - 200;
    }

    [MapFromConfigAction(MapConfigActionMode.AfterProperty, nameof(MapConfigAsyncSource.Number))]
    public Task FromAfter() => RecordAsync("after;");

    [MapFromConfigAction(MapConfigActionMode.End)]
    public Task FromEnd() => RecordAsync("end;");

    private async Task RecordAsync(string value)
    {
        await Task.Yield();
        Source.Trace += value;
        Target.Trace += value;
    }
}

[MapTarget(typeof(MapConfigRedirectTarget), typeof(MapConfigRedirectConfiguration))]
public sealed class MapConfigRedirectSource
{
    [MapProperty(typeof(MapConfigRedirectTarget), nameof(MapConfigRedirectTarget.Text))]
    public int Number { get; set; }
}

public sealed class MapConfigRedirectTarget
{
    public int Number { get; set; }
    public string Text { get; set; } = string.Empty;
}

public sealed class MapConfigRedirectConfiguration
    : MapperConfig<MapConfigRedirectSource, MapConfigRedirectTarget>
{
    public MapConfigRedirectConfiguration(
        MapConfigRedirectSource source,
        MapConfigRedirectTarget target
    )
        : base(source, target) { }

    public MapConfigPropertyConverter<int, string> NumberConverter { get; } =
        new(
            nameof(MapConfigRedirectSource.Number),
            nameof(MapConfigRedirectTarget.Text),
            (value, args) =>
            {
                Assert.IsInstanceOfType<MapConfigRedirectSource>(args.Source);
                Assert.IsInstanceOfType<MapConfigRedirectTarget>(args.Target);
                Assert.AreEqual(nameof(MapConfigRedirectSource.Number), args.SourcePropertyName);
                Assert.AreEqual(nameof(MapConfigRedirectTarget.Text), args.TargetPropertyName);
                return $"configured:{value}";
            },
            (value, args) =>
            {
                Assert.IsInstanceOfType<MapConfigRedirectSource>(args.Source);
                Assert.IsInstanceOfType<MapConfigRedirectTarget>(args.Target);
                Assert.AreEqual(nameof(MapConfigRedirectSource.Number), args.SourcePropertyName);
                Assert.AreEqual(nameof(MapConfigRedirectTarget.Text), args.TargetPropertyName);
                return int.Parse(value[11..]);
            }
        );
}

#pragma warning disable MAP0027
[MapTarget(typeof(MapConfigPriorityTarget), typeof(MapConfigPriorityConfiguration))]
public sealed class MapConfigPrioritySource
{
    [MapProperty(typeof(MapConfigPriorityTarget), typeof(MapConfigAttributePriorityConverter))]
    public int Value { get; set; }
}

public sealed class MapConfigPriorityTarget
{
    public int Value { get; set; }
}

public sealed class MapConfigAttributePriorityConverter : IMapConverter<int, int>
{
    public int Convert(int value, MapConvertArgs args) => value + 1000;

    public int ConvertBack(int value, MapConvertArgs args) => value - 1000;
}

public sealed class MapConfigPriorityConfiguration
    : MapperConfig<MapConfigPrioritySource, MapConfigPriorityTarget>
{
    public MapConfigPriorityConfiguration(
        MapConfigPrioritySource source,
        MapConfigPriorityTarget target
    )
        : base(source, target) { }

    public MapConfigPropertyConverter<int, int> ValueConverter { get; } =
        new(
            nameof(MapConfigPrioritySource.Value),
            (value, _) => value + 10,
            (value, _) => value - 10
        );
}

public class MapConfigInheritedBase
{
    public int Number { get; set; }
    public int Included { get; set; }
    public int IgnoredByAttribute { get; set; }
    public int IgnoredByConfig { get; set; }
}

[MapTarget(
    typeof(MapConfigInheritedTarget),
    typeof(MapConfigInheritedConfiguration),
    MapParentProperties = true,
    IgnoreProperties = new[] { nameof(MapConfigInheritedSource.IgnoredByAttribute) }
)]
public sealed class MapConfigInheritedSource : MapConfigInheritedBase;

public sealed class MapConfigInheritedTarget
{
    public string Number { get; set; } = string.Empty;
    public int Included { get; set; }
    public int IgnoredByAttribute { get; set; }
    public int IgnoredByConfig { get; set; }
}

public sealed class MapConfigInheritedConfiguration
    : MapperConfig<MapConfigInheritedSource, MapConfigInheritedTarget>
{
    public MapConfigInheritedConfiguration(
        MapConfigInheritedSource source,
        MapConfigInheritedTarget target
    )
        : base(source, target) { }

    public string[] IgnoreProperties { get; } = [nameof(MapConfigInheritedSource.IgnoredByConfig)];

    public MapConfigPropertyConverter<int, string> NumberConverter { get; } =
        new(
            nameof(MapConfigInheritedSource.Number),
            (value, _) => value.ToString(),
            (value, _) => int.Parse(value)
        );
}

public readonly record struct MapConfigRange(int Low, int High);

[MapTarget(typeof(MapConfigCompositeTarget), typeof(MapConfigCompositeConfiguration))]
public sealed class MapConfigCompositeSource
{
    public MapConfigRange Plain { get; set; }

    [MapCompositeProperty(
        typeof(MapConfigCompositeTarget),
        typeof(MapConfigAttributeCompositeConverter),
        nameof(MapConfigCompositeTarget.AttributeLow),
        nameof(MapConfigCompositeTarget.AttributeHigh)
    )]
    public MapConfigRange Priority { get; set; }
}

public sealed class MapConfigCompositeTarget
{
    public int PlainLow { get; set; }
    public int PlainHigh { get; set; }
    public int ConfigLow { get; set; }
    public int ConfigHigh { get; set; }
    public int AttributeLow { get; set; }
    public int AttributeHigh { get; set; }
}

public sealed class MapConfigAttributeCompositeConverter
    : IMapCompositeConverter<MapConfigRange, (int Low, int High)>
{
    public (int Low, int High) Convert(MapConfigRange value, MapCompositeConvertArgs args) =>
        (value.Low + 1000, value.High + 1000);

    public MapConfigRange ConvertBack((int Low, int High) values, MapCompositeConvertArgs args) =>
        new(values.Low - 1000, values.High - 1000);
}

public sealed class MapConfigCompositeConfiguration
    : MapperConfig<MapConfigCompositeSource, MapConfigCompositeTarget>
{
    public MapConfigCompositeConfiguration(
        MapConfigCompositeSource source,
        MapConfigCompositeTarget target
    )
        : base(source, target) { }

    public MapConfigCompositePropertyConverter<
        MapConfigRange,
        (int Low, int High)
    > PlainConverter { get; } =
        new(
            nameof(MapConfigCompositeSource.Plain),
            [nameof(MapConfigCompositeTarget.PlainLow), nameof(MapConfigCompositeTarget.PlainHigh)],
            (value, _) => (value.Low, value.High),
            (values, _) => new(values.Low, values.High)
        );

    public MapConfigCompositePropertyConverter<
        MapConfigRange,
        (int Low, int High)
    > PriorityConverter { get; } =
        new(
            nameof(MapConfigCompositeSource.Priority),
            [
                nameof(MapConfigCompositeTarget.ConfigLow),
                nameof(MapConfigCompositeTarget.ConfigHigh),
            ],
            (value, args) =>
            {
                Assert.IsInstanceOfType<MapConfigCompositeSource>(args.Source);
                Assert.IsInstanceOfType<MapConfigCompositeTarget>(args.Target);
                Assert.AreEqual(nameof(MapConfigCompositeSource.Priority), args.SourcePropertyName);
                CollectionAssert.AreEqual(
                    new[]
                    {
                        nameof(MapConfigCompositeTarget.ConfigLow),
                        nameof(MapConfigCompositeTarget.ConfigHigh),
                    },
                    args.TargetPropertyNames
                );
                return (value.Low + 100, value.High + 200);
            },
            (values, args) =>
            {
                Assert.IsInstanceOfType<MapConfigCompositeSource>(args.Source);
                Assert.IsInstanceOfType<MapConfigCompositeTarget>(args.Target);
                Assert.AreEqual(nameof(MapConfigCompositeSource.Priority), args.SourcePropertyName);
                CollectionAssert.AreEqual(
                    new[]
                    {
                        nameof(MapConfigCompositeTarget.ConfigLow),
                        nameof(MapConfigCompositeTarget.ConfigHigh),
                    },
                    args.TargetPropertyNames
                );
                return new(values.Low - 100, values.High - 200);
            }
        );
}
#pragma warning restore MAP0027
