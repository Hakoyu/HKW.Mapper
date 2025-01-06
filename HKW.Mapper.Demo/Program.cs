using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq.Expressions;
using System.Numerics;
using System.Windows.Input;
using HKW.HKWMapper;

namespace HKW.HKWMapper.Demo;

internal class Program
{
    //private string $Name;
    //public string Name { get; set; } = string.Empty;


    static void Main(string[] args)
    {
        var task = new Task<int>(() => 1);
        //var test = new Test2();
        //var c = new TestMapConfig();
        //test.Value ??= 1;
        //test.
    }

    //internal static HKW.HKWMapper.Demo.TestTarget MapToTestTarget(
    //    this HKW.HKWMapper.Demo.TestSource source,
    //    HKW.HKWMapper.Demo.TestTarget target
    //)
    //{
    //    HKW_HKWMapper_Demo_TestMapConfig.BeginMapAction(source, target);
    //    HKW_HKWMapper_Demo_TestMapConfig.GetMapActionAsync("Value")(source, target).Wait();
    //    if (source.Value1 == default)
    //        if (target.Value1 == default)
    //            target.Value1 = source.Value1;
    //    if (source.Value2 is not null)
    //        if (target.Value2 is null)
    //            target.Value2 = source.Value2;
    //    target.Value3 = HKW_HKWMapper_Demo_TestConverter.Convert(source, source.Value3);
    //    target.Value4 = HKW_HKWMapper_Demo_TestConverter.Convert(source, source.Value4);
    //    return target;
    //}
}

[MapTo(
    typeof(TestTarget),
    ScrutinyMode = true,
    MapperConfig = typeof(TestMapConfig),
    InvokeState = MapMethodInvokeState.Both
)]
[MapFrom(typeof(TestSource), TargetName = "TestSource1")]
[MapFrom(typeof(TestTarget))]
internal class TestSource
{
    public int Value { get; set; }

    [TestSourceMapToTestTargetProperty(
        "Value1",
        MapWhenLValueNullOrDefault = true,
        MapWhenRValueNotNullOrDefault = true
    )]
    [TestSourceMapFromTestTargetProperty(
        "Value1",
        MapWhenLValueNullOrDefault = true,
        MapWhenRValueNotNullOrDefault = true
    )]
    public int Value1 { get; set; }

    [TestSourceMapToTestTargetProperty(
        "Value2",
        MapWhenLValueNullOrDefault = true,
        MapWhenRValueNotNullOrDefault = true
    )]
    [TestSourceMapFromTestTargetProperty(
        "Value2",
        MapWhenLValueNullOrDefault = true,
        MapWhenRValueNotNullOrDefault = true
    )]
    public string Value2 { get; set; }

    [TestSourceMapToTestTargetProperty(typeof(TestConverter))]
    [TestSourceMapFromTestTargetProperty(typeof(TestConverter))]
    public int Value3 { get; set; }

    //[TestSourceMapFromTestTargetProperty(typeof(TestConverter))]
    //public int Value4 { get; set; }
}

internal class TestTarget
{
    internal int Abc;
    internal int Value { get; set; }
    public int Value1 { get; set; }
    public string Value2 { get; set; } = null!;
    public string? Value3 { get; set; }
    public Task<int> Value4 { get; set; }
}

internal static class Test3
{
    public class Test33
    {
        public int Value { get; set; }
    }
}

internal class TestConverter : MapConverter<int, string>
{
    public override string Convert(object source, int value)
    {
        return value.ToString();
    }

    public override int ConvertBack(object source, string value)
    {
        return int.Parse(value);
    }
}

internal class TestAsyncConverter : MapConverter<Task<int>, Task<string>>
{
    public override Task<string> Convert(object source, Task<int> value)
    {
        throw new NotImplementedException();
    }

    public override Task<int> ConvertBack(object source, Task<string> value)
    {
        throw new NotImplementedException();
    }
}

internal class TestMapConfig : MapperConfig<TestSource, TestTarget>
{
    public TestMapConfig()
    {
        AddMapAsync(
            x => x.Value,
            async (s, t) =>
            {
                await Task.Delay(1);
            }
        );
    }

    public override void BeginMapAction(TestSource source, TestTarget target)
    {
        return;
    }
}

internal class TestMap1Config : MapperConfig<TestSource, TestTarget>
{
    public TestMap1Config()
    {
        //AddMap(
        //    x => x.Value,
        //    (s, t) =>
        //    {
        //        s.Value = t.Value;
        //    }
        //);
    }

    public override void BeginMapAction(TestSource source, TestTarget target)
    {
        return;
    }

    public override void EndMapAction(TestSource source, TestTarget target)
    {
        return;
    }
}
