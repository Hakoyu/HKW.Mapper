using HKW.Mapper;

namespace HKW.MapperTest;

[TestClass]
public sealed class MapConfigPropertyConverterTests
{
    [TestMethod]
    public void ConvertsBothDirectionsAndValidatesDelegates()
    {
        var converter = new MapConfigPropertyConverter<int, string>(
            "Number",
            "Text",
            (value, _) => $"number:{value}",
            (value, _) => int.Parse(value[7..])
        );

        var source = new object();
        var target = new object();
        var args = new MapConvertArgs(source, target, "Number", "Text");

        Assert.AreSame(source, args.Source);
        Assert.AreSame(target, args.Target);
        Assert.AreEqual("Number", args.SourcePropertyName);
        Assert.AreEqual("Text", args.TargetPropertyName);
        Assert.AreEqual("number:5", converter.Convert(5, args));
        Assert.AreEqual(6, converter.ConvertBack("number:6", args));
        Assert.Throws<ArgumentNullException>(() =>
            new MapConfigPropertyConverter<int, string>("Number", "Text", null!, (value, _) => 0)
        );
        Assert.Throws<ArgumentNullException>(() =>
            new MapConfigPropertyConverter<int, string>(
                "Number",
                "Text",
                (value, _) => string.Empty,
                null!
            )
        );
    }

    [TestMethod]
    public void CanMapToAnotherProperty()
    {
        var source = new RedirectSource { Original = 3 };
        var target = new RedirectTarget();

        source.MapTo(target);

        Assert.AreEqual(103, target.Mapped);
        target.Mapped = 205;
        source.MapFrom(target);
        Assert.AreEqual(5, source.Original);
    }
}

[MapTarget(typeof(RedirectTarget), typeof(RedirectConfig))]
public sealed class RedirectSource
{
    [MapProperty(typeof(RedirectTarget), "Mapped")]
    public int Original { get; set; }
}

public sealed class RedirectTarget
{
    public int Mapped { get; set; }
}

public sealed class RedirectConfig : MapperConfig<RedirectSource, RedirectTarget>
{
    public RedirectConfig(RedirectSource source, RedirectTarget target)
        : base(source, target) { }

    public MapConfigPropertyConverter<int, int> ValueConverter { get; } =
        new(
            nameof(RedirectSource.Original),
            nameof(RedirectTarget.Mapped),
            (value, args) =>
            {
                Assert.IsInstanceOfType<RedirectSource>(args.Source);
                Assert.IsInstanceOfType<RedirectTarget>(args.Target);
                Assert.AreEqual(nameof(RedirectSource.Original), args.SourcePropertyName);
                Assert.AreEqual(nameof(RedirectTarget.Mapped), args.TargetPropertyName);
                return value + 100;
            },
            (value, args) =>
            {
                Assert.IsInstanceOfType<RedirectSource>(args.Source);
                Assert.IsInstanceOfType<RedirectTarget>(args.Target);
                Assert.AreEqual(nameof(RedirectSource.Original), args.SourcePropertyName);
                Assert.AreEqual(nameof(RedirectTarget.Mapped), args.TargetPropertyName);
                return value - 200;
            }
        );
}
