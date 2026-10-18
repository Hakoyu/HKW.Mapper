using HKW.Mapper;

namespace HKW.MapperTest;

[TestClass]
public sealed class IMapConverterTests
{
    [TestMethod]
    public void AttributeConverterIsInvokedInBothDirectionsWithExpectedArguments()
    {
        IMapConverterTestTrackingConverter.Reset();
        var source = new IMapConverterTestTrackingSource { Number = 4 };
        var target = new IMapConverterTestTrackingTarget();

        source.MapTo(target);

        Assert.AreEqual("number:4", target.Text);
        Assert.AreEqual(1, IMapConverterTestTrackingConverter.ConvertCalls);
        AssertConvertArgs(
            IMapConverterTestTrackingConverter.LastArgs,
            source,
            target,
            "Number",
            "Text"
        );

        target.Text = "number:9";
        source.MapFrom(target);

        Assert.AreEqual(9, source.Number);
        Assert.AreEqual(1, IMapConverterTestTrackingConverter.ConvertBackCalls);
        AssertConvertArgs(
            IMapConverterTestTrackingConverter.LastArgs,
            source,
            target,
            "Number",
            "Text"
        );
    }

    [TestMethod]
    public void AttributeConverterSupportsInterfaceTypesInBothDirections()
    {
        var source = new IMapConverterTestInterfaceSource { Values = [1, 2] };
        var target = new IMapConverterTestInterfaceTarget();

        source.MapTo(target);
        Assert.AreEqual("1,2", target.Values);

        target.Values = "3,4";
        source.MapFrom(target);
        CollectionAssert.AreEqual(new[] { 3, 4 }, source.Values);
    }

    [TestMethod]
    public void ConfigConverterSupportsInterfaceGenericTypesForConcreteProperties()
    {
        var source = new IMapConverterTestConfigInterfaceSource { Values = [5, 6] };
        var target = new IMapConverterTestConfigInterfaceTarget();

        source.MapTo(target);
        CollectionAssert.AreEqual(new[] { "5", "6" }, target.Values);

        target.Values = ["7", "8"];
        source.MapFrom(target);
        CollectionAssert.AreEqual(new[] { 7, 8 }, source.Values);
    }

    [TestMethod]
    public void ConverterSupportsConcreteSubtypesForInterfaceProperties()
    {
        var source = new IMapConverterTestSubtypeSource
        {
            Values = new List<int> { 5, 6 },
        };
        var target = new IMapConverterTestSubtypeTarget();

        source.MapTo(target);
        CollectionAssert.AreEqual(new[] { "5", "6" }, target.Values.ToList());

        target.Values = new List<string> { "7", "8" };
        source.MapFrom(target);
        CollectionAssert.AreEqual(new[] { 7, 8 }, source.Values.ToList());
    }

    [TestMethod]
    public void ExplicitSubtypeConversionRejectsAnIncompatibleRuntimeType()
    {
        var source = new IMapConverterTestSubtypeSource { Values = new[] { 1, 2 } };

        Assert.Throws<InvalidCastException>(() =>
            source.MapTo(new IMapConverterTestSubtypeTarget())
        );
    }

    [TestMethod]
    public void ConverterSupportsBoxingToEnumBaseTypeInBothDirections()
    {
        var source = new IMapConverterTestBoxingSource
        {
            Mode = new IMapConverterTestEnumWrapper<IMapConverterTestMode>(
                IMapConverterTestMode.First
            ),
        };
        var target = new IMapConverterTestBoxingTarget();

        source.MapTo(target);
        Assert.AreEqual(IMapConverterTestMode.First, target.Mode);

        target.Mode = IMapConverterTestMode.Second;
        source.MapFrom(target);
        Assert.AreEqual(IMapConverterTestMode.Second, source.Mode.Value);
    }

    [TestMethod]
    public void ConfigConverterHandlesInheritedPropertyAndTakesPriorityOverAttributeConverter()
    {
        var source = new IMapConverterTestConfiguredSource { Number = 12 };
        var target = new IMapConverterTestConfiguredTarget();

        source.MapTo(target);
        Assert.AreEqual("config:12", target.Text);

        target.Text = "config:34";
        source.MapFrom(target);
        Assert.AreEqual(34, source.Number);
    }

    [TestMethod]
    public void MapConfigMemberConverterConstructorsDelegateBothDirections()
    {
        var source = new object();
        var target = new object();
        var sameNameArgs = new MapConvertArgs(source, target, "Value", "Value");
        var redirectedArgs = new MapConvertArgs(source, target, "Number", "Text");
        MapConvertArgs observedSameNameArgs = default;
        MapConvertArgs observedRedirectedArgs = default;

        var sameNameConverter = new MapConfigMemberConverter<int, string>(
            "Value",
            (value, args) =>
            {
                observedSameNameArgs = args;
                return $"same:{value}";
            },
            (value, args) =>
            {
                observedSameNameArgs = args;
                return int.Parse(value[5..]);
            }
        );
        var redirectedConverter = new MapConfigMemberConverter<int, string>(
            "Number",
            "Text",
            (value, args) =>
            {
                observedRedirectedArgs = args;
                return $"redirected:{value}";
            },
            (value, args) =>
            {
                observedRedirectedArgs = args;
                return int.Parse(value[11..]);
            }
        );

        Assert.AreEqual("same:5", sameNameConverter.Convert(5, sameNameArgs));
        AssertConvertArgs(observedSameNameArgs, source, target, "Value", "Value");
        Assert.AreEqual(6, sameNameConverter.ConvertBack("same:6", sameNameArgs));
        AssertConvertArgs(observedSameNameArgs, source, target, "Value", "Value");
        Assert.AreEqual("redirected:7", redirectedConverter.Convert(7, redirectedArgs));
        AssertConvertArgs(observedRedirectedArgs, source, target, "Number", "Text");
        Assert.AreEqual(8, redirectedConverter.ConvertBack("redirected:8", redirectedArgs));
        AssertConvertArgs(observedRedirectedArgs, source, target, "Number", "Text");
    }

    [TestMethod]
    public void MapConfigMemberConverterRejectsEmptyMemberNames()
    {
        Assert.Throws<ArgumentException>(() =>
            new MapConfigMemberConverter<int, string>("", (_, _) => string.Empty, (_, _) => 0)
        );
        Assert.Throws<ArgumentException>(() =>
            new MapConfigMemberConverter<int, string>("Value", " ", (_, _) => string.Empty, (_, _) => 0)
        );
    }

    [TestMethod]
    public void MapConfigMemberConverterRejectsNullDelegatesForBothConstructors()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new MapConfigMemberConverter<int, string>("Value", null!, (_, _) => 0)
        );
        Assert.Throws<ArgumentNullException>(() =>
            new MapConfigMemberConverter<int, string>("Value", (_, _) => string.Empty, null!)
        );
        Assert.Throws<ArgumentNullException>(() =>
            new MapConfigMemberConverter<int, string>("Number", "Text", null!, (_, _) => 0)
        );
        Assert.Throws<ArgumentNullException>(() =>
            new MapConfigMemberConverter<int, string>(
                "Number",
                "Text",
                (_, _) => string.Empty,
                null!
            )
        );
    }

    private static void AssertConvertArgs(
        MapConvertArgs args,
        object source,
        object target,
        string sourcePropertyName,
        string targetPropertyName
    )
    {
        Assert.AreSame(source, args.Source);
        Assert.AreSame(target, args.Target);
        Assert.AreEqual(sourcePropertyName, args.SourcePropertyName);
        Assert.AreEqual(targetPropertyName, args.TargetPropertyName);
    }
}

public sealed class IMapConverterTestTrackingConverter : IMapConverter<int, string>
{
    public static int ConvertCalls { get; private set; }
    public static int ConvertBackCalls { get; private set; }
    public static MapConvertArgs LastArgs { get; private set; }

    public static void Reset()
    {
        ConvertCalls = 0;
        ConvertBackCalls = 0;
        LastArgs = default;
    }

    public string Convert(int value, MapConvertArgs args)
    {
        ConvertCalls++;
        LastArgs = args;
        return $"number:{value}";
    }

    public int ConvertBack(string value, MapConvertArgs args)
    {
        ConvertBackCalls++;
        LastArgs = args;
        return int.Parse(value[7..]);
    }
}

[MapTarget(typeof(IMapConverterTestTrackingTarget))]
public sealed class IMapConverterTestTrackingSource
{
    [MapProperty(
        typeof(IMapConverterTestTrackingTarget),
        nameof(IMapConverterTestTrackingTarget.Text),
        typeof(IMapConverterTestTrackingConverter)
    )]
    public int Number { get; set; }
}

public sealed class IMapConverterTestTrackingTarget
{
    public string Text { get; set; } = string.Empty;
}

public sealed class IMapConverterTestInterfaceConverter : IMapConverter<IList<int>, string>
{
    public string Convert(IList<int> value, MapConvertArgs args) => string.Join(',', value);

    public IList<int> ConvertBack(string value, MapConvertArgs args) =>
        value.Split(',').Select(int.Parse).ToList();
}

[MapTarget(typeof(IMapConverterTestInterfaceTarget))]
public sealed class IMapConverterTestInterfaceSource
{
    [MapProperty(
        typeof(IMapConverterTestInterfaceTarget),
        typeof(IMapConverterTestInterfaceConverter)
    )]
    public List<int> Values { get; set; } = [];
}

public sealed class IMapConverterTestInterfaceTarget
{
    public string Values { get; set; } = string.Empty;
}

[MapTarget(
    typeof(IMapConverterTestConfigInterfaceTarget),
    typeof(IMapConverterTestInterfaceGenericConfig)
)]
public sealed class IMapConverterTestConfigInterfaceSource
{
    public List<int> Values { get; set; } = [];
}

public sealed class IMapConverterTestConfigInterfaceTarget
{
    public List<string> Values { get; set; } = [];
}

public sealed class IMapConverterTestInterfaceGenericConfig
    : MapperConfigBase<
        IMapConverterTestConfigInterfaceSource,
        IMapConverterTestConfigInterfaceTarget
    >
{
    public IMapConverterTestInterfaceGenericConfig(
        IMapConverterTestConfigInterfaceSource source,
        IMapConverterTestConfigInterfaceTarget target
    )
        : base(source, target) { }

    public MapConfigMemberConverter<IList<int>, IList<string>> ValuesConverter { get; } =
        new(
            nameof(IMapConverterTestConfigInterfaceSource.Values),
            (values, _) => values.Select(value => value.ToString()).ToList(),
            (values, _) => values.Select(int.Parse).ToList()
        );
}

public sealed class IMapConverterTestConcreteListConverter : IMapConverter<List<int>, List<string>>
{
    public List<string> Convert(List<int> value, MapConvertArgs args) =>
        value.Select(x => x.ToString()).ToList();

    public List<int> ConvertBack(List<string> value, MapConvertArgs args) =>
        value.Select(int.Parse).ToList();
}

[MapTarget(typeof(IMapConverterTestSubtypeTarget))]
public sealed class IMapConverterTestSubtypeSource
{
    [MapProperty(
        typeof(IMapConverterTestSubtypeTarget),
        typeof(IMapConverterTestConcreteListConverter)
    )]
    public IList<int> Values { get; set; } = new List<int>();
}

public sealed class IMapConverterTestSubtypeTarget
{
    public IList<string> Values { get; set; } = new List<string>();
}

public interface IIMapConverterTestEnumWrapper
{
    Enum Value { get; }
}

public sealed class IMapConverterTestEnumWrapper<TEnum>(TEnum value) : IIMapConverterTestEnumWrapper
    where TEnum : struct, Enum
{
    public TEnum Value { get; } = value;

    Enum IIMapConverterTestEnumWrapper.Value => Value;
}

public enum IMapConverterTestMode
{
    First,
    Second,
}

public sealed class IMapConverterTestBoxingConverter
    : IMapConverter<IIMapConverterTestEnumWrapper, Enum>
{
    public Enum Convert(IIMapConverterTestEnumWrapper value, MapConvertArgs args) => value.Value;

    public IIMapConverterTestEnumWrapper ConvertBack(Enum value, MapConvertArgs args) =>
        new IMapConverterTestEnumWrapper<IMapConverterTestMode>((IMapConverterTestMode)value);
}

[MapTarget(typeof(IMapConverterTestBoxingTarget))]
public sealed class IMapConverterTestBoxingSource
{
    [MapProperty(typeof(IMapConverterTestBoxingTarget), typeof(IMapConverterTestBoxingConverter))]
    public IMapConverterTestEnumWrapper<IMapConverterTestMode> Mode { get; set; } =
        new(IMapConverterTestMode.First);
}

public sealed class IMapConverterTestBoxingTarget
{
    public IMapConverterTestMode Mode { get; set; }
}

#pragma warning disable MAP0027
public class IMapConverterTestConfiguredBase
{
    [MapProperty(
        typeof(IMapConverterTestConfiguredTarget),
        nameof(IMapConverterTestConfiguredTarget.Text),
        typeof(IMapConverterTestIgnoredAttributeConverter)
    )]
    public int Number { get; set; }
}

[MapTarget(
    typeof(IMapConverterTestConfiguredTarget),
    typeof(IMapConverterTestConfig),
    MapParentProperties = true
)]
public sealed class IMapConverterTestConfiguredSource : IMapConverterTestConfiguredBase;

public sealed class IMapConverterTestConfiguredTarget
{
    public string Text { get; set; } = string.Empty;
}

public sealed class IMapConverterTestIgnoredAttributeConverter : IMapConverter<int, string>
{
    public string Convert(int value, MapConvertArgs args) => $"attribute:{value}";

    public int ConvertBack(string value, MapConvertArgs args) => int.Parse(value[10..]);
}

public sealed class IMapConverterTestConfig
    : MapperConfigBase<IMapConverterTestConfiguredSource, IMapConverterTestConfiguredTarget>
{
    public IMapConverterTestConfig(
        IMapConverterTestConfiguredSource source,
        IMapConverterTestConfiguredTarget target
    )
        : base(source, target) { }

    public MapConfigMemberConverter<int, string> NumberConverter { get; } =
        new(
            nameof(IMapConverterTestConfiguredSource.Number),
            nameof(IMapConverterTestConfiguredTarget.Text),
            (value, args) =>
            {
                Assert.AreEqual(
                    nameof(IMapConverterTestConfiguredSource.Number),
                    args.SourcePropertyName
                );
                Assert.AreEqual(
                    nameof(IMapConverterTestConfiguredTarget.Text),
                    args.TargetPropertyName
                );
                return $"config:{value}";
            },
            (value, args) =>
            {
                Assert.AreEqual(
                    nameof(IMapConverterTestConfiguredSource.Number),
                    args.SourcePropertyName
                );
                Assert.AreEqual(
                    nameof(IMapConverterTestConfiguredTarget.Text),
                    args.TargetPropertyName
                );
                return int.Parse(value[7..]);
            }
        );
}
#pragma warning restore MAP0027
