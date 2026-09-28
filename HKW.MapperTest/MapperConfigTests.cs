using HKW.HKWMapper;

namespace HKW.MapperTest;

[TestClass]
public sealed class MapperConfigTests
{
    [TestMethod]
    public void ActionsAndConverterAreUsedByGeneratedMapping()
    {
        const string actionLog = "start;before;replace;after;end";
        var source = new ConfigSource();
        var target = new ConfigTarget();

        source.MapTo(target);

        Assert.AreEqual(actionLog, source.ActionLog);

        source.ActionLog = string.Empty;
        source.MapFrom(target);

        Assert.AreEqual(actionLog, source.ActionLog);
    }

    [TestMethod]
    public async Task ActionsAndConverterAreUsedByGeneratedMappingAsync()
    {
        const string actionLog = "start;before;replace;after;end";
        var source = new ConfigSourceAsync();
        var target = new ConfigTarget();

        await source.MapToAsync(target);

        Assert.AreEqual(actionLog, source.ActionLog);

        source.ActionLog = string.Empty;
        await source.MapFromAsync(target);

        Assert.AreEqual(actionLog, source.ActionLog);
    }

    [TestMethod]
    public void TakesPriorityOverPropertyAttributeConverter()
    {
        var source = new PrioritySource { Value = 2 };
        var target = new PriorityTarget();

        source.MapTo(target);

        Assert.AreEqual(12, target.Value);
        target.Value = 20;
        source.MapFrom(target);
        Assert.AreEqual(10, source.Value);
    }
}

[MapTarget(typeof(ConfigTarget), typeof(TestMapperConfig))]
public sealed class ConfigSource
{
    public int Number { get; set; }

    [MapIgnoreProperty]
    public string ActionLog { get; set; } = string.Empty;
}

[MapTarget(typeof(ConfigTarget), typeof(TestMapperAsyncConfig))]
public sealed class ConfigSourceAsync
{
    public int Number { get; set; }

    [MapIgnoreProperty]
    public string ActionLog { get; set; } = string.Empty;
}

public sealed class ConfigTarget
{
    public int Number { get; set; }
}

public sealed class TestMapperConfig : MapperConfig<ConfigSource, ConfigTarget>
{
    public TestMapperConfig(ConfigSource source, ConfigTarget target)
        : base(source, target) { }

    public MapConfigPropertyConverter<int, int> NumberConverter { get; } =
        new(nameof(ConfigSource.Number), (_, value) => value + 10, (_, value) => value - 10);

    [MapToConfigAction(MapConfigActionMode.Start)]
    [MapFromConfigAction(MapConfigActionMode.Start)]
    public void Start() => Source.ActionLog += "start;";

    [MapToConfigAction(MapConfigActionMode.BeforeProperty, nameof(ConfigSource.Number))]
    [MapFromConfigAction(MapConfigActionMode.BeforeProperty, nameof(ConfigSource.Number))]
    public void BeforeNumber() => Source.ActionLog += "before;";

    [MapToConfigAction(MapConfigActionMode.ReplaceProperty, nameof(ConfigSource.Number))]
    [MapFromConfigAction(MapConfigActionMode.ReplaceProperty, nameof(ConfigSource.Number))]
    public void ReplaceNumber() => Source.ActionLog += "replace;";

    [MapToConfigAction(MapConfigActionMode.AfterProperty, nameof(ConfigSource.Number))]
    [MapFromConfigAction(MapConfigActionMode.AfterProperty, nameof(ConfigSource.Number))]
    public void AfterNumber() => Source.ActionLog += "after;";

    [MapToConfigAction(MapConfigActionMode.End)]
    [MapFromConfigAction(MapConfigActionMode.End)]
    public void End() => Source.ActionLog += "end";
}

public sealed class TestMapperAsyncConfig : MapperConfig<ConfigSourceAsync, ConfigTarget>
{
    public TestMapperAsyncConfig(ConfigSourceAsync source, ConfigTarget target)
        : base(source, target) { }

    public MapConfigPropertyConverter<int, int> NumberConverter { get; } =
        new(nameof(ConfigSourceAsync.Number), (_, value) => value + 10, (_, value) => value - 10);

    [MapToConfigAction(MapConfigActionMode.Start)]
    [MapFromConfigAction(MapConfigActionMode.Start)]
    public async Task Start()
    {
        await Task.Delay(1);
        Source.ActionLog += "start;";
    }

    [MapToConfigAction(MapConfigActionMode.BeforeProperty, nameof(ConfigSourceAsync.Number))]
    [MapFromConfigAction(MapConfigActionMode.BeforeProperty, nameof(ConfigSourceAsync.Number))]
    public async Task BeforeNumber()
    {
        await Task.Delay(1);
        Source.ActionLog += "before;";
    }

    [MapToConfigAction(MapConfigActionMode.ReplaceProperty, nameof(ConfigSourceAsync.Number))]
    [MapFromConfigAction(MapConfigActionMode.ReplaceProperty, nameof(ConfigSourceAsync.Number))]
    public async Task ReplaceNumber()
    {
        await Task.Delay(1);
        Source.ActionLog += "replace;";
    }

    [MapToConfigAction(MapConfigActionMode.AfterProperty, nameof(ConfigSourceAsync.Number))]
    [MapFromConfigAction(MapConfigActionMode.AfterProperty, nameof(ConfigSourceAsync.Number))]
    public async Task AfterNumber()
    {
        await Task.Delay(1);
        Source.ActionLog += "after;";
    }

    [MapToConfigAction(MapConfigActionMode.End)]
    [MapFromConfigAction(MapConfigActionMode.End)]
    public async Task End()
    {
        await Task.Delay(1);
        Source.ActionLog += "end";
    }
}

[MapTarget(typeof(PriorityTarget), typeof(PriorityConfig))]
public sealed class PrioritySource
{
    [MapProperty(typeof(PriorityTarget), typeof(AttributePriorityConverter))]
    public int Value { get; set; }
}

public sealed class PriorityTarget
{
    public int Value { get; set; }
}

public sealed class AttributePriorityConverter : IMapConverter<int, int>
{
    public int Convert(object source, int value) => value + 1000;

    public int ConvertBack(object target, int value) => value - 1000;
}

public sealed class PriorityConfig : MapperConfig<PrioritySource, PriorityTarget>
{
    public PriorityConfig(PrioritySource source, PriorityTarget target)
        : base(source, target) { }

    public MapConfigPropertyConverter<int, int> ValueConverter { get; } =
        new(nameof(PrioritySource.Value), (_, value) => value + 10, (_, value) => value - 10);
}
