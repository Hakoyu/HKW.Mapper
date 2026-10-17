namespace HKW.Mapper.Benchmark.Models;

[MapTarget(typeof(FooDto), Direction = MapDirections.To)]
public sealed class Foo
{
    public string Name { get; set; } = string.Empty;
    public int Int32 { get; set; }
    public long Int64 { get; set; }
    public int? NullableInt { get; set; }
    public float Float { get; set; }
    public double Double { get; set; }
    public DateTime DateTime { get; set; }

    [MapProperty(typeof(FooDto), nameof(FooDto.Child), typeof(FooConverter))]
    public Foo? Child { get; set; }
    public List<Foo> Children { get; set; } = [];
    public Foo[] ChildArray { get; set; } = [];
    public int[] IntArray { get; set; } = [];
    public List<int> Ints { get; set; } = [];
}

public sealed class FooConverter : IMapConverter<Foo?, FooDto?>
{
    public FooDto? Convert(Foo? value, MapConvertArgs args) =>
        value?.MapTo(new FooDto());

    public Foo? ConvertBack(FooDto? value, MapConvertArgs args) =>
        throw new NotSupportedException();
}

public sealed class FooDto
{
    public string Name { get; set; } = string.Empty;
    public int Int32 { get; set; }
    public long Int64 { get; set; }
    public int? NullableInt { get; set; }
    public float Float { get; set; }
    public double Double { get; set; }
    public DateTime DateTime { get; set; }
    public FooDto? Child { get; set; }
    public List<FooDto> Children { get; set; } = [];
    public FooDto[] ChildArray { get; set; } = [];
    public int[] IntArray { get; set; } = [];
    public List<int> Ints { get; set; } = [];
}
