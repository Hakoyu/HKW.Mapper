using HKW.HKWMapper;

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
            (_, value) => $"number:{value}",
            (_, value) => int.Parse(value[7..])
        );

        Assert.AreEqual("number:5", converter.Convert(new object(), 5));
        Assert.AreEqual(6, converter.ConvertBack(new object(), "number:6"));
        Assert.Throws<ArgumentNullException>(() =>
            new MapConfigPropertyConverter<int, string>("Number", "Text", null!, (_, value) => 0)
        );
        Assert.Throws<ArgumentNullException>(() =>
            new MapConfigPropertyConverter<int, string>(
                "Number",
                "Text",
                (_, value) => string.Empty,
                null!
            )
        );
    }

    [TestMethod]
    public void CanMapToAnotherProperty()
    {
        var source = new RedirectSource { Original = 3 };
        var target = new RedirectTarget();

        source.MapToRedirectTarget(target);

        Assert.AreEqual(103, target.Mapped);
        target.Mapped = 205;
        source.MapFromRedirectTarget(target);
        Assert.AreEqual(5, source.Original);
    }
}

[MapTarget(typeof(RedirectTarget), typeof(RedirectConfig))]
public sealed class RedirectSource
{
    [RedirectSourceMapTargetRedirectTargetProperty("Mapped")]
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
            (_, value) => value + 100,
            (_, value) => value - 200
        );
}
